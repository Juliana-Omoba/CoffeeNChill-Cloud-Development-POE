using CoffeeNChill.Functions.DTOs;
using CoffeeNChill.Functions.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace CoffeeNChill.Functions.Functions.MenuItems
{
    public class UpdateMenuItemFunction
    {
        private readonly ITableStorageService _tableStorageService;
        private readonly ILogger<UpdateMenuItemFunction> _logger;

        public UpdateMenuItemFunction(
            ITableStorageService tableStorageService,
            ILogger<UpdateMenuItemFunction> logger)
        {
            _tableStorageService = tableStorageService;
            _logger = logger;
        }

        [Function("UpdateMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "put",
                Route = "menu/{category}/{sku}")]
            HttpRequestData req,
            string category,
            string sku)
        {
            _logger.LogInformation(
                "Updating menu item. Category: {Category}, SKU: {SKU}",
                category,
                sku);

            try
            {
                // Validate route parameters
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

                // Read and deserialize request body
                var request =
                    await JsonSerializer.DeserializeAsync<UpdateMenuItemRequest>(
                        req.Body,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                // Validate request body
                if (request == null)
                {
                    var badRequest =
                        req.CreateResponse(HttpStatusCode.BadRequest);

                    await badRequest.WriteAsJsonAsync(new
                    {
                        error = "Invalid request body."
                    });

                    return badRequest;
                }

                // Validate Name
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    var badRequest =
                        req.CreateResponse(HttpStatusCode.BadRequest);

                    await badRequest.WriteAsJsonAsync(new
                    {
                        error = "Name is required."
                    });

                    return badRequest;
                }

                // Validate Description
                if (string.IsNullOrWhiteSpace(request.Description))
                {
                    var badRequest =
                        req.CreateResponse(HttpStatusCode.BadRequest);

                    await badRequest.WriteAsJsonAsync(new
                    {
                        error = "Description is required."
                    });

                    return badRequest;
                }

                // Validate Price
                if (request.Price <= 0)
                {
                    var badRequest =
                        req.CreateResponse(HttpStatusCode.BadRequest);

                    await badRequest.WriteAsJsonAsync(new
                    {
                        error = "Price must be greater than zero."
                    });

                    return badRequest;
                }

                // Update menu item
                var updatedMenuItem =
                    await _tableStorageService.UpdateMenuItemAsync(
                        category,
                        sku,
                        request);

                // Menu item does not exist
                if (updatedMenuItem == null)
                {
                    var notFound =
                        req.CreateResponse(HttpStatusCode.NotFound);

                    await notFound.WriteAsJsonAsync(new
                    {
                        error = "Menu item not found."
                    });

                    return notFound;
                }

                // Successful update
                var response =
                    req.CreateResponse(HttpStatusCode.OK);

                //await response.WriteAsJsonAsync(updatedMenuItem);

                var responseDto = new MenuItemResponseDto
                {
                    Category = updatedMenuItem.PartitionKey,
                    SKU = updatedMenuItem.RowKey,
                    Name = updatedMenuItem.Name,
                    Description = updatedMenuItem.Description,
                    Price = updatedMenuItem.Price,
                    IsAvailable = updatedMenuItem.IsAvailable
                };

                await response.WriteAsJsonAsync(responseDto);

                return response;
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Invalid JSON received while updating menu item.");

                var badRequest =
                    req.CreateResponse(HttpStatusCode.BadRequest);

                await badRequest.WriteAsJsonAsync(new
                {
                    error = "The request body contains invalid JSON."
                });

                return badRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating menu item.");

                var response =
                    req.CreateResponse(
                        HttpStatusCode.InternalServerError);

                await response.WriteAsJsonAsync(new
                {
                    error = "An unexpected error occurred while updating the menu item."
                });

                return response;
            }
        }
    }
}