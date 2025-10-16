using ShopNew.Models;
using MongoDB.Driver;

namespace ShopNew.Services
{
    public class ProductService
    {
        private readonly IMongoCollection<Product> _products;
        private readonly MongoSequenceService _sequenceService;

        public ProductService(IMongoDatabase database, MongoSequenceService sequenceService)
        {
            _products = database.GetCollection<Product>("products");
            _sequenceService = sequenceService;
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _products.Find(Builders<Product>.Filter.Empty).ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            product.Id = await _sequenceService.GetNextSequenceAsync("products");
            await _products.InsertOneAsync(product);
            return product;
        }

        public async Task<Product?> UpdateProductAsync(int id, Product product)
        {
            var existingProduct = await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (existingProduct == null) return null;

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.Stock = product.Stock;
            existingProduct.ImageUrl = product.ImageUrl;

            await _products.ReplaceOneAsync(p => p.Id == id, existingProduct);
            return existingProduct;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var result = await _products.DeleteOneAsync(p => p.Id == id);
            return result.DeletedCount > 0;
        }
    }
}