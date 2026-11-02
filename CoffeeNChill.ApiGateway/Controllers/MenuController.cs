
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace CoffeeNChill.ApiGateway.Controllers
{
    // =====================================================
    // POE PART 3: MENU API GATEWAY CONTROLLER
    // =====================================================
    //
    // PURPOSE:
    //
    // Route menu requests from Swagger/API clients
    // to the existing Azure Functions application.
    //
    // The Azure Functions remain responsible for:
    //
    // 1. Azure Table Storage operations.
    // 2. Menu item creation.
    // 3. Menu item retrieval.
    // 4. Menu item updates.
    // 5. Menu item deletion.
    //
    // The API Gateway is responsible for:
    //
    // 1. Exposing the public API routes.
    // 2. JWT authentication.
    // 3. Role-based authorisation.
    // 4. Forwarding requests to Azure Functions.
    // 5. Returning the backend HTTP response.
    //
    // EXISTING PART 2 FUNCTIONS ARE PRESERVED.
    // =====================================================

    [ApiController]
    [Route("api/menu")]
    [Produces("application/json")]
    public class MenuController : ControllerBase
    {
        // =================================================
        // STEP 1: DEPENDENCIES
        // =================================================
        //
        // IHttpClientFactory creates the named HTTP client
        // registered in Program.cs as "AzureFunctions".
        //
        // ILogger records Gateway errors without exposing
        // internal exception details to API consumers.
        // =================================================

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MenuController> _logger;

        public MenuController(
            IHttpClientFactory httpClientFactory,
            ILogger<MenuController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // =================================================
        // STEP 2: GET ALL MENU ITEMS
        // =================================================
        //
        // Gateway:
        // GET /api/menu
        //
        // Azure Function:
        // GET /api/menu
        //
        // This endpoint allows users to view the menu
        // without requiring a JWT.
        // =================================================

        /// <summary>
        /// Retrieves all menu items from Azure Table Storage.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            return await ForwardAsync(
                HttpMethod.Get,
                "api/menu",
                null,
                cancellationToken);
        }

        // =================================================
        // STEP 3: GET A SINGLE MENU ITEM
        // =================================================
        //
        // Gateway:
        // GET /api/menu/item/{category}/{sku}
        //
        // Azure Function:
        // GET /api/menu/item/{category}/{sku}
        // =================================================

        /// <summary>
        /// Retrieves a menu item using its category and SKU.
        /// </summary>
        [HttpGet("item/{category}/{sku}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(
            string category,
            string sku,
            CancellationToken cancellationToken)
        {
            string path =
                $"api/menu/item/" +
                $"{Uri.EscapeDataString(category)}/" +
                $"{Uri.EscapeDataString(sku)}";

            return await ForwardAsync(
                HttpMethod.Get,
                path,
                null,
                cancellationToken);
        }

        // =================================================
        // STEP 4: GET MENU ITEMS BY CATEGORY
        // =================================================
        //
        // Gateway:
        // GET /api/menu/category/{category}
        //
        // Azure Function:
        // GET /api/menu/category/{category}
        // =================================================

        /// <summary>
        /// Retrieves menu items belonging to a category.
        /// </summary>
        [HttpGet("category/{category}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByCategory(
            string category,
            CancellationToken cancellationToken)
        {
            string path =
                $"api/menu/category/" +
                Uri.EscapeDataString(category);

            return await ForwardAsync(
                HttpMethod.Get,
                path,
                null,
                cancellationToken);
        }

        // =================================================
        // STEP 5: CREATE MENU ITEM
        // =================================================
        //
        // Gateway:
        // POST /api/menu
        //
        // Azure Function:
        // POST /api/menu
        //
        // Only Staff and Admin users may create products.
        //
        // The request body is forwarded to the existing
        // CreateMenuItemFunction without changing its
        // DTO or storage implementation.
        // =================================================

        /// <summary>
        /// Creates a menu item. Requires Staff or Admin.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Staff,Admin")]
        [Consumes("application/json")]
        public async Task<IActionResult> Create(
            [FromBody] System.Text.Json.JsonElement menuItem,
            CancellationToken cancellationToken)
        {
            return await ForwardAsync(
                HttpMethod.Post,
                "api/menu",
                menuItem.GetRawText(),
                cancellationToken);
        }

        // =================================================
        // STEP 6: UPDATE MENU ITEM
        // =================================================
        //
        // Gateway:
        // PUT /api/menu/{category}/{sku}
        //
        // Azure Function:
        // PUT /api/menu/{category}/{sku}
        //
        // Only Staff and Admin users may update products.
        // =================================================

        /// <summary>
        /// Updates an existing menu item. Requires Staff or Admin.
        /// </summary>
        [HttpPut("{category}/{sku}")]
        [Authorize(Roles = "Staff,Admin")]
        [Consumes("application/json")]
        public async Task<IActionResult> Update(
            string category,
            string sku,
            [FromBody] System.Text.Json.JsonElement menuItem,
            CancellationToken cancellationToken)
        {
            string path =
                $"api/menu/" +
                $"{Uri.EscapeDataString(category)}/" +
                $"{Uri.EscapeDataString(sku)}";

            return await ForwardAsync(
                HttpMethod.Put,
                path,
                menuItem.GetRawText(),
                cancellationToken);
        }

        // =================================================
        // STEP 7: DELETE MENU ITEM
        // =================================================
        //
        // Gateway:
        // DELETE /api/menu/{category}/{sku}
        //
        // Azure Function:
        // DELETE /api/menu/{category}/{sku}
        //
        // Only Staff and Admin users may delete products.
        // =================================================

        /// <summary>
        /// Deletes a menu item. Requires Staff or Admin.
        /// </summary>
        [HttpDelete("{category}/{sku}")]
        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> Delete(
            string category,
            string sku,
            CancellationToken cancellationToken)
        {
            string path =
                $"api/menu/" +
                $"{Uri.EscapeDataString(category)}/" +
                $"{Uri.EscapeDataString(sku)}";

            return await ForwardAsync(
                HttpMethod.Delete,
                path,
                null,
                cancellationToken);
        }

        // =================================================
        // STEP 8: SHARED AZURE FUNCTIONS FORWARDING METHOD
        // =================================================
        //
        // This method prevents duplicated HTTP forwarding
        // logic across all six Gateway endpoints.
        //
        // It:
        //
        // 1. Creates the Azure Functions HTTP client.
        // 2. Constructs the outgoing HTTP request.
        // 3. Includes a JSON body when necessary.
        // 4. Sends the request to Azure Functions.
        // 5. Preserves the backend status code.
        // 6. Returns the backend response body.
        //
        // The Gateway does not directly modify the
        // Azure Table Storage menu records.
        // =================================================

        private async Task<IActionResult> ForwardAsync(
            HttpMethod method,
            string functionPath,
            string? jsonBody,
            CancellationToken cancellationToken)
        {
            try
            {
                // Obtain the configured Azure Functions
                // client from the HTTP client factory.
                var client =
                    _httpClientFactory.CreateClient(
                        "AzureFunctions");

                // Construct the outgoing request.
                using var request =
                    new HttpRequestMessage(
                        method,
                        functionPath);

                // Attach JSON only when the endpoint
                // requires a request body.
                if (jsonBody != null)
                {
                    request.Content =
                        new StringContent(
                            jsonBody,
                            System.Text.Encoding.UTF8,
                            "application/json");
                }

                // Request JSON responses from Functions.
                request.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue(
                        "application/json"));

                // Forward the request to Azure Functions.
                using var response =
                    await client.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                // Read the backend response body.
                string responseBody =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                // A successful DELETE may return 204
                // with no response body.
                if (response.StatusCode ==
                    System.Net.HttpStatusCode.NoContent)
                {
                    return NoContent();
                }

                // Preserve the status code returned by
                // the Azure Function, including:
                //
                // 200 OK
                // 201 Created
                // 400 Bad Request
                // 404 Not Found
                // 409 Conflict
                // 500 Internal Server Error

                int statusCode =
                    (int)response.StatusCode;

                // Preserve JSON response content when
                // Azure Functions returns JSON.
                //
                // Content(...) is used instead of Ok(...)
                // so that the existing JSON is not
                // serialized a second time.
                string contentType =
                    response.Content.Headers.ContentType?
                        .ToString()
                    ?? "application/json";

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
                // The backend request timed out.
                _logger.LogWarning(
                    "Azure Functions request timed out: {Path}",
                    functionPath);

                return StatusCode(
                    StatusCodes.Status504GatewayTimeout,
                    new
                    {
                        error =
                            "The Azure Functions service did not respond in time."
                    });
            }
            catch (HttpRequestException ex)
            {
                // This commonly occurs when the Azure
                // Functions host is not running.
                _logger.LogError(
                    ex,
                    "Unable to contact Azure Functions: {Path}",
                    functionPath);

                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        error =
                            "The API Gateway could not reach Azure Functions."
                    });
            }
        }
    }
}
