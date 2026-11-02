
using CoffeeNChill.Functions.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace CoffeeNChill.Functions.Functions
{
    // ============================================================
    // POE PART 3: RETRIEVE AN INDIVIDUAL ORDER
    // ============================================================
    //
    // HTTP endpoint:
    // GET /api/orders/{orderDate}/{orderId}
    //
    // Example:
    // GET /api/orders/2026-10-23/ORD-TEST-001
    //
    // Azure Table Storage keys:
    //
    // PartitionKey = orderDate
    // RowKey       = orderId
    //
    // This endpoint only retrieves an existing order.
    // ============================================================

    public class GetOrderFunction
    {
        private readonly IOrderTableService _orderTableService;

        private readonly ILogger<GetOrderFunction> _logger;

        // --------------------------------------------------------
        // Constructor: Inject existing storage service.
        // --------------------------------------------------------

        public GetOrderFunction(
            IOrderTableService orderTableService,
            ILogger<GetOrderFunction> logger)
        {
            _orderTableService = orderTableService;
            _logger = logger;
        }

        // --------------------------------------------------------
        // HTTP GET: Retrieve one order using its storage keys.
        // --------------------------------------------------------

        [Function("GetOrder")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "orders/{orderDate}/{orderId}")]
            HttpRequestData req,
            string orderDate,
            string orderId)
        {
            // Validate the date format used as PartitionKey.
            if (!DateTime.TryParseExact(
                orderDate,
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out _))
            {
                var badRequest =
                    req.CreateResponse(HttpStatusCode.BadRequest);

                await badRequest.WriteAsJsonAsync(new
                {
                    error =
                        "orderDate must use yyyy-MM-dd format."
                });

                return badRequest;
            }

            // Validate the order ID.
            if (string.IsNullOrWhiteSpace(orderId))
            {
                var badRequest =
                    req.CreateResponse(HttpStatusCode.BadRequest);

                await badRequest.WriteAsJsonAsync(new
                {
                    error = "Order ID is required."
                });

                return badRequest;
            }

            try
            {
                // Retrieve the order using PartitionKey and RowKey.
                var order =
                    await _orderTableService.GetOrderAsync(
                        orderDate,
                        orderId);

                // Return HTTP 404 if the order does not exist.
                if (order == null)
                {
                    var notFound =
                        req.CreateResponse(HttpStatusCode.NotFound);

                    await notFound.WriteAsJsonAsync(new
                    {
                        error = "Order not found."
                    });

                    return notFound;
                }

                _logger.LogInformation(
                    "Successfully retrieved order {OrderId}.",
                    orderId);

                // Return the matching order as JSON.
                var response =
                    req.CreateResponse(HttpStatusCode.OK);

                await response.WriteAsJsonAsync(order);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while retrieving order {OrderId}.",
                    orderId);

                var response =
                    req.CreateResponse(
                        HttpStatusCode.InternalServerError);

                await response.WriteAsJsonAsync(new
                {
                    error =
                        "Unable to retrieve the order at this time."
                });

                return response;
            }
        }
    }
}
