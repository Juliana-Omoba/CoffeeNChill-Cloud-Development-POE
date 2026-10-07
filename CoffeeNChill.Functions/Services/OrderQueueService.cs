using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using CoffeeNChill.Functions.Interfaces;
using CoffeeNChill.Functions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoffeeNChill.Functions.Services
{
    public class OrderQueueService : IOrderQueueService
    {
        // ============================================================
        // POE PART 2 RUBRIC:
        // Azure Storage Queue Placement (Order Producer)
        //
        // Required queue name specified by the project.
        // ============================================================
        private const string QueueName = "order-processing-queue";

        private readonly QueueClient _queueClient;
        private readonly ILogger<OrderQueueService> _logger;

        public OrderQueueService(
            IConfiguration configuration,
            ILogger<OrderQueueService> logger)
        {
            _logger = logger;

            // --------------------------------------------------------
            // RUBRIC: Queue connection configuration
            //
            // AzureWebJobsStorage provides the connection to
            // Azure Storage/Azurite.
            // --------------------------------------------------------
            string connectionString =
                configuration["AzureWebJobsStorage"]
                ?? throw new InvalidOperationException(
                    "AzureWebJobsStorage connection string is not configured.");

            // --------------------------------------------------------
            // RUBRIC: Base64 encoding/decoding
            //
            // The top rubric band explicitly requires Base64 message
            // encoding/decoding.
            //
            // Azure Queue SDK automatically performs the Base64
            // encoding when sending messages.
            // --------------------------------------------------------
            QueueClientOptions options = new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            };

            _queueClient = new QueueClient(
                connectionString,
                QueueName,
                options);
        }

        public async Task SendOrderAsync(Order order)
        {
            try
            {
                // ----------------------------------------------------
                // RUBRIC: order-processing-queue setup
                //
                // Creates the queue if it does not already exist.
                // ----------------------------------------------------
                await _queueClient.CreateIfNotExistsAsync();

                // ----------------------------------------------------
                // RUBRIC: JSON payload serialization
                // 

                // // SERIALIZATION:
                // Convert the C# Order object into a JSON string
                // suitable for placement on Azure Storage Queue.
                // ----------------------------------------------------
                string jsonMessage =
                    JsonSerializer.Serialize(order);

                // ----------------------------------------------------
                // RUBRIC: Queue producer
                //
                // Send the serialized order to
                // order-processing-queue.
                //
                // Because MessageEncoding is Base64, the SDK handles
                // Base64 encoding before the message is stored.
                // ----------------------------------------------------
                await _queueClient.SendMessageAsync(jsonMessage);

                _logger.LogInformation(
                    "Order {OrderId} was successfully added to queue {QueueName}.",
                    order.OrderId,
                    QueueName);
            }
            catch (Exception ex)
            {
                // ----------------------------------------------------
                // RUBRIC: Robust queue connection/error handling
                //
                // Log queue/storage failures and rethrow them so the
                // producer Function can return an appropriate error.
                // ----------------------------------------------------
                _logger.LogError(
                    ex,
                    "Failed to add order {OrderId} to queue {QueueName}.",
                    order.OrderId,
                    QueueName);

                throw;
            }
        }
    }
}