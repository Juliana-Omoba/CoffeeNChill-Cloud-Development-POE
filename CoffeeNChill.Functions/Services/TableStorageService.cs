using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Functions.DTOs;
using CoffeeNChill.Functions.Interfaces;
using CoffeeNChill.Functions.Models;
using Microsoft.Extensions.Configuration;

namespace CoffeeNChill.Functions.Services
{

    public class TableStorageService : ITableStorageService
    {

        // Add Private Field
        private readonly TableClient _tableClient;

        // Adding Constructor
        public TableStorageService(IConfiguration configuration)
        {
            string connectionString =
                configuration["AzureWebJobsStorage"]
                ?? throw new InvalidOperationException(
                    "AzureWebJobsStorage connection string is missing.");

            _tableClient = new TableClient(connectionString, "MenuItems");

            _tableClient.CreateIfNotExists();
        }

        public async Task<MenuItem> CreateMenuItemAsync(CreateMenuItemRequest request)
        {
            // Create a new MenuItem entity
            MenuItem menuItem = new MenuItem
            {
                PartitionKey = request.Category,
                RowKey = request.SKU,
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                IsAvailable = request.IsAvailable
            };

            // Save to Azure Table Storage
            await _tableClient.AddEntityAsync(menuItem);

            return menuItem;
        }


        public async Task<bool> DeleteMenuItemAsync(string category, string sku)
        {
            try
            {
                // Retrieve the entity to obtain its ETag
                Response<MenuItem> response =
                    await _tableClient.GetEntityAsync<MenuItem>(category, sku);

                // Delete the entity
                await _tableClient.DeleteEntityAsync(
                    category,
                    sku,
                    response.Value.ETag);

                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return false;
            }
        }


        public async Task<List<MenuItem>> GetAllMenuItemsAsync()
        {
            List<MenuItem> menuItems = new List<MenuItem>();

            await foreach (MenuItem item in _tableClient.QueryAsync<MenuItem>())
            {
                menuItems.Add(item);
            }

            return menuItems;
        }


        public async Task<MenuItem?> GetMenuItemAsync(string category, string sku)
        {
            try
            {
                Response<MenuItem> response =
                    await _tableClient.GetEntityAsync<MenuItem>(category, sku);

                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<List<MenuItem>> GetMenuItemsByCategoryAsync(string category)
        {
            List<MenuItem> menuItems = new List<MenuItem>();

            //string filter = $"PartitionKey eq '{category}'";

            //await foreach (MenuItem item in _tableClient.QueryAsync<MenuItem>(filter))
            //{
            //    menuItems.Add(item);
            //}

            string filter =
                TableClient.CreateQueryFilter($"PartitionKey eq {category}");

            await foreach (
                MenuItem item in _tableClient.QueryAsync<MenuItem>(filter))
            {
                menuItems.Add(item);
            }

            return menuItems;
        }


        public async Task<MenuItem?> UpdateMenuItemAsync(
            string category,
            string sku,
            UpdateMenuItemRequest request)
        {
            try
            {
                // Retrieve the existing entity
                Response<MenuItem> response =
                    await _tableClient.GetEntityAsync<MenuItem>(category, sku);

                MenuItem menuItem = response.Value;

                // Update the entity
                menuItem.Name = request.Name;
                menuItem.Description = request.Description;
                menuItem.Price = request.Price;
                menuItem.IsAvailable = request.IsAvailable;

                // Save the updated entity
                await _tableClient.UpdateEntityAsync(
                    menuItem,
                    menuItem.ETag,
                    TableUpdateMode.Replace);

                return menuItem;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }



    }

}
