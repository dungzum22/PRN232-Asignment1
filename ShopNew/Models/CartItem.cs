using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace ShopNew.Models
{
    public class CartItem
    {
        [Key]
        [BsonId]
        [BsonElement("_id")]
        public int Id { get; set; }
        
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
    }
}
