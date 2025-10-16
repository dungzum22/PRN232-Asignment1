using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ShopNew.Models;
using ShopNew.Services;
using System.Security.Claims;

namespace ShopNew.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : Controller
    {
        private readonly UserService _userService;
        private readonly ProductService _productService;
        private readonly OrderService _orderService;

        public AdminController(UserService userService, ProductService productService, OrderService orderService)
        {
            _userService = userService;
            _productService = productService;
            _orderService = orderService;
        }

        public async Task<IActionResult> Index()
        {
            var stats = new
            {
                TotalUsers = await _userService.GetTotalUsersCountAsync(),
                TotalProducts = (await _productService.GetAllProductsAsync()).Count,
                TotalOrders = await _orderService.GetTotalOrdersCountAsync(),
                PendingOrders = await _orderService.GetPendingOrdersCountAsync(),
                PaidOrders = await _orderService.GetPaidOrdersCountAsync()
            };

            ViewBag.Stats = stats;
            // Recent orders for dashboard table (latest 8)
            var recentOrders = (await _orderService.GetAllOrdersAsync()).Take(8).ToList();
            ViewBag.RecentOrders = recentOrders;
            return View();
        }

        public async Task<IActionResult> Users()
        {
            var users = await _userService.GetAllUsersAsync();
            return View(users);
        }

        public async Task<IActionResult> Orders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return View(orders);
        }

        public async Task<IActionResult> Products()
        {
            var products = await _productService.GetAllProductsAsync();
            return View(products);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserRole(int userId, string newRole)
        {
            if (newRole != UserRoles.Admin && newRole != UserRoles.User)
            {
                return Json(new { success = false, message = "Invalid role" });
            }

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == currentUserId)
            {
                return Json(new { success = false, message = "Cannot change your own role" });
            }

            var result = await _userService.UpdateUserRoleAsync(userId, newRole);

            if (result)
            {
                return Json(new { success = true, message = "Role updated successfully" });
            }

            return Json(new { success = false, message = "Failed to update role" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == currentUserId)
            {
                return Json(new { success = false, message = "Cannot delete your own account" });
            }

            var result = await _userService.DeleteUserAsync(userId);
            
            if (result)
            {
                return Json(new { success = true, message = "User deleted successfully" });
            }

            return Json(new { success = false, message = "Failed to delete user" });
        }
    }
}
