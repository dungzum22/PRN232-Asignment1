using ShopNew.Models;
using MongoDB.Driver;

namespace ShopNew.Services
{
    public class OrderService
    {
        private readonly IMongoCollection<Order> _orders;
        private readonly IMongoCollection<Product> _products;
        private readonly MongoSequenceService _sequenceService;

        public OrderService(IMongoDatabase database, MongoSequenceService sequenceService)
        {
            _orders = database.GetCollection<Order>("orders");
            _products = database.GetCollection<Product>("products");
            _sequenceService = sequenceService;
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _orders.Find(Builders<Order>.Filter.Empty)
                .Sort(Builders<Order>.Sort.Descending(o => o.CreatedAt))
                .ToListAsync();
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            return await _orders.Find(o => o.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<Order>> GetOrdersByUserIdAsync(int userId)
        {
            return await _orders.Find(o => o.UserId == userId)
                .Sort(Builders<Order>.Sort.Descending(o => o.CreatedAt))
                .ToListAsync();
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            order.Id = await _sequenceService.GetNextSequenceAsync("orders");
            await _orders.InsertOneAsync(order);
            return order;
        }

        public async Task<Order?> UpdateOrderAsync(int id, Order order)
        {
            var existingOrder = await _orders.Find(o => o.Id == id).FirstOrDefaultAsync();
            if (existingOrder == null) return null;

            existingOrder.Status = order.Status;
            existingOrder.PaymentStatus = order.PaymentStatus;
            existingOrder.TotalAmount = order.TotalAmount;

            await _orders.ReplaceOneAsync(o => o.Id == id, existingOrder);
            return existingOrder;
        }

        public async Task<int> GetTotalOrdersCountAsync()
        {
            var count = await _orders.CountDocumentsAsync(Builders<Order>.Filter.Empty);
            return (int)count;
        }

        public async Task<int> GetPendingOrdersCountAsync()
        {
            var count = await _orders.CountDocumentsAsync(o => o.Status == "Pending");
            return (int)count;
        }

        public async Task<int> GetPaidOrdersCountAsync()
        {
            var count = await _orders.CountDocumentsAsync(o => o.Status == "Paid");
            return (int)count;
        }

        public async Task UpdateProductStockAsync(Order order)
        {
            foreach (var item in order.Products)
            {
                var filter = Builders<Product>.Filter.Eq(p => p.Id, item.ProductId);
                var product = await _products.Find(filter).FirstOrDefaultAsync();
                if (product != null)
                {
                    var newStock = Math.Max(0, product.Stock - item.Quantity);
                    var update = Builders<Product>.Update.Set(p => p.Stock, newStock);
                    await _products.UpdateOneAsync(filter, update);
                }
            }
        }
    }
}


