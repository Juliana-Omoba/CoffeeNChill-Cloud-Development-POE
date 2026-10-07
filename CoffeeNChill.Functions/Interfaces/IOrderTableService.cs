using CoffeeNChill.Functions.Models;
using System.Threading.Tasks;

namespace CoffeeNChill.Functions.Interfaces
{
    public interface IOrderTableService
    {
        Task CreateOrderAsync(OrderEntity order);

        Task UpdateOrderStatusAsync(
            string partitionKey,
            string orderId,
            string status);
    }
}