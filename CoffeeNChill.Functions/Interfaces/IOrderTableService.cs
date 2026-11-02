
using CoffeeNChill.Functions.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoffeeNChill.Functions.Interfaces
{
    // ============================================================
    // POE PART 2 + PART 3:
    // AZURE TABLE STORAGE ORDER SERVICE CONTRACT
    // ============================================================
    //
    // Existing methods are preserved:
    //
    // 1. CreateOrderAsync
    // 2. UpdateOrderStatusAsync
    //
    // New retrieval methods:
    //
    // 3. GetAllOrdersAsync
    // 4. GetOrderAsync
    //
    // These methods allow authorised API Gateway users
    // to retrieve orders stored in Azure Table Storage.
    // ============================================================

    public interface IOrderTableService
    {
        // --------------------------------------------------------
        // EXISTING: Save a newly queued order.
        // --------------------------------------------------------

        Task CreateOrderAsync(OrderEntity order);

        // --------------------------------------------------------
        // EXISTING: Update an order's lifecycle status.
        // --------------------------------------------------------

        Task UpdateOrderStatusAsync(
            string partitionKey,
            string orderId,
            string status);

        // --------------------------------------------------------
        // NEW: Retrieve all stored orders.
        // --------------------------------------------------------

        Task<List<OrderEntity>> GetAllOrdersAsync();

        // --------------------------------------------------------
        // NEW: Retrieve a specific order.
        //
        // The order is identified by:
        //
        // PartitionKey = OrderDate (yyyy-MM-dd)
        // RowKey       = OrderId
        // --------------------------------------------------------

        Task<OrderEntity?> GetOrderAsync(
            string partitionKey,
            string orderId);
    }
}
