using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CoffeeNChill.Functions.DTOs;
using CoffeeNChill.Functions.Models;

namespace CoffeeNChill.Functions.Interfaces
{
    public interface ITableStorageService
    {
        Task<MenuItem> CreateMenuItemAsync(CreateMenuItemRequest request);

        Task<List<MenuItem>> GetAllMenuItemsAsync();

        Task<List<MenuItem>> GetMenuItemsByCategoryAsync(string category);

        Task<MenuItem?> GetMenuItemAsync(string category, string sku);

        Task<MenuItem?> UpdateMenuItemAsync(
            string category,
            string sku,
            UpdateMenuItemRequest request);

        Task<bool> DeleteMenuItemAsync(
            string category,
            string sku);
    }
}
