using CoffeeNChill.Functions.Models;
using System.Threading.Tasks;

namespace CoffeeNChill.Functions.Services
{
    public interface IOrderQueueService
    {
        Task SendOrderAsync(Order order);
    }
}