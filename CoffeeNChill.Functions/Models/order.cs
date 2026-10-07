using System;
using System.Collections.Generic;

namespace CoffeeNChill.Functions.Models
{

    // ============================================================
    // POE PART 2:
    // Queue message domain model.
    //
    // An Order object is populated by the producer, serialized
    // to JSON, placed on order-processing-queue, and reconstructed
    // by ProcessOrderQueue.
    // ============================================================


    public class Order
    {
        public string OrderId { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public List<string> SelectedItemSKUs { get; set; } = new();

        public decimal TotalPrice { get; set; }

        public DateTimeOffset OrderTimestamp { get; set; }
    }
}