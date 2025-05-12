using FoodOrderBots.Models;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace FoodOrderBots.Services;

public class OrderService
{
    private readonly ConcurrentDictionary<string, FoodOrderDetails> _orders;

    public OrderService()
    {
        _orders = new ConcurrentDictionary<string, FoodOrderDetails>();
    }

    public Task SaveOrderAsync(FoodOrderDetails order)
    {
        _orders[order.OrderId] = order;
        return Task.CompletedTask;
    }

    public Task<FoodOrderDetails> GetOrderAsync(string orderId)
    {
        return Task.FromResult(_orders.TryGetValue(orderId, out var order) ? order : null);
    }

    public Task<bool> CancelOrderAsync(string orderId)
    {
        return Task.FromResult(_orders.TryRemove(orderId, out _));
    }
}
