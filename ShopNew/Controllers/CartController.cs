using Microsoft.AspNetCore.Mvc;
using ShopNew.Services;

namespace ShopNew.Controllers
{
    public class CartController : Controller
    {
        private readonly CartService _cartService;
        private readonly ProductService _productService;

        public CartController(CartService cartService, ProductService productService)
        {
            _cartService = cartService;
            _productService = productService;
        }

        public IActionResult Index()
        {
            var cart = _cartService.GetCart();
            ViewBag.Total = _cartService.GetTotal();
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
            {
                if (IsAjaxRequest())
                {
                    return BadRequest(new { error = "Product not found." });
                }
                return RedirectToAction("Index");
            }

            if (product.Stock <= 0)
            {
                if (IsAjaxRequest())
                {
                    return BadRequest(new { error = $"{product.Name} is out of stock." });
                }
                return RedirectToAction("Index");
            }

            if (quantity > product.Stock)
            {
                if (IsAjaxRequest())
                {
                    return BadRequest(new { error = $"Only {product.Stock} item(s) available for {product.Name}." });
                }
                return RedirectToAction("Index");
            }

            _cartService.AddToCart(product, quantity);
            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = true,
                    message = $"{product.Name} added to cart.",
                    totalAmount = _cartService.GetTotal()
                });
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            _cartService.UpdateQuantity(productId, quantity);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            _cartService.RemoveFromCart(productId);
            return RedirectToAction("Index");
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   Request.Headers.ContainsKey("Accept") && Request.Headers["Accept"].ToString().Contains("application/json");
        }
    }
}
