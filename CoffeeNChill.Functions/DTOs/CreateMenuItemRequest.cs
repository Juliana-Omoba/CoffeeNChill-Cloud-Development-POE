using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CoffeeNChill.Functions.DTOs
{
    public class CreateMenuItemRequest
    {
        // Category of the menu item (used as PartitionKey)
        public string Category { get; set; } = string.Empty;

        // Unique Stock Keeping Unit (used as RowKey)
        public string SKU { get; set; } = string.Empty;

        // Name of the menu item
        public string Name { get; set; } = string.Empty;

        // Description of the menu item
        public string Description { get; set; } = string.Empty;

        // Selling price
        public double Price { get; set; }

        // Indicates if the item is available
        public bool IsAvailable { get; set; }
    }
}
