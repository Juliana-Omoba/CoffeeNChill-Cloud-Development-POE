
using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Functions.Interfaces;
using CoffeeNChill.Functions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoffeeNChill.Functions.Services
{
    // ============================================================
    // POE PART 2 + PART 3:
    // AZURE TABLE STORAGE ORDER SERVICE
    // ============================================================
    //
    // Stores and retrieves orders using Azure Table Storage.
    //
    // Table name: Orders
    //
    // PartitionKey = OrderDate (yyyy-MM-dd)
    // RowKey       = OrderId
    //
    // Existing Part 2 operations remain unchanged.
    // New Part 3 retrieval operations are added below.
    // ============================================================

    public class OrderTableService : IOrderTableService
    {
        private const string TableName = "Orders";

        private readonly TableClient _tableClient;

        private readonly ILogger<OrderTableService> _logger;

        // ========================================================
        // STEP 1: CONSTRUCTOR
        // ========================================================

        public OrderTableService(
            IConfiguration configuration,
            ILogger<OrderTableService> logger)
        {
            _logger = logger;

            string connectionString =
                configuration["AzureWebJobsStorage"]
                ?? throw new InvalidOperationException(
                    "AzureWebJobsStorage connection string is not configured.");

            _tableClient = new TableClient(
                connectionString,
                TableName);
        }

        // ========================================================
        // EXISTING PART 2: CREATE ORDER
        // ========================================================

        public async Task CreateOrderAsync(OrderEntity order)
        {
            try
            {
                // RUBRIC: Ensure the Orders table exists.
                await _tableClient.CreateIfNotExistsAsync();

                // RUBRIC: Create the initial order record.
                // The entity enters the table with Status = "Received".
                await _tableClient.AddEntityAsync(order);

                _logger.LogInformation(
                    "Order {OrderId} created in Orders table with status {Status}.",
                    order.OrderId,
                    order.Status);
            }
            catch (Exception ex)
            {
                // RUBRIC: Storage failure logging/error handling.
                _logger.LogError(
                    ex,
                    "Failed to create order {OrderId} in Orders table.",
                    order.OrderId);

                throw;
            }
        }

        // ========================================================
        // EXISTING PART 2: UPDATE ORDER STATUS
        // ========================================================

        public async Task UpdateOrderStatusAsync(
            string partitionKey,
            string orderId,
            string status)
        {
            try
            {
                // Locate the existing order using the required
                // PartitionKey + RowKey combination.
                Response<OrderEntity> response =
                    await _tableClient.GetEntityAsync<OrderEntity>(
                        partitionKey,
                        orderId);

                OrderEntity entity = response.Value;

                // RUBRIC: Order lifecycle status management.
                entity.Status = status;
                entity.LastUpdated = DateTimeOffset.UtcNow;

                await _tableClient.UpdateEntityAsync(
                    entity,
                    entity.ETag,
                    TableUpdateMode.Replace);

                _logger.LogInformation(
                    "Order {OrderId} status updated to {Status}.",
                    orderId,
                    status);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update order {OrderId} to status {Status}.",
                    orderId,
                    status);

                throw;
            }
        }

        // ========================================================
        // NEW PART 3: RETRIEVE ALL ORDERS
        // ========================================================
        //
        // Retrieves orders stored in the Orders table.
        //
        // Results are ordered by newest OrderTimestamp first.
        //
        // This method does not change order records.
        // ========================================================

        public async Task<List<OrderEntity>> GetAllOrdersAsync()
        {
            try
            {
                // Ensure the table exists before querying.
                await _tableClient.CreateIfNotExistsAsync();

                var orders = new List<OrderEntity>();

                // Azure Table Storage returns query results
                // asynchronously, potentially across pages.
                await foreach (
                    OrderEntity order in
                    _tableClient.QueryAsync<OrderEntity>())
                {
                    orders.Add(order);
                }

                // Display the newest orders first.
                orders.Sort((first, second) =>
                    second.OrderTimestamp.CompareTo(
                        first.OrderTimestamp));

                _logger.LogInformation(
                    "Retrieved {Count} orders from Azure Table Storage.",
                    orders.Count);

                return orders;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to retrieve orders from Azure Table Storage.");

                throw;
            }
        }

        // ========================================================
        // NEW PART 3: RETRIEVE ONE ORDER
        // ========================================================
        //
        // Uses the exact Azure Table Storage keys:
        //
        // PartitionKey = order date
        // RowKey       = order ID
        //
        // Returns null when the order does not exist.
        // ========================================================

        public async Task<OrderEntity?> GetOrderAsync(
            string partitionKey,
            string orderId)
        {
            // Reject missing storage keys.
            if (string.IsNullOrWhiteSpace(partitionKey) ||
                string.IsNullOrWhiteSpace(orderId))
            {
                return null;
            }

            try
            {
                await _tableClient.CreateIfNotExistsAsync();

                Response<OrderEntity> response =
                    await _tableClient.GetEntityAsync<OrderEntity>(
                        partitionKey,
                        orderId);

                return response.Value;
            }
            catch (RequestFailedException ex)
                when (ex.Status == 404)
            {
                // The requested order was not found.
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to retrieve order {OrderId} for date {OrderDate}.",
                    orderId,
                    partitionKey);

                throw;
            }
        }
    }
}
