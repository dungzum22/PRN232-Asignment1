using ShopNew.Models;
using ShopNew.Data;
using Microsoft.EntityFrameworkCore;

namespace ShopNew.Services
{
    public class OrderService
    {
        private readonly ApplicationDbContext _context;

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            return await _context.Orders.FindAsync(id);
        }

        public async Task<List<Order>> GetOrdersByUserIdAsync(int userId)
        {
            return await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order?> UpdateOrderAsync(int id, Order order)
        {
            var existingOrder = await _context.Orders.FindAsync(id);
            if (existingOrder == null) return null;

            existingOrder.Status = order.Status;
            existingOrder.PaymentStatus = order.PaymentStatus;
            existingOrder.TotalAmount = order.TotalAmount;

            await _context.SaveChangesAsync();
            return existingOrder;
        }

        public async Task<int> GetTotalOrdersCountAsync()
        {
            return await _context.Orders.CountAsync();
        }

        public async Task<int> GetPendingOrdersCountAsync()
        {
            return await _context.Orders.CountAsync(o => o.Status == "Pending");
        }

        public async Task<int> GetPaidOrdersCountAsync()
        {
            return await _context.Orders.CountAsync(o => o.Status == "Paid");
        }

        public async Task UpdateProductStockAsync(Order order)
        {
            foreach (var item in order.Products)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.Stock = Math.Max(0, product.Stock - item.Quantity);
                }
            }
            await _context.SaveChangesAsync();
        }
    }
}


