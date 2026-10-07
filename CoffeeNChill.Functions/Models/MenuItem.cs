using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.Functions.Models
{
    public class MenuItem : ITableEntity
    {
        // Azure Table Storage Partition Key
        public string PartitionKey { get; set; } = string.Empty;

        // Azure Table Storage Row Key
        public string RowKey { get; set; } = string.Empty;

        // Menu Item Name
        public string Name { get; set; } = string.Empty;

        // Description of the menu item
        public string Description { get; set; } = string.Empty;

        // Selling price
        public double Price { get; set; }

        // Indicates whether the item is available
        public bool IsAvailable { get; set; }

        // Automatically maintained by Azure Table Storage
        public DateTimeOffset? Timestamp { get; set; }

        // Entity Tag used for concurrency
        public ETag ETag { get; set; }
    }
}