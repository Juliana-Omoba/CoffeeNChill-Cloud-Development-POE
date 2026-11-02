
using Azure;
using Azure.Data.Tables;
using CoffeeNChill.ApiGateway.Interfaces;
using CoffeeNChill.ApiGateway.Models;

namespace CoffeeNChill.ApiGateway.Services
{
    // =====================================================
    // POE PART 3: STAFF INVITATION TABLE STORAGE
    // =====================================================
    //
    // Stores Staff invitations in Azure Table Storage.
    //
    // Table: StaffInvitations
    //
    // Supports:
    // - Creating invitations
    // - Retrieving invitations
    // - Listing invitations
    // - Updating invitation status
    //
    // Uses ETag optimistic concurrency to prevent
    // conflicting invitation updates.
    // =====================================================

    public class StaffInvitationTableService
        : IStaffInvitationTableService
    {
        private const string TableName =
            "StaffInvitations";

        private readonly TableClient _tableClient;

        private readonly
            ILogger<StaffInvitationTableService> _logger;

        // =================================================
        // CONSTRUCTOR
        // =================================================

        public StaffInvitationTableService(
            IConfiguration configuration,
            ILogger<StaffInvitationTableService> logger)
        {
            _logger = logger;

            string connectionString =
                configuration.GetConnectionString(
                    "AzureStorage")
                ?? throw new InvalidOperationException(
                    "AzureStorage connection string is missing.");

            var serviceClient =
                new TableServiceClient(connectionString);

            _tableClient =
                serviceClient.GetTableClient(TableName);
        }

        // =================================================
        // CREATE INVITATION
        // =================================================

        public async Task CreateInvitationAsync(
            StaffInvitationEntity invitation)
        {
            await _tableClient.CreateIfNotExistsAsync();

            await _tableClient.AddEntityAsync(
                invitation);

            _logger.LogInformation(
                "Staff invitation {InvitationId} created.",
                invitation.RowKey);
        }

        // =================================================
        // GET INVITATION BY ID
        // =================================================

        public async Task<StaffInvitationEntity?>
            GetInvitationAsync(string invitationId)
        {
            await _tableClient.CreateIfNotExistsAsync();

            try
            {
                var response =
                    await _tableClient
                        .GetEntityAsync<StaffInvitationEntity>(
                            "Invitation",
                            invitationId);

                return response.Value;
            }
            catch (RequestFailedException ex)
                when (ex.Status == 404)
            {
                return null;
            }
        }

        // =================================================
        // GET ALL INVITATIONS
        // =================================================

        public async Task<List<StaffInvitationEntity>>
            GetAllInvitationsAsync()
        {
            await _tableClient.CreateIfNotExistsAsync();

            var invitations =
                new List<StaffInvitationEntity>();

            await foreach (
                StaffInvitationEntity invitation in
                _tableClient.QueryAsync<StaffInvitationEntity>(
                    item =>
                        item.PartitionKey == "Invitation"))
            {
                invitations.Add(invitation);
            }

            return invitations
                .OrderByDescending(
                    item => item.CreatedAt)
                .ToList();
        }

        // =================================================
        // UPDATE INVITATION WITH ETAG CONCURRENCY
        // =================================================
        //
        // STEP 1:
        // Update the entity using its current ETag.
        //
        // STEP 2:
        // Retrieve the updated entity from storage.
        //
        // STEP 3:
        // Copy the refreshed ETag to the caller's entity.
        //
        // This allows a later update to use the
        // correct ETag rather than a stale value.
        // =================================================

        public async Task UpdateInvitationAsync(
            StaffInvitationEntity invitation)
        {
            await _tableClient.CreateIfNotExistsAsync();

            // Do not bypass optimistic concurrency.
            await _tableClient.UpdateEntityAsync(
                invitation,
                invitation.ETag,
                TableUpdateMode.Replace);

            // Retrieve the latest ETag after the update.
            var refreshedResponse =
                await _tableClient
                    .GetEntityAsync<StaffInvitationEntity>(
                        invitation.PartitionKey,
                        invitation.RowKey);

            invitation.ETag =
                refreshedResponse.Value.ETag;

            invitation.Timestamp =
                refreshedResponse.Value.Timestamp;

            _logger.LogInformation(
                "Staff invitation {InvitationId} updated " +
                "with status {Status}.",
                invitation.RowKey,
                invitation.Status);
        }
    }
}
