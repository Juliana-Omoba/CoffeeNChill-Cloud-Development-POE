using Azure;
using Azure.Data.Tables;
using System;

namespace CoffeeNChill.Functions.Models
{
    public class OrderEntity : ITableEntity
    {

        // ============================================================
        // POE PART 2 RUBRIC:
        // Orders Azure Table entity.
        //
        // PartitionKey = order date
        // RowKey       = OrderId
        //
        // Stores the order and its lifecycle status:
        // Received → Preparing → Ready → Collected
        // ============================================================


        // Azure Table Storage required properties
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // CoffeeNChill Order properties
        public string OrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;

        // Store multiple SKUs as a JSON string
        public string SelectedItemSKUs { get; set; } = string.Empty;

        public double TotalPrice { get; set; }

        public DateTimeOffset OrderTimestamp { get; set; }

        // RUBRIC: Initial order lifecycle status.
        public string Status { get; set; } = "Received";

        // Records the most recent status update.
        public DateTimeOffset LastUpdated { get; set; }
    }
}