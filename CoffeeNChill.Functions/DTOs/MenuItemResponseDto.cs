namespace CoffeeNChill.Functions.DTOs
{
    public class MenuItemResponseDto
    {
        // Menu item category
        public string Category { get; set; } = string.Empty;

        // Unique Stock Keeping Unit
        public string SKU { get; set; } = string.Empty;

        // Menu item name
        public string Name { get; set; } = string.Empty;

        // Menu item description
        public string Description { get; set; } = string.Empty;

        // Selling price
        public double Price { get; set; }

        // Indicates whether the menu item is available
        public bool IsAvailable { get; set; }
    }
}