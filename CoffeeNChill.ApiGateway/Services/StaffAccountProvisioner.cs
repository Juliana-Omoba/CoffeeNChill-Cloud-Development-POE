
using Azure;
using Azure.Data.Tables;
using CoffeeNChill.ApiGateway.Models;
using Microsoft.AspNetCore.Identity;

namespace CoffeeNChill.ApiGateway.Services
{
    // =====================================================
    // DEVELOPMENT-ONLY STAFF ACCOUNT PROVISIONER
    // =====================================================
    //
    // PURPOSE:
    //
    // Creates a Staff account directly through the
    // application's trusted server-side code.
    //
    // This is used only for local development and testing.
    //
    // It does NOT:
    // 1. Allow customers to register as Staff.
    // 2. Create a public Staff registration endpoint.
    // 3. Modify existing Customer accounts.
    // 4. Store passwords as plain text.
    //
    // The account is stored in two Azure Tables:
    //
    // Users:
    //   PartitionKey = "Staff"
    //   RowKey       = normalised email
    //
    // UserEmailIndex:
    //   PartitionKey = "Email"
    //   RowKey       = normalised email
    //   Role         = "Staff"
    //   State        = "Active"
    //
    // =====================================================

    public static class StaffAccountProvisioner
    {
        public static async Task ProvisionAsync(
            IConfiguration configuration,
            IPasswordHasher<UserEntity> passwordHasher,
            string email,
            string fullName,
            string password)
        {
            // =============================================
            // STEP 1: VALIDATE INPUT
            // =============================================

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException(
                    "Staff email address is required.");
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException(
                    "Staff full name is required.");
            }

            if (string.IsNullOrWhiteSpace(password) ||
                password.Length < 12)
            {
                throw new ArgumentException(
                    "Staff password must contain at least 12 characters.");
            }

            // Normalise email addresses consistently
            // with the existing UserTableService.
            string normalisedEmail =
                email.Trim().ToLowerInvariant();

            // =============================================
            // STEP 2: CONNECT TO AZURE TABLE STORAGE
            // =============================================

            string connectionString =
                configuration.GetConnectionString("AzureStorage")
                ?? throw new InvalidOperationException(
                    "AzureStorage connection string is missing.");

            var serviceClient =
                new TableServiceClient(connectionString);

            TableClient usersTable =
                serviceClient.GetTableClient("Users");

            TableClient emailIndexTable =
                serviceClient.GetTableClient("UserEmailIndex");

            // Create tables only if they do not exist.
            // Existing tables and records are preserved.
            await usersTable.CreateIfNotExistsAsync();

            await emailIndexTable.CreateIfNotExistsAsync();

            // =============================================
            // STEP 3: RESERVE THE EMAIL ADDRESS
            // =============================================
            //
            // The email index is shared by Customer,
            // Staff and Admin accounts.
            //
            // This prevents the same email from being
            // registered under multiple roles.
            // =============================================

            var emailIndex = new UserEmailIndexEntity
            {
                PartitionKey = "Email",
                RowKey = normalisedEmail,
                Role = "Staff",
                State = "Pending"
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
                    "Use a different email for the Staff account.",
                    ex);
            }

            // =============================================
            // STEP 4: CREATE THE STAFF USER
            // =============================================

            var staffUser = new UserEntity
            {
                PartitionKey = "Staff",
                RowKey = normalisedEmail,
                FullName = fullName.Trim(),
                CreatedDate = DateTimeOffset.UtcNow
            };

            // Hash the password using the same ASP.NET
            // Core password hasher as Customer registration.
            //
            // The original password is not stored.
            staffUser.PasswordHash =
                passwordHasher.HashPassword(
                    staffUser,
                    password);

            // =============================================
            // STEP 5: SAVE STAFF ACCOUNT
            // =============================================
            //
            // AddEntityAsync creates a new record.
            //
            // It does not overwrite an existing user.
            // =============================================

            await usersTable.AddEntityAsync(
                staffUser);

            // =============================================
            // STEP 6: ACTIVATE THE EMAIL INDEX
            // =============================================
            //
            // Login checks that State is "Active".
            //
            // Therefore, the index is activated only
            // after the Staff user has been saved.
            // =============================================

            emailIndex.State = "Active";

            await emailIndexTable.UpdateEntityAsync(
                emailIndex,
                ETag.All,
                TableUpdateMode.Replace);
        }
    }
}
