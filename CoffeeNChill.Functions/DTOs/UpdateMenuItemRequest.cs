using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace CoffeeNChill.Functions.DTOs
{
    public class UpdateMenuItemRequest
    {
        // Updated menu item name
        public string Name { get; set; } = string.Empty;

        // Updated description
        public string Description { get; set; } = string.Empty;

        // Updated price
        public double Price { get; set; }

        // Updated availability
        public bool IsAvailable { get; set; }
    }
}