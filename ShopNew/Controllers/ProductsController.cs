using Microsoft.AspNetCore.Mvc;
using ShopNew.Models;
using System.IO;
using MongoDB.Driver;
using MongoDB.Bson;

namespace ShopNew.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IMongoCollection<Product> _products;

        public ProductsController(IMongoCollection<Product> products) => _products = products;

        public async Task<IActionResult> Index(string searchString)
        {
            var filter = string.IsNullOrWhiteSpace(searchString)
                ? Builders<Product>.Filter.Empty
                : Builders<Product>.Filter.Regex(p => p.Name, new BsonRegularExpression(searchString, "i"));
            var products = await _products.Find(filter).ToListAsync();
            ViewData["CurrentFilter"] = searchString;
            return View(products);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            // Check if product name already exists
            var exists = await _products.Find(p => p.Name == product.Name).AnyAsync();
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

                // Prevent MongoDB from attempting to serialize the uploaded form file
                product.ImageFile = null;
                await _products.InsertOneAsync(product);
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var product = await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            // Check if product name already exists (excluding current product)
            var nameExists = await _products.Find(p => p.Name == product.Name && p.Id != id).AnyAsync();
            if (nameExists) ModelState.AddModelError("Name", "A product with this name already exists.");

            if (ModelState.IsValid)
            {
                try
                {
                    // Get existing product from database
                    var existingProduct = await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
                    if (existingProduct == null)
                    {
                        return NotFound();
                    }

                    // Update basic properties
                    existingProduct.Name = product.Name;
                    existingProduct.Description = product.Description;
                    existingProduct.Price = product.Price;

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

                    var replaceResult = await _products.ReplaceOneAsync(p => p.Id == id, existingProduct);
                    if (replaceResult.MatchedCount == 0) return NotFound();
                    return RedirectToAction(nameof(Index));
                }
                catch
                { throw; }
            }
            return View(product);
        }

        public async Task<IActionResult> Details(string id)
        {
            var product = await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        public async Task<IActionResult> Delete(string id)
        {
            var product = await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var result = await _products.DeleteOneAsync(p => p.Id == id);
            return RedirectToAction(nameof(Index));
        }
    }
}
