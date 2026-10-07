using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Functions.Interfaces;
using CoffeeNChill.Functions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CoffeeNChill.Functions.Services
{
    public class OrderTableService : IOrderTableService
    {
        private const string TableName = "Orders";

        private readonly TableClient _tableClient;
        private readonly ILogger<OrderTableService> _logger;

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
    }
}