
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
    // POE PART 3: RETRIEVE ALL CUSTOMER ORDERS
    // ============================================================
    //
    // HTTP endpoint:
    // GET /api/orders
    //
    // Retrieves existing orders from Azure Table Storage.
    //
    // This Function does not create, modify or delete orders.
    //
    // Staff/Admin authorisation must be enforced by the
    // API Gateway. The Function itself currently uses
    // AuthorizationLevel.Anonymous for local integration.
    // ============================================================

    public class GetAllOrdersFunction
    {
        private readonly IOrderTableService _orderTableService;

        private readonly ILogger<GetAllOrdersFunction> _logger;

        // --------------------------------------------------------
        // Constructor: Inject the existing order storage service.
        // --------------------------------------------------------

        public GetAllOrdersFunction(
            IOrderTableService orderTableService,
            ILogger<GetAllOrdersFunction> logger)
        {
            _orderTableService = orderTableService;
            _logger = logger;
        }

        // --------------------------------------------------------
        // HTTP GET: Retrieve all orders.
        // --------------------------------------------------------

        [Function("GetAllOrders")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "orders")]
            HttpRequestData req)
        {
            try
            {
                // Retrieve the orders through the service layer.
                // The service sorts orders newest first.
                var orders =
                    await _orderTableService.GetAllOrdersAsync();

                _logger.LogInformation(
                    "Successfully retrieved {Count} orders.",
                    orders.Count);

                // Return the stored orders as JSON.
                var response =
                    req.CreateResponse(HttpStatusCode.OK);

                await response.WriteAsJsonAsync(orders);

                return response;
            }
            catch (Exception ex)
            {
                // Log internal errors without exposing
                // storage connection details to clients.
                _logger.LogError(
                    ex,
                    "An error occurred while retrieving orders.");

                var response =
                    req.CreateResponse(
                        HttpStatusCode.InternalServerError);

                await response.WriteAsJsonAsync(new
                {
                    error =
                        "Unable to retrieve orders at this time."
                });

                return response;
            }
        }
    }
}
