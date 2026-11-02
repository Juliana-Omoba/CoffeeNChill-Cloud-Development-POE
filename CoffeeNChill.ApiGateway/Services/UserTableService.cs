
using Azure;
using Azure.Data.Tables;
using CoffeeNChill.ApiGateway.Interfaces;
using CoffeeNChill.ApiGateway.Models;

namespace CoffeeNChill.ApiGateway.Services
{
    // =====================================================
    // POE PART 3: AZURE TABLE USER STORAGE SERVICE
    // =====================================================
    //
    // Azure Tables:
    //
    // 1. Users
    //    PartitionKey = Customer, Staff or Admin
    //    RowKey       = Normalised email
    //
    // 2. UserEmailIndex
    //    PartitionKey = Email
    //    RowKey       = Normalised email
    //    Role         = Customer, Staff or Admin
    //    State        = Pending or Active
    //
    // UserEmailIndex.State controls whether the
    // registration has been completed.
    //
    // UserEntity.IsActive controls whether an
    // administrator has enabled the account.
    //
    // These are separate responsibilities.
    // =====================================================

    public class UserTableService : IUserTableService
    {
        private readonly TableClient _users;
        private readonly TableClient _emailIndex;

        // =================================================
        // CONSTRUCTOR
        // =================================================

        public UserTableService(
            IConfiguration configuration)
        {
            string connectionString =
                configuration.GetConnectionString(
                    "AzureStorage")
                ?? throw new InvalidOperationException(
                    "AzureStorage connection string is missing.");

            var serviceClient =
                new TableServiceClient(connectionString);

            _users = serviceClient.GetTableClient("Users");

            _emailIndex =
                serviceClient.GetTableClient(
                    "UserEmailIndex");
        }

        // =================================================
        // GET USERS TABLE CLIENT
        // =================================================

        public async Task<TableClient> GetTableClientAsync()
        {
            await _users.CreateIfNotExistsAsync();

            return _users;
        }

        // =================================================
        // GET USER BY ROLE AND EMAIL
        // =================================================

