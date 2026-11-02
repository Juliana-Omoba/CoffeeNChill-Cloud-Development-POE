
using Azure;
using Azure.Data.Tables;
using CoffeeNChill.ApiGateway.Models;
using Microsoft.AspNetCore.Identity;

namespace CoffeeNChill.ApiGateway.Services
{
    // =====================================================
    // POE PART 3: DEVELOPMENT-ONLY ADMIN PROVISIONER
    // =====================================================
    //
    // PURPOSE:
    //
    // Create an Admin account for local development
    // and Swagger testing.
    //
    // SECURITY:
    //
    // 1. No public Admin registration endpoint.
    // 2. Passwords are securely hashed.
    // 3. Existing accounts are never overwritten.
    // 4. Emails remain unique across all roles.
    //
    // The Development environment restriction is
    // enforced by Program.cs.
    // =====================================================

    public static class AdminAccountProvisioner
    {
        public static async Task ProvisionAsync(
            IConfiguration configuration,
            IPasswordHasher<UserEntity> passwordHasher,
            string email,
            string fullName,
            string password)
        {
            // =============================================
            // STEP 1: VALIDATE ADMIN DETAILS
            // =============================================

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException(
                    "Admin email address is required.");
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException(
                    "Admin full name is required.");
            }

            if (string.IsNullOrWhiteSpace(password) ||
                password.Length < 12)
            {
                throw new ArgumentException(
                    "Admin password must contain at least 12 characters.");
            }

            // Use the same email normalisation as
            // the existing Customer and Staff accounts.
            string normalisedEmail =
                email.Trim().ToLowerInvariant();

            // =============================================
            // STEP 2: CONNECT TO AZURE TABLE STORAGE
            // =============================================

            string connectionString =
                configuration.GetConnectionString(
                    "AzureStorage")
                ?? throw new InvalidOperationException(
                    "AzureStorage connection string is missing.");

            var serviceClient =
                new TableServiceClient(connectionString);

            TableClient usersTable =
                serviceClient.GetTableClient("Users");

            TableClient emailIndexTable =
                serviceClient.GetTableClient(
                    "UserEmailIndex");

            // Preserve existing tables and records.
            await usersTable.CreateIfNotExistsAsync();

            await emailIndexTable.CreateIfNotExistsAsync();

            // =============================================
            // STEP 3: RESERVE ADMIN EMAIL ADDRESS
            // =============================================
            //
            // All roles share the UserEmailIndex table.
            //
            // The Pending state prevents login until
            // the Admin user has been created.
            // =============================================

            var emailIndex =
                new UserEmailIndexEntity
                {
                    PartitionKey = "Email",
                    RowKey = normalisedEmail,
                    Role = "Admin",
                    State = "Pending",
                    CreatedDate = DateTimeOffset.UtcNow
                };

            try
            {
                await emailIndexTable.AddEntityAsync(
                    emailIndex);
            }
            catch (RequestFailedException ex)
                when (ex.Status == 409)
            {
                throw new InvalidOperationException(
                    "This email address is already registered. " +
                    "Use a different email for the Admin account.",
                    ex);
            }

            // =============================================
            // STEP 4: CREATE ADMIN USER ENTITY
            // =============================================

            var adminUser = new UserEntity
            {
                PartitionKey = "Admin",
                RowKey = normalisedEmail,
                FullName = fullName.Trim(),
                CreatedDate = DateTimeOffset.UtcNow
            };

            // Use the same ASP.NET Core Identity
            // password hashing as the existing
            // Customer and Staff implementations.
            adminUser.PasswordHash =
                passwordHasher.HashPassword(
                    adminUser,
                    password);

            // =============================================
            // STEP 5: SAVE ADMIN USER
            // =============================================
            //
            // AddEntityAsync does not overwrite
            // an existing account.
            // =============================================

            await usersTable.AddEntityAsync(adminUser);

            // =============================================
            // STEP 6: ACTIVATE ADMIN EMAIL INDEX
            // =============================================
            //
            // AuthService can now retrieve this Admin
            // account using GetUserByEmailAsync().
            // =============================================

            emailIndex.State = "Active";

            await emailIndexTable.UpdateEntityAsync(
                emailIndex,
                ETag.All,
                TableUpdateMode.Replace);
        }
    }
}
