using Microsoft.AspNetCore.Mvc;
using ShopNew.Models;
using ShopNew.Services;
using System.Security.Claims;
using Stripe;

namespace ShopNew.Controllers
{
    public class OrderController : Controller
    {
        private readonly OrderService _orderService;
        private readonly CartService _cartService;
        private readonly PaymentService _paymentService;

        public OrderController(OrderService orderService, CartService cartService, PaymentService paymentService)
        {
            _orderService = orderService;
            _cartService = cartService;
            _paymentService = paymentService;
        }

        public IActionResult Checkout()
        {
            var cart = _cartService.GetCart();
            if (!cart.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            ViewBag.Total = _cartService.GetTotal();
            ViewBag.StripePublishableKey = _paymentService.GetPublishableKey();
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCheckoutSession()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { error = "User not authenticated. Please log in first." });
                }

                var cart = _cartService.GetCart();
                if (!cart.Any())
                {
                    return Json(new { error = "Cart is empty" });
                }

                var totalAmount = _cartService.GetTotal();
                if (totalAmount <= 0)
                {
                    return Json(new { error = "Invalid order amount" });
                }

                // Create order first
                var order = new Order
                {
                    UserId = int.Parse(userId),
                    Products = cart.ToList(),
                    TotalAmount = totalAmount,
                    Status = "Pending",
                    PaymentStatus = "pending",
                    CreatedAt = DateTime.UtcNow
                };

                await _orderService.CreateOrderAsync(order);

                // Create checkout session
                var session = await _paymentService.CreateCheckoutSessionAsync(totalAmount, "usd", order.Id.ToString());

                return Json(new { checkout_url = session.Url });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Checkout Session Error: {ex.Message}");
                return Json(new { error = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(int orderId)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    return Json(new { error = "Order not found" });
                }

                if (!string.IsNullOrEmpty(order.PaymentIntentId))
                {
                    var paymentIntent = await _paymentService.GetPaymentIntentAsync(order.PaymentIntentId);
                    
                    if (paymentIntent.Status == "succeeded")
                    {
                        // Update order status
                        order.Status = "Paid";
                        order.PaymentStatus = "succeeded";
                        await _orderService.UpdateOrderAsync(orderId, order);
                        
                        // Clear cart
                        _cartService.ClearCart();
                        
                        return Json(new { success = true, redirectUrl = Url.Action("OrderSuccess", new { orderId = order.Id }) });
                    }
                }

                return Json(new { error = "Payment not completed" });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        public async Task<IActionResult> OrderSuccess(int orderId, string? session_id)
        {
            try
            {
                if (!string.IsNullOrEmpty(session_id))
                {
                    var session = await _paymentService.GetSessionAsync(session_id);
                    if (session?.PaymentStatus == "paid")
                    {
                        var order = await _orderService.GetOrderByIdAsync(orderId);
                        if (order != null)
                        {
                            order.Status = "Paid";
                            order.PaymentStatus = "succeeded";
                            order.PaymentIntentId = session.PaymentIntentId;
                            await _orderService.UpdateOrderAsync(orderId, order);
                            
                            // Update product stock
                            await _orderService.UpdateProductStockAsync(order);
                            
                            _cartService.ClearCart();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OrderSuccess Error: {ex.Message}");
            }

            ViewBag.OrderId = orderId;
            return View();
        }

        public async Task<IActionResult> History()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var orders = await _orderService.GetOrdersByUserIdAsync(int.Parse(userId));

            return View(orders);
        }
    }
}