        public async Task<UserEntity?> GetUserAsync(
            string role,
            string email)
        {
            if (string.IsNullOrWhiteSpace(role) ||
                string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            // Only trusted application roles are allowed.
            if (role != "Customer" &&
                role != "Staff" &&
                role != "Admin")
            {
                return null;
            }

            await _users.CreateIfNotExistsAsync();

            string normalisedEmail =
                email.Trim().ToLowerInvariant();

            try
            {
                var response =
                    await _users.GetEntityAsync<UserEntity>(
                        role,
                        normalisedEmail);

                return response.Value;
            }
            catch (RequestFailedException ex)
                when (ex.Status == 404)
            {
                return null;
            }
        }

        // =================================================
        // GET ACTIVE REGISTERED USER BY EMAIL
        // =================================================
        //
        // Login uses UserEmailIndex to determine
        // which trusted role owns the email.
        //
        // Both conditions must be satisfied:
        //
        // 1. Registration index is Active.
        // 2. User account is not disabled.
        // =================================================

        public async Task<UserEntity?> GetUserByEmailAsync(
            string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            await _emailIndex.CreateIfNotExistsAsync();

            string normalisedEmail =
                email.Trim().ToLowerInvariant();

            UserEmailIndexEntity index;

            try
            {
                var response =
                    await _emailIndex
                        .GetEntityAsync<UserEmailIndexEntity>(
                            "Email",
                            normalisedEmail);

                index = response.Value;
            }
            catch (RequestFailedException ex)
                when (ex.Status == 404)
            {
                return null;
            }

            // Incomplete registrations cannot log in.
            if (!string.Equals(
                    index.State,
                    "Active",
                    StringComparison.Ordinal))
            {
                return null;
            }

            if (index.Role != "Customer" &&
                index.Role != "Staff" &&
                index.Role != "Admin")
            {
                return null;
            }

            UserEntity? user =
                await GetUserAsync(
                    index.Role,
                    normalisedEmail);

            if (user == null)
            {
                return null;
            }

            // Explicitly disabled accounts are rejected.
            // null means an existing legacy account.
            if (user.IsActive == false)
            {
                return null;
            }

            return user;
        }

        // =================================================
        // REGISTER CUSTOMER
        // =================================================
        //
        // Public registration can create only
        // Customer accounts.
        // =================================================

        public async Task<bool> RegisterCustomerAsync(
            UserEntity user)
        {
            return await RegisterUserAsync(
                user,
                "Customer");
        }

        // =================================================
        // REGISTER INVITED STAFF
        // =================================================
        //
        // Called after the invitation service has
        // validated a Staff invitation.
        // =================================================

        public async Task<bool> RegisterInvitedStaffAsync(
            UserEntity user)
        {
            return await RegisterUserAsync(
                user,
                "Staff");
        }

        // =================================================
        // REGISTER USER WITH TRUSTED ROLE
        // =================================================
        //
        // Step 1: Reserve the email globally.
        // Step 2: Create the user.
        // Step 3: Activate the email index.
        //
        // These operations span two Azure Tables.
        // They are not one atomic transaction.
        //
        // A partial storage failure may require
        // administrator reconciliation.
        // =================================================

        private async Task<bool> RegisterUserAsync(
            UserEntity user,
            string trustedRole)
        {
            if (user == null)
            {
                throw new ArgumentNullException(
                    nameof(user));
            }

            if (string.IsNullOrWhiteSpace(user.RowKey))
            {
                throw new ArgumentException(
                    "User email is required.");
            }

            if (string.IsNullOrWhiteSpace(
                    user.PasswordHash))
            {
                throw new ArgumentException(
                    "A password hash is required.");
            }

            await _users.CreateIfNotExistsAsync();
            await _emailIndex.CreateIfNotExistsAsync();

            string email =
                user.RowKey.Trim().ToLowerInvariant();

            // Never trust a caller-supplied role.
            user.PartitionKey = trustedRole;
            user.RowKey = email;

            // Newly registered accounts are enabled.
            user.IsActive = true;

            var index = new UserEmailIndexEntity
            {
                PartitionKey = "Email",
                RowKey = email,
                Role = trustedRole,
                State = "Pending",
                CreatedDate = DateTimeOffset.UtcNow
            };

            // ============================================
            // STEP 1: RESERVE EMAIL
            // ============================================

            try
            {
                await _emailIndex.AddEntityAsync(index);
            }
            catch (RequestFailedException ex)
                when (ex.Status == 409)
            {
                // The email already exists or is reserved.
                return false;
            }

            // ============================================
            // STEP 2: SAVE USER
            // ============================================

            await _users.AddEntityAsync(user);

            // ============================================
            // STEP 3: ACTIVATE EMAIL INDEX
            // ============================================

            index.State = "Active";

            await _emailIndex.UpdateEntityAsync(
                index,
                ETag.All,
                TableUpdateMode.Replace);

            return true;
        }

        // =================================================
        // GET ALL STAFF ACCOUNTS
        // =================================================
        //
        // Only entities in the Staff partition
        // are returned.
        //
        // This method returns storage entities
        // internally. The API controller must
        // exclude PasswordHash from its response.
        // =================================================

        public async Task<List<UserEntity>> GetAllStaffAsync()
        {
            await _users.CreateIfNotExistsAsync();

            var staffAccounts =
                new List<UserEntity>();

            await foreach (
                UserEntity staff in
                _users.QueryAsync<UserEntity>(
                    user => user.PartitionKey == "Staff"))
            {
                staffAccounts.Add(staff);
            }

            return staffAccounts
                .OrderBy(user => user.FullName)
                .ToList();
        }

        // =================================================
        // ENABLE OR DISABLE STAFF ACCOUNT
        // =================================================
        //
        // The update affects only Staff accounts.
        //
        // Azure Table ETag concurrency prevents
        // silently overwriting conflicting changes.
        //
        // Merge preserves the other stored fields.
        // =================================================

        public async Task<bool> UpdateStaffStatusAsync(
            string email,
            bool isActive)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            string normalisedEmail =
                email.Trim().ToLowerInvariant();

            UserEntity? staff =
                await GetUserAsync(
                    "Staff",
                    normalisedEmail);

            if (staff == null)
            {
                return false;
            }

            // Set the requested status.
            staff.IsActive = isActive;

            // Use the ETag obtained during retrieval.
            await _users.UpdateEntityAsync(
                staff,
                staff.ETag,
                TableUpdateMode.Merge);

            return true;
        }
    }
}
