
using CoffeeNChill.ApiGateway.DTOs;
using CoffeeNChill.ApiGateway.Interfaces;
using CoffeeNChill.ApiGateway.Models;
using Microsoft.AspNetCore.Identity;

namespace CoffeeNChill.ApiGateway.Services
{
    // =====================================================
    // POE PART 3: REGISTRATION AND LOGIN SERVICE
    // =====================================================
    //
    // REGISTRATION:
    // - Public registration creates Customers only.
    // - Passwords are hashed before storage.
    //
    // LOGIN:
    // - Retrieves the trusted user record.
    // - Rejects incomplete registrations.
    // - Rejects disabled accounts.
    // - Verifies the password hash.
    // - Issues a signed JWT.
    //
    // Never store or return plain-text passwords.
    // =====================================================

    public class AuthService : IAuthService
    {
        private readonly IUserTableService _users;

        private readonly IPasswordHasher<UserEntity>
            _hasher;

        private readonly IJwtTokenService _jwtTokens;

        // =================================================
        // CONSTRUCTOR
        // =================================================

        public AuthService(
            IUserTableService users,
            IPasswordHasher<UserEntity> hasher,
            IJwtTokenService jwtTokens)
        {
            _users = users;
            _hasher = hasher;
            _jwtTokens = jwtTokens;
        }

        // =================================================
        // REGISTER CUSTOMER
        // =================================================

        public async Task<bool> RegisterAsync(
            RegisterRequestDto request)
        {
            var user = new UserEntity
            {
                // Public registration cannot choose
                // Staff or Admin privileges.
                PartitionKey = "Customer",

                RowKey =
                    request.Email.Trim().ToLowerInvariant(),

                FullName = request.FullName.Trim(),

                CreatedDate = DateTimeOffset.UtcNow,

                IsActive = true
            };

            // ASP.NET Core Identity securely hashes
            // the password before it is saved.
            user.PasswordHash =
                _hasher.HashPassword(
                    user,
                    request.Password);

            return await _users.RegisterCustomerAsync(
                user);
        }

        // =================================================
        // LOGIN
        // =================================================

        public async Task<LoginResponseDto?> LoginAsync(
            LoginRequestDto request)
        {
            // Retrieve only a registered, enabled user.
            UserEntity? user =
                await _users.GetUserByEmailAsync(
                    request.Email);

            if (user == null)
            {
                // Do not reveal whether the account
                // does not exist or has been disabled.
                return null;
            }

            // Defence in depth:
            // explicitly reject disabled accounts.
            if (user.IsActive == false)
            {
                return null;
            }

            // Compare against the stored password hash.
            PasswordVerificationResult verificationResult =
                _hasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    request.Password);

            if (verificationResult ==
                PasswordVerificationResult.Failed)
            {
                return null;
            }

            // Issue JWT only after all checks succeed.
            return _jwtTokens.GenerateToken(user);
        }
    }
}
