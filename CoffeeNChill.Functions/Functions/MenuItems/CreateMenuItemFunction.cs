using Azure;
using CoffeeNChill.Functions.DTOs;
using CoffeeNChill.Functions.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace CoffeeNChill.Functions.Functions.MenuItems
{
    public class CreateMenuItemFunction
    {
        private readonly ITableStorageService _tableStorageService;
        private readonly ILogger<CreateMenuItemFunction> _logger;

        public CreateMenuItemFunction(
            ITableStorageService tableStorageService,
            ILogger<CreateMenuItemFunction> logger)
        {
            _tableStorageService = tableStorageService;
            _logger = logger;
        }

        [Function("CreateMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "post",
                Route = "menu")]
            HttpRequestData req)
        {
            _logger.LogInformation("Creating a new menu item.");

            try
            {
                // Read and deserialize the request body
                var request =
                    await JsonSerializer.DeserializeAsync<CreateMenuItemRequest>(
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

                // Validate Category
                if (string.IsNullOrWhiteSpace(request.Category))
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
                if (string.IsNullOrWhiteSpace(request.SKU))
                {
                    var badRequest =
                        req.CreateResponse(HttpStatusCode.BadRequest);

                    await badRequest.WriteAsJsonAsync(new
                    {
                        error = "SKU is required."
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



                // Create menu item
                var menuItem =
                    await _tableStorageService
                        .CreateMenuItemAsync(request);

                // Map storage model to response DTO
                var responseDto = new MenuItemResponseDto
                {
                    Category = menuItem.PartitionKey,
                    SKU = menuItem.RowKey,
                    Name = menuItem.Name,
                    Description = menuItem.Description,
                    Price = menuItem.Price,
                    IsAvailable = menuItem.IsAvailable
                };

                // Return 201 Created
                var response =
                    req.CreateResponse(HttpStatusCode.Created);

                await response.WriteAsJsonAsync(responseDto);

                return response;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid JSON received while creating menu item.");

                var badRequest =
                    req.CreateResponse(HttpStatusCode.BadRequest);

                await badRequest.WriteAsJsonAsync(new
                {
                    error = "The request body contains invalid JSON."
                });

                return badRequest;
            }
            catch (RequestFailedException ex)
                when (ex.Status == (int)HttpStatusCode.Conflict)
            {
                _logger.LogWarning(
                    ex,
                    "Duplicate menu item attempted.");

                var conflict =
                    req.CreateResponse(HttpStatusCode.Conflict);

                await conflict.WriteAsJsonAsync(new
                {
                    error =
                        "A menu item with the same category and SKU already exists."
                });

                return conflict;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating menu item.");

                var response =
                    req.CreateResponse(
                        HttpStatusCode.InternalServerError);

                await response.WriteAsJsonAsync(new
                {
                    error =
                        "An unexpected error occurred while creating the menu item."
                });

                return response;
            }
        }
    }
}