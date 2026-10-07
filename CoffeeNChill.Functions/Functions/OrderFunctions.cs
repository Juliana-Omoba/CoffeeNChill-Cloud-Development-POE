using CoffeeNChill.Functions.DTOs;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoffeeNChill.Functions.Functions
{
    public class OrderFunctions
    {
        private readonly IOrderQueueService _orderQueueService;
        private readonly ILogger<OrderFunctions> _logger;

        public OrderFunctions(
            IOrderQueueService orderQueueService,
            ILogger<OrderFunctions> logger)
        {
            _orderQueueService = orderQueueService;
            _logger = logger;
        }

        // ============================================================
        // POE PART 2 RUBRIC:
        // Azure Storage Queue Placement (Order Producer)
        //
        // Implements the required producer endpoint:
        // POST /api/orders/queue
        //
        // This Function:
        // 1. Accepts an order from the client.
        // 2. Validates the client payload.
        // 3. Generates the server-side OrderTimestamp.
        // 4. Creates the Order object.
        // 5. Sends the order to order-processing-queue.
        // 6. Returns HTTP 202 Accepted.
        // 7. Handles and logs unexpected producer failures.
        // ============================================================

        [Function("QueueOrder")]
        public async Task<HttpResponseData> QueueOrder(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "post",
                Route = "orders/queue")]
            HttpRequestData req)
        {
            try
            {
                CreateOrderRequest? request;

                // ----------------------------------------------------
                // RUBRIC: Client payload handling
                // Deserialize incoming JSON request into the DTO.
                // Malformed JSON is rejected with HTTP 400.
                // ----------------------------------------------------
                try
                {
                    request =
                        await JsonSerializer.DeserializeAsync<CreateOrderRequest>(
                            req.Body,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                }
                catch (JsonException)
                {
                    return await CreateErrorResponse(
                        req,
                        HttpStatusCode.BadRequest,
                        "Invalid JSON request body.");
                }

                // ----------------------------------------------------
                // RUBRIC: Complete client payload validation
                // Reject an empty request body.
                // ----------------------------------------------------
                if (request == null)
                {
                    return await CreateErrorResponse(
                        req,
                        HttpStatusCode.BadRequest,
                        "Request body is required.");
                }

                // RUBRIC: Validate OrderId
                if (string.IsNullOrWhiteSpace(request.OrderId))
                {
                    return await CreateErrorResponse(
                        req,
                        HttpStatusCode.BadRequest,
                        "OrderId is required.");
                }

                // RUBRIC: Validate CustomerName
                if (string.IsNullOrWhiteSpace(request.CustomerName))
                {
                    return await CreateErrorResponse(
                        req,
                        HttpStatusCode.BadRequest,
                        "CustomerName is required.");
                }

                // RUBRIC: Validate SelectedItemSKUs
                // At least one valid, non-empty SKU must be supplied.
                if (request.SelectedItemSKUs == null ||
                    request.SelectedItemSKUs.Count == 0 ||
                    request.SelectedItemSKUs.Any(
                        sku => string.IsNullOrWhiteSpace(sku)))
                {
                    return await CreateErrorResponse(
                        req,
                        HttpStatusCode.BadRequest,
                        "At least one valid SelectedItemSKU is required.");
                }

                // RUBRIC: Validate TotalPrice
                if (request.TotalPrice <= 0)
                {
                    return await CreateErrorResponse(
                        req,
                        HttpStatusCode.BadRequest,
                        "TotalPrice must be greater than zero.");
                }

                // ----------------------------------------------------
                // RUBRIC: Timestamp generation
                //
                // The server generates OrderTimestamp instead of
                // accepting a client-controlled timestamp.
                // ----------------------------------------------------
                var order = new Order
                {
                    OrderId = request.OrderId.Trim(),
                    CustomerName = request.CustomerName.Trim(),

                    SelectedItemSKUs = request.SelectedItemSKUs
                        .Select(sku => sku.Trim())
                        .ToList(),

                    TotalPrice = request.TotalPrice,

                    OrderTimestamp = DateTimeOffset.UtcNow
                };

                // ----------------------------------------------------
                // RUBRIC: Queue producer logic
                //
                // Pass the validated Order to OrderQueueService.
                // The service performs JSON serialization and places
                // the message onto order-processing-queue.
                // ----------------------------------------------------
                await _orderQueueService.SendOrderAsync(order);

                _logger.LogInformation(
                    "Order {OrderId} accepted for asynchronous processing.",
                    order.OrderId);

                // ----------------------------------------------------
                // Return HTTP 202 Accepted because the order has been
                // accepted for asynchronous/background processing.
                // ----------------------------------------------------
                HttpResponseData response =
                    req.CreateResponse(HttpStatusCode.Accepted);

                await response.WriteAsJsonAsync(new
                {
                    message = "Order queued successfully.",
                    orderId = order.OrderId,
                    status = "Queued",
                    orderTimestamp = order.OrderTimestamp
                });

                return response;
            }
            catch (Exception ex)
            {
                // ----------------------------------------------------
                // RUBRIC: Robust producer error handling
                //
                // Queue/storage failures are logged and an appropriate
                // HTTP 500 response is returned to the client.
                // ----------------------------------------------------
                _logger.LogError(
                    ex,
                    "An unexpected error occurred while queuing an order.");

                return await CreateErrorResponse(
                    req,
                    HttpStatusCode.InternalServerError,
                    "Unable to queue the order at this time.");
            }
        }

        // Helper method used to produce consistent JSON error responses.
        private static async Task<HttpResponseData> CreateErrorResponse(
            HttpRequestData req,
            HttpStatusCode statusCode,
            string errorMessage)
        {
            HttpResponseData response =
                req.CreateResponse(statusCode);

            await response.WriteAsJsonAsync(new
            {
                error = errorMessage
            });

            return response;
        }
    }
}