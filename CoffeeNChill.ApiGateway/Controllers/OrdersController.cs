
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Globalization;

namespace CoffeeNChill.ApiGateway.Controllers
{
    // ============================================================
    // POE PART 3: ORDERS API GATEWAY CONTROLLER
    // ============================================================
    //
    // PURPOSE:
    //
    // Expose the existing Azure Functions order producer
    // and the new order retrieval Functions through the
    // ASP.NET Core API Gateway.
    //
    // ARCHITECTURE:
    //
    // Client / Swagger
    //      |
    //      v
    // API Gateway (JWT authentication and authorisation)
    //      |
    //      v
    // Azure Functions
    //      |
    //      +-- POST /api/orders/queue
    //      |       |
    //      |       v
    //      |   Azure Queue Storage
    //      |       |
    //      |       v
    //      |   ProcessOrderQueue Function
    //      |       |
    //      |       v
    //      |   Azure Table Storage
    //      |
    //      +-- GET /api/orders
    //      |       |
    //      |       v
    //      |   Azure Table Storage
    //      |
    //      +-- GET /api/orders/{orderDate}/{orderId}
    //              |
    //              v
    //          Azure Table Storage
    //
    // IMPORTANT:
    //
    // The existing POE Part 2 queue-processing workflow
    // remains unchanged.
    //
    // POE PART 3 SECURITY:
    //
    // Customer:
    //     May submit orders.
    //
    // Staff and Admin:
    //     May retrieve orders.
    //
    // These role restrictions apply at the API Gateway.
    // Direct Azure Functions access must also be secured
    // before production deployment.
    // ============================================================

    [ApiController]
    [Route("api/orders")]
    [Produces("application/json")]
    public class OrdersController : ControllerBase
    {
        // ========================================================
        // STEP 1: DEPENDENCIES
        // ========================================================
        //
        // IHttpClientFactory supplies the named HTTP client
        // "AzureFunctions", registered in Program.cs.
        //
        // ILogger records failures for troubleshooting.
        // ========================================================

        private readonly IHttpClientFactory _httpClientFactory;

        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            IHttpClientFactory httpClientFactory,
            ILogger<OrdersController> logger)
        {
            _httpClientFactory = httpClientFactory;

            _logger = logger;
        }

        // ========================================================
        // STEP 2: QUEUE A CUSTOMER ORDER
        // ========================================================
        //
        // Gateway endpoint:
        // POST /api/orders/queue
        //
        // Existing Azure Function endpoint:
        // POST /api/orders/queue
        //
        // SECURITY:
        // Only authenticated Customer users may submit orders
        // through the API Gateway.
        //
        // The Azure Function validates:
        // - OrderId
        // - CustomerName
        // - SelectedItemSKUs
        // - TotalPrice
        //
        // It then sends the order to Azure Queue Storage.
        //
        // IMPORTANT:
        // Preserve the existing request format and POE Part 2
        // queue producer implementation.
        // ========================================================

        /// <summary>
        /// Queues a customer order for asynchronous processing.
        /// Requires an authenticated Customer.
        /// </summary>
        [HttpPost("queue")]
        [Authorize(Roles = "Customer")]
        [Consumes("application/json")]
        public async Task<IActionResult> QueueOrder(
            [FromBody] JsonElement orderRequest,
            CancellationToken cancellationToken)
        {
            // ----------------------------------------------------
            // 1. Ensure the incoming JSON is an object.
            // ----------------------------------------------------

            if (orderRequest.ValueKind != JsonValueKind.Object)
            {
                return BadRequest(new
                {
                    error = "The order request must be a JSON object."
                });
            }

            // ----------------------------------------------------
            // 2. Preserve the original JSON request structure.
            // ----------------------------------------------------

            string jsonBody = orderRequest.GetRawText();

            try
            {
                // ------------------------------------------------
                // 3. Obtain the named Azure Functions HTTP client.
                // ------------------------------------------------

                HttpClient client =
                    _httpClientFactory.CreateClient(
                        "AzureFunctions");

                // ------------------------------------------------
                // 4. Create the outgoing HTTP POST request.
                // ------------------------------------------------

                using var request =
                    new HttpRequestMessage(
                        HttpMethod.Post,
                        "api/orders/queue");

                // ------------------------------------------------
                // 5. Attach the JSON request body.
                // ------------------------------------------------

                request.Content =
                    new StringContent(
                        jsonBody,
                        Encoding.UTF8,
                        "application/json");

                // ------------------------------------------------
                // 6. Request a JSON response.
                // ------------------------------------------------

                request.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue(
                        "application/json"));

                // ------------------------------------------------
                // 7. Forward the order to Azure Functions.
                // ------------------------------------------------

                using HttpResponseMessage response =
                    await client.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                // ------------------------------------------------
                // 8. Read the Azure Functions response.
                // ------------------------------------------------

                string responseBody =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                // ------------------------------------------------
                // 9. Preserve the original Function status code.
                //
                // Examples:
                // 202 Accepted
                // 400 Bad Request
                // 500 Internal Server Error
                // ------------------------------------------------

                int statusCode = (int)response.StatusCode;

                string contentType =
                    response.Content.Headers.ContentType?
                        .ToString()
                    ?? "application/json";

                // ------------------------------------------------
                // 10. Return the JSON without serialising it again.
                // ------------------------------------------------

                return new ContentResult
                {
                    StatusCode = statusCode,
                    Content = responseBody,
                    ContentType = contentType
                };
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                // The Azure Functions request timed out.

                _logger.LogWarning(
                    "Azure Functions timed out while queuing an order.");

                return StatusCode(
                    StatusCodes.Status504GatewayTimeout,
                    new
                    {
                        error =
                            "The order-processing service did not respond in time."
                    });
            }
            catch (HttpRequestException ex)
            {
                // The Gateway could not reach Azure Functions.

                _logger.LogError(
                    ex,
                    "Unable to contact Azure Functions while queuing an order.");

                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        error =
                            "The API Gateway could not reach the order-processing service."
                    });
            }
        }

        // ========================================================
        // STEP 3: RETRIEVE ALL ORDERS
        // ========================================================
        //
        // Gateway endpoint:
        // GET /api/orders
        //
        // Azure Function endpoint:
        // GET /api/orders
        //
        // SECURITY:
        // Only authenticated Staff and Admin users may
        // retrieve the complete order list.
        //
        // This operation does not modify order records.
        // ========================================================

        /// <summary>
        /// Retrieves all customer orders from Azure Table Storage.
        /// Requires Staff or Admin.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> GetAllOrders(
            CancellationToken cancellationToken)
        {
            // Forward the GET request to GetAllOrdersFunction.
            // The helper preserves the backend HTTP status
            // and JSON response body.

            return await ForwardGetAsync(
                "api/orders",
                cancellationToken);
        }

        // ========================================================
        // STEP 4: RETRIEVE ONE ORDER
        // ========================================================
        //
        // Gateway endpoint:
        // GET /api/orders/{orderDate}/{orderId}
        //
        // Azure Function endpoint:
        // GET /api/orders/{orderDate}/{orderId}
        //
        // Example:
        // GET /api/orders/2026-10-23/ORD-TEST-001
        //
        // STORAGE KEYS:
        //
        // PartitionKey = orderDate (yyyy-MM-dd)
        // RowKey       = orderId
        //
        // SECURITY:
        // Only Staff and Admin users may retrieve an order.
        // ========================================================

        /// <summary>
        /// Retrieves one order using its date and order ID.
        /// Requires Staff or Admin.
        /// </summary>
        [HttpGet("{orderDate}/{orderId}")]
        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> GetOrder(
            string orderDate,
            string orderId,
            CancellationToken cancellationToken)
        {
            // ----------------------------------------------------
            // 1. Validate the required Azure Table PartitionKey
            //    date format.
            // ----------------------------------------------------

            bool validDate =
                DateTime.TryParseExact(
                    orderDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _);

            if (!validDate)
            {
                return BadRequest(new
                {
                    error =
                        "orderDate must use yyyy-MM-dd format."
                });
            }

            // ----------------------------------------------------
            // 2. Validate the order ID.
            // ----------------------------------------------------

            if (string.IsNullOrWhiteSpace(orderId))
            {
                return BadRequest(new
                {
                    error = "Order ID is required."
                });
            }

            // ----------------------------------------------------
            // 3. Construct the Azure Functions endpoint.
            //
            // EscapeDataString safely encodes route values.
            // ----------------------------------------------------

            string functionPath =
                $"api/orders/" +
                $"{Uri.EscapeDataString(orderDate)}/" +
                $"{Uri.EscapeDataString(orderId)}";

            // ----------------------------------------------------
            // 4. Forward the request to GetOrderFunction.
            // ----------------------------------------------------

            return await ForwardGetAsync(
                functionPath,
                cancellationToken);
        }

        // ========================================================
        // STEP 5: SHARED HTTP GET FORWARDING METHOD
        // ========================================================
        //
        // PURPOSE:
        //
        // Avoid duplicating HTTP forwarding logic between
        // GetAllOrders and GetOrder.
        //
        // RESPONSIBILITIES:
        //
        // 1. Obtain the Azure Functions HTTP client.
        // 2. Create the outgoing GET request.
        // 3. Send the request to Azure Functions.
        // 4. Read the response.
        // 5. Preserve the backend status code.
        // 6. Return the JSON without double serialisation.
        // 7. Handle backend connectivity failures.
        //
        // The API Gateway does not directly query
        // Azure Table Storage.
        // ========================================================

        private async Task<IActionResult> ForwardGetAsync(
            string functionPath,
            CancellationToken cancellationToken)
        {
            try
            {
                // ------------------------------------------------
                // 1. Obtain the configured Azure Functions client.
                // ------------------------------------------------

                HttpClient client =
                    _httpClientFactory.CreateClient(
                        "AzureFunctions");

                // ------------------------------------------------
                // 2. Construct the outgoing HTTP GET request.
                // ------------------------------------------------

                using var request =
                    new HttpRequestMessage(
                        HttpMethod.Get,
                        functionPath);

                // ------------------------------------------------
                // 3. Request JSON content.
                // ------------------------------------------------

                request.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue(
                        "application/json"));

                // ------------------------------------------------
                // 4. Forward the request to Azure Functions.
                // ------------------------------------------------

                using HttpResponseMessage response =
                    await client.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                // ------------------------------------------------
                // 5. Read the response body.
                // ------------------------------------------------

                string responseBody =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                // ------------------------------------------------
                // 6. Preserve the Azure Functions status code.
                //
                // Examples:
                //
                // 200 OK: Orders retrieved successfully.
                // 400 Bad Request: Invalid request parameters.
                // 404 Not Found: Requested order does not exist.
                // 500 Internal Server Error: Backend failure.
                // ------------------------------------------------

                int statusCode = (int)response.StatusCode;

                string contentType =
                    response.Content.Headers.ContentType?
                        .ToString()
                    ?? "application/json";

                // ------------------------------------------------
                // 7. Return the original backend response.
                // ------------------------------------------------

                return new ContentResult
                {
                    StatusCode = statusCode,
                    Content = responseBody,
                    ContentType = contentType
                };
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                // ------------------------------------------------
                // Azure Functions did not respond before timeout.
                // ------------------------------------------------

                _logger.LogWarning(
                    "Azure Functions timed out: {Path}",
                    functionPath);

                return StatusCode(
                    StatusCodes.Status504GatewayTimeout,
                    new
                    {
                        error =
                            "The order retrieval service did not respond in time."
                    });
            }
            catch (HttpRequestException ex)
            {
                // ------------------------------------------------
                // Azure Functions is unavailable or unreachable.
                // ------------------------------------------------

                _logger.LogError(
                    ex,
                    "Unable to contact Azure Functions: {Path}",
                    functionPath);

                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        error =
                            "The API Gateway could not reach the order retrieval service."
                    });
            }
        }
    }
}
