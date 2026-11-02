
using Azure;
using CoffeeNChill.ApiGateway.DTOs;
using CoffeeNChill.ApiGateway.Interfaces;
using CoffeeNChill.ApiGateway.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;

namespace CoffeeNChill.ApiGateway.Services
{
    // =====================================================
    // POE PART 3: STAFF INVITATION BUSINESS SERVICE
    // =====================================================
    //
    // RESPONSIBILITIES:
    //
    // 1. Create secure Staff invitations.
    // 2. Store invitation token hashes.
    // 3. Retrieve invitation records.
    // 4. Revoke pending invitations.
    // 5. Validate invitation acceptance.
    // 6. Hash Staff passwords.
    // 7. Register invited Staff accounts.
    //
    // Invitation tokens are returned only when
    // created. The storage table contains hashes.
    // =====================================================

    public class StaffInvitationService
        : IStaffInvitationService
    {
        private readonly IStaffInvitationTableService
            _invitations;

        private readonly IUserTableService _users;

        private readonly IPasswordHasher<UserEntity>
            _passwordHasher;

        // =================================================
        // CONSTRUCTOR
        // =================================================

        public StaffInvitationService(
            IStaffInvitationTableService invitations,
            IUserTableService users,
            IPasswordHasher<UserEntity> passwordHasher)
        {
            _invitations = invitations;
            _users = users;
            _passwordHasher = passwordHasher;
        }

        // =================================================
        // CREATE INVITATION
        // =================================================

        public async Task<(
            StaffInvitationResponseDto Invitation,
            string Token)> CreateAsync(
                CreateStaffInvitationRequestDto request,
                string invitedBy)
        {
            string email =
                request.Email.Trim().ToLowerInvariant();

            // Reject an email that already belongs
            // to an active account.
            var existingUser =
                await _users.GetUserByEmailAsync(email);

            if (existingUser != null)
            {
                throw new InvalidOperationException(
                    "This email is already registered.");
            }

            // Generate 32 cryptographically random bytes.
            byte[] tokenBytes =
                RandomNumberGenerator.GetBytes(32);

            // Encode as hexadecimal for Swagger testing.
            string token =
                Convert.ToHexString(tokenBytes);

            // Store only the token hash.
            string tokenHash =
                Convert.ToHexString(
                    SHA256.HashData(
                        Encoding.UTF8.GetBytes(token)));

            DateTimeOffset now =
                DateTimeOffset.UtcNow;

            var invitation = new StaffInvitationEntity
            {
                PartitionKey = "Invitation",
                RowKey = Guid.NewGuid().ToString("N"),
                Email = email,
                FullName = request.FullName.Trim(),
                TokenHash = tokenHash,
                CreatedAt = now,
                ExpiresAt = now.AddHours(24),
                Status = "Pending",
                InvitedBy = invitedBy
            };

            await _invitations.CreateInvitationAsync(
                invitation);

            return (
                MapToResponse(invitation),
                token);
        }

        // =================================================
        // GET ALL INVITATIONS
        // =================================================

        public async Task<List<StaffInvitationResponseDto>>
            GetAllAsync()
        {
            var invitations =
                await _invitations.GetAllInvitationsAsync();

            return invitations
                .Select(MapToResponse)
                .ToList();
        }

        // =================================================
        // REVOKE INVITATION
        // =================================================

        public async Task<bool> RevokeAsync(
            string invitationId)
        {
            var invitation =
                await _invitations.GetInvitationAsync(
                    invitationId);

            if (invitation == null ||
                invitation.Status != "Pending")
            {
                return false;
            }

            invitation.Status = "Revoked";

            try
            {
                // ETag prevents overwriting a newer
                // invitation state.
                await _invitations.UpdateInvitationAsync(
                    invitation);

                return true;
            }
            catch (RequestFailedException ex)
                when (ex.Status == 409 ||
                      ex.Status == 412)
            {
                return false;
            }
        }

        // =================================================
        // ACCEPT INVITATION
        // =================================================

        public async Task<bool> AcceptAsync(
            AcceptStaffInvitationRequestDto request)
        {
            // ============================================
            // STEP 1: VALIDATE REQUEST
            // ============================================

            if (request == null ||
                string.IsNullOrWhiteSpace(
                    request.InvitationId) ||
                string.IsNullOrWhiteSpace(
                    request.Token) ||
                string.IsNullOrWhiteSpace(
                    request.Password) ||
                request.Password.Length < 12)
            {
                return false;
            }

            // ============================================
            // STEP 2: RETRIEVE INVITATION
            // ============================================

            var invitation =
                await _invitations.GetInvitationAsync(
                    request.InvitationId.Trim());

            if (invitation == null)
            {
                return false;
            }

            // ============================================
            // STEP 3: CHECK STATUS AND EXPIRY
            // ============================================

            if (invitation.Status != "Pending" ||
                invitation.ExpiresAt <=
                    DateTimeOffset.UtcNow)
            {
                return false;
            }

            // ============================================
            // STEP 4: VERIFY TOKEN
            // ============================================

            byte[] suppliedHash =
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(
                        request.Token.Trim()));

            byte[] storedHash;

            try
            {
                storedHash =
                    Convert.FromHexString(
                        invitation.TokenHash);
            }
            catch (FormatException)
            {
                return false;
            }

            // Reject malformed stored hashes.
            if (storedHash.Length != suppliedHash.Length)
            {
                return false;
            }

            // Compare hashes in constant time.
            if (!CryptographicOperations.FixedTimeEquals(
                suppliedHash,
                storedHash))
            {
                return false;
            }

            // ============================================
            // STEP 5: CHECK EXISTING ACTIVE ACCOUNT
            // ============================================

            var existingUser =
                await _users.GetUserByEmailAsync(
                    invitation.Email);

            if (existingUser != null)
            {
                return false;
            }

            // ============================================
            // STEP 6: CLAIM INVITATION
            // ============================================
            //
            // ETag concurrency allows only one request
            // to move this invitation from Pending
            // to Processing.
            // ============================================

            invitation.Status = "Processing";

            try
            {
                await _invitations.UpdateInvitationAsync(
                    invitation);
            }
            catch (RequestFailedException ex)
                when (ex.Status == 409 ||
                      ex.Status == 412)
            {
                return false;
            }

            // ============================================
            // STEP 7: PREPARE STAFF ACCOUNT
            // ============================================

            var staffUser = new UserEntity
            {
                PartitionKey = "Staff",
                RowKey =
                    invitation.Email.Trim().ToLowerInvariant(),
                FullName = invitation.FullName,
                CreatedDate = DateTimeOffset.UtcNow
            };

            // Hash password before storing.
            staffUser.PasswordHash =
                _passwordHasher.HashPassword(
                    staffUser,
                    request.Password);

            // ============================================
            // STEP 8: REGISTER STAFF ACCOUNT
            // ============================================

            bool registered =
                await _users.RegisterInvitedStaffAsync(
                    staffUser);

            if (!registered)
            {
                // Another account already owns
                // or has reserved this email.
                invitation.Status = "Revoked";

                await _invitations.UpdateInvitationAsync(
                    invitation);

                return false;
            }

            // =================================================
            // STEP 9: FINALISE STAFF INVITATION
            // =================================================
            //
            // The Staff account has already been created.
            //
            // We now mark the invitation as Accepted.
            //
            // If the final Azure Table update fails, we log
            // the error and rethrow it. We must not report
            // successful acceptance until the invitation
            // record has been updated.
            //
            // An Admin recovery operation can subsequently
            // reconcile a Processing invitation with an
            // existing active Staff account.
            // =================================================

            invitation.Status = "Accepted";

            invitation.AcceptedAt =
                DateTimeOffset.UtcNow;

            try
            {
                await _invitations.UpdateInvitationAsync(
                    invitation);
            }
            catch (RequestFailedException)
            {
                // Do not return true if the final update failed.
                // Allow the existing exception middleware to
                // record the error and return HTTP 500.
                throw;
            }

            return true;
        }

        // =================================================
        // MAP STORAGE ENTITY TO RESPONSE DTO
        // =================================================
        //
        // Never include TokenHash in API responses.
        // =================================================

        private static StaffInvitationResponseDto
            MapToResponse(StaffInvitationEntity entity)
        {
            return new StaffInvitationResponseDto
            {
                InvitationId = entity.RowKey,
                FullName = entity.FullName,
                Email = entity.Email,

                Status =
                    entity.Status == "Pending" &&
                    entity.ExpiresAt <=
                        DateTimeOffset.UtcNow
                        ? "Expired"
                        : entity.Status,

                InvitedBy = entity.InvitedBy,
                CreatedAt = entity.CreatedAt,
                ExpiresAt = entity.ExpiresAt,
                AcceptedAt = entity.AcceptedAt
            };
        }
    }
}
