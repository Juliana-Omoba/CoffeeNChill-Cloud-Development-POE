using CoffeeNChill.Functions.DTOs;
using CoffeeNChill.Functions.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CoffeeNChill.Functions.Functions.MenuItems
{
    public class GetAllMenuItemsFunction
    {
        private readonly ITableStorageService _tableStorageService;
        private readonly ILogger<GetAllMenuItemsFunction> _logger;

        public GetAllMenuItemsFunction(
            ITableStorageService tableStorageService,
            ILogger<GetAllMenuItemsFunction> logger)
        {
            _tableStorageService = tableStorageService;
            _logger = logger;
        }

        [Function("GetAllMenuItems")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "menu")]
            HttpRequestData req)
        {
            _logger.LogInformation(
                "Retrieving all menu items.");

            try
            {
                var menuItems =
                    await _tableStorageService
                        .GetAllMenuItemsAsync();

                // Map all storage models to response DTOs
                var responseDtos =
                    menuItems.Select(menuItem =>
                        new MenuItemResponseDto
                        {
                            Category = menuItem.PartitionKey,
                            SKU = menuItem.RowKey,
                            Name = menuItem.Name,
                            Description = menuItem.Description,
                            Price = menuItem.Price,
                            IsAvailable = menuItem.IsAvailable
                        })
                    .ToList();

                var response =
                    req.CreateResponse(HttpStatusCode.OK);

                await response.WriteAsJsonAsync(responseDtos);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving all menu items.");

                var response =
                    req.CreateResponse(
                        HttpStatusCode.InternalServerError);

                await response.WriteAsJsonAsync(new
                {
                    error =
                        "An unexpected error occurred while retrieving menu items."
                });

                return response;
            }
        }
    }
}