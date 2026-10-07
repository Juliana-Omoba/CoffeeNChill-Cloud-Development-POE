using CoffeeNChill.Functions.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CoffeeNChill.Functions.Functions.MenuItems
{
    public class DeleteMenuItemFunction
    {
        private readonly ITableStorageService _tableStorageService;
        private readonly ILogger<DeleteMenuItemFunction> _logger;

        public DeleteMenuItemFunction(
            ITableStorageService tableStorageService,
            ILogger<DeleteMenuItemFunction> logger)
        {
            _tableStorageService = tableStorageService;
            _logger = logger;
        }

        [Function("DeleteMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "delete",
                Route = "menu/{category}/{sku}")]
            HttpRequestData req,
            string category,
            string sku)
        {
            _logger.LogInformation(
                "Deleting menu item. Category: {Category}, SKU: {SKU}",
                category,
                sku);

            try
            {
                // Validate category
                if (string.IsNullOrWhiteSpace(category))
                {
                    var badRequest =
                        req.CreateResponse(HttpStatusCode.BadRequest);

                    await badRequest.WriteAsJsonAsync(new
                    {
                        error = "Category is required."
                    });

                    return badRequest;
                }

                // Validate SKU
                if (string.IsNullOrWhiteSpace(sku))
                {
                    var badRequest =
                        req.CreateResponse(HttpStatusCode.BadRequest);

                    await badRequest.WriteAsJsonAsync(new
                    {
                        error = "SKU is required."
                    });

                    return badRequest;
                }

                // Attempt to delete the menu item
                bool deleted =
                    await _tableStorageService
                        .DeleteMenuItemAsync(category, sku);

                // Menu item does not exist
                if (!deleted)
                {
                    var notFound =
                        req.CreateResponse(HttpStatusCode.NotFound);

                    await notFound.WriteAsJsonAsync(new
                    {
                        error = "Menu item not found."
                    });

                    return notFound;
                }

                // Successful deletion
                return req.CreateResponse(
                    HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting menu item. Category: {Category}, SKU: {SKU}",
                    category,
                    sku);

                var response =
                    req.CreateResponse(
                        HttpStatusCode.InternalServerError);

                await response.WriteAsJsonAsync(new
                {
                    error = "An unexpected error occurred while deleting the menu item."
                });

                return response;
            }
        }
    }
}