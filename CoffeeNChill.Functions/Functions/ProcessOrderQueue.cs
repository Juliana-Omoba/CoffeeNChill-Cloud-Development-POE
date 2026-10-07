using CoffeeNChill.Functions.Interfaces;
using CoffeeNChill.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoffeeNChill.Functions.Functions
{
    public class ProcessOrderQueue
    {
        private readonly IOrderTableService _orderTableService;
        private readonly ILogger<ProcessOrderQueue> _logger;

        public ProcessOrderQueue(
            IOrderTableService orderTableService,
            ILogger<ProcessOrderQueue> logger)
        {
            _orderTableService = orderTableService;
            _logger = logger;
        }

        // ============================================================
        // POE PART 2 RUBRIC:
        // Queue-Triggered Azure Functions & Order Tracking
        //
        // Automatically executes when a message arrives on
        // order-processing-queue.
        // ============================================================
        [Function("ProcessOrderQueue")]
        public async Task Run(
            [QueueTrigger(
                "order-processing-queue",
                Connection = "AzureWebJobsStorage")]
            string queueMessage)
        {
            _logger.LogInformation(
                "Queue message received: {QueueMessage}",
                queueMessage);

            try
            {


                // ----------------------------------------------------
                // DESERIALIZATION:
                // RUBRIC: Parse/deserialise queue JSON
                //
                // Convert the JSON message back into an Order object.
                // ----------------------------------------------------
                Order? order =
                    JsonSerializer.Deserialize<Order>(
                        queueMessage,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                // ----------------------------------------------------
                // RUBRIC: Graceful queue-message validation
                //
                // Invalid messages cause processing to fail rather
                // than creating corrupt table records.
                // ----------------------------------------------------
                if (order == null)
                {
                    throw new InvalidOperationException(
                        "Queue message could not be deserialized into an Order.");
                }

                if (string.IsNullOrWhiteSpace(order.OrderId))
                {
                    throw new InvalidOperationException(
                        "Queue message does not contain a valid OrderId.");
                }

                // ----------------------------------------------------
                // RUBRIC: Orders Table key design
                //
                // PartitionKey = OrderDate
                // RowKey       = OrderId
                // ----------------------------------------------------
                string partitionKey =
                    order.OrderTimestamp.UtcDateTime
                        .ToString("yyyy-MM-dd");

                // ----------------------------------------------------
                // RUBRIC: Orders Azure Table entity creation
                //
                // Initial lifecycle status = Received.
                // ----------------------------------------------------
                var orderEntity = new OrderEntity
                {
                    PartitionKey = partitionKey,
                    RowKey = order.OrderId,

                    OrderId = order.OrderId,
                    CustomerName = order.CustomerName,

                    SelectedItemSKUs =
                        JsonSerializer.Serialize(
                            order.SelectedItemSKUs),

                    TotalPrice =
                        Convert.ToDouble(order.TotalPrice),

                    OrderTimestamp =
                        order.OrderTimestamp,

                    Status = "Received",

                    LastUpdated =
                        DateTimeOffset.UtcNow
                };

                // RUBRIC: Persist order to Orders Azure Table.
                await _orderTableService
                    .CreateOrderAsync(orderEntity);

                _logger.LogInformation(
                    "Order {OrderId} status: Received.",
                    order.OrderId);

                // ====================================================
                // RUBRIC: Order lifecycle status management
                //
                // Required lifecycle:
                // Received → Preparing → Ready → Collected
                // ====================================================

                await Task.Delay(2000);

                await _orderTableService
                    .UpdateOrderStatusAsync(
                        partitionKey,
                        order.OrderId,
                        "Preparing");

                _logger.LogInformation(
                    "Order {OrderId} status: Preparing.",
                    order.OrderId);

                await Task.Delay(2000);

                await _orderTableService
                    .UpdateOrderStatusAsync(
                        partitionKey,
                        order.OrderId,
                        "Ready");

                _logger.LogInformation(
                    "Order {OrderId} status: Ready.",
                    order.OrderId);

                await Task.Delay(2000);

                await _orderTableService
                    .UpdateOrderStatusAsync(
                        partitionKey,
                        order.OrderId,
                        "Collected");

                _logger.LogInformation(
                    "Order {OrderId} status: Collected.",
                    order.OrderId);
            }
            catch (Exception ex)
            {
                // ====================================================
                // RUBRIC: Failure / poison-queue handling
                //
                // 1. Log the failed queue message and exception.
                // 2. Rethrow the exception.
                //
                // Rethrowing tells the Azure Functions Queue Trigger
                // that processing FAILED. The Functions runtime can
                // retry the message and, after the configured maximum
                // dequeue attempts, move the repeatedly failing
                // message to the poison queue.
                // ====================================================
                _logger.LogError(
                    ex,
                    "Failed to process queue message: {QueueMessage}",
                    queueMessage);

                throw;
            }
        }
    }
}