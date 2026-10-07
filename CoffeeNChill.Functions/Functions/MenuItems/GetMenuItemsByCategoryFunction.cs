using CoffeeNChill.Functions.DTOs;
using CoffeeNChill.Functions.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CoffeeNChill.Functions.Functions.MenuItems
{
    public class GetMenuItemsByCategoryFunction
    {
        private readonly ITableStorageService _tableStorageService;
        private readonly ILogger<GetMenuItemsByCategoryFunction> _logger;

        public GetMenuItemsByCategoryFunction(
            ITableStorageService tableStorageService,
            ILogger<GetMenuItemsByCategoryFunction> logger)
        {
            _tableStorageService = tableStorageService;
            _logger = logger;
        }

        [Function("GetMenuItemsByCategory")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "menu/category/{category}")]
            HttpRequestData req,
            string category)
        {
            _logger.LogInformation(
                "Retrieving menu items for category: {Category}",
                category);


            try
            {
                if (string.IsNullOrWhiteSpace(category))
                {
                    var badRequest =
                        req.CreateResponse(HttpStatusCode.BadRequest);

                    //await badRequest.WriteStringAsync(
                     //   "Category is required.");

                    await badRequest.WriteAsJsonAsync(new
                    {
                        error = "Category is required."
                    });

                    return badRequest;
                }

                var menuItems =
                    await _tableStorageService
                        .GetMenuItemsByCategoryAsync(category);



                //await response.WriteAsJsonAsync(menuItems);

                var responseDtos =
                menuItems.Select(menuItem => new MenuItemResponseDto
                {
                    Category = menuItem.PartitionKey,
                    SKU = menuItem.RowKey,
                    Name = menuItem.Name,
                    Description = menuItem.Description,
                    Price = menuItem.Price,
                    IsAvailable = menuItem.IsAvailable
                }).ToList();

                var response =
                    req.CreateResponse(HttpStatusCode.OK);

                await response.WriteAsJsonAsync(responseDtos);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving menu items for category: {Category}",
                    category);

                var response =
                    req.CreateResponse(
                        HttpStatusCode.InternalServerError);

                //await response.WriteStringAsync(
                //   "An error occurred while retrieving menu items by category.");

                await response.WriteAsJsonAsync(new
                {
                    error = "An unexpected error occurred while retrieving menu items by category."
                });

                return response;
            }
        }
    }
}