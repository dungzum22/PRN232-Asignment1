using System.ComponentModel.DataAnnotations;

namespace ShopNew.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        
        public int UserId { get; set; }
        public List<CartItem> Products { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public string? PaymentIntentId { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
