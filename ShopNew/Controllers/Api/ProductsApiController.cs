using Microsoft.AspNetCore.Mvc;
using ShopNew.Models;
using ShopNew.Services;

namespace ShopNew.Controllers.Api
{
    [ApiController]
    [Route("api/products")]
    public class ProductsApiController : ControllerBase
    {
        private readonly ProductService _productService;
        public ProductsApiController(ProductService productService) => _productService = productService;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetAll() => await _productService.GetAllProductsAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetById(int id)
        {
            var p = await _productService.GetProductByIdAsync(id);
            if (p == null) return NotFound();
            return p;
        }

        [HttpPost]
        public async Task<ActionResult<Product>> Create(Product product)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var created = await _productService.CreateProductAsync(product);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Product product)
        {
            if (id != product.Id) return BadRequest();
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var updated = await _productService.UpdateProductAsync(id, product);
            if (updated == null) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _productService.DeleteProductAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}

