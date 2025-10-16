using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ShopNew.Models;
using ShopNew.Services;
using System.IO;

namespace ShopNew.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ProductService _productService;

        public ProductsController(ProductService productService) => _productService = productService;

        public async Task<IActionResult> Index(string searchString)
        {
            var products = await _productService.GetAllProductsAsync();
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            ViewData["CurrentFilter"] = searchString;
            return View(products);
        }

        [Authorize(Policy = "AdminOnly")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            // Check if product name already exists
            var products = await _productService.GetAllProductsAsync();
            var exists = products.Any(p => p.Name == product.Name);
            if (exists) ModelState.AddModelError("Name", "A product with this name already exists.");

            if (ModelState.IsValid)
            {
                // Handle file upload
                if (product.ImageFile != null && product.ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "images");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + product.ImageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await product.ImageFile.CopyToAsync(fileStream);
                    }

                    product.ImageUrl = "/uploads/images/" + uniqueFileName;
                }

                await _productService.CreateProductAsync(product);
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Products", "Admin");
                }
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            // Check if product name already exists (excluding current product)
            var products = await _productService.GetAllProductsAsync();
            var nameExists = products.Any(p => p.Name == product.Name && p.Id != id);
            if (nameExists) ModelState.AddModelError("Name", "A product with this name already exists.");

            if (ModelState.IsValid)
            {
                try
                {
                    // Get existing product from database
                    var existingProduct = await _productService.GetProductByIdAsync(id);
                    if (existingProduct == null)
                    {
                        return NotFound();
                    }

                    // Update basic properties
                    existingProduct.Name = product.Name;
                    existingProduct.Description = product.Description;
                    existingProduct.Price = product.Price;
                    existingProduct.Stock = product.Stock;

                    // Handle file upload - only update if new file is provided
                    if (product.ImageFile != null && product.ImageFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "images");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + product.ImageFile.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await product.ImageFile.CopyToAsync(fileStream);
                        }

                        existingProduct.ImageUrl = "/uploads/images/" + uniqueFileName;
                    }
                    // If no new file is provided, keep the existing ImageUrl

                    await _productService.UpdateProductAsync(id, existingProduct);
                    if (User.IsInRole("Admin"))
                    {
                        return RedirectToAction("Products", "Admin");
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch
                { throw; }
            }
            return View(product);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Policy = "AdminOnly")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _productService.DeleteProductAsync(id);
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Products", "Admin");
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
