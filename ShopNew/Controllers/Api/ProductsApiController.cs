using Microsoft.AspNetCore.Mvc;
using ShopNew.Models;
using MongoDB.Driver;

namespace ShopNew.Controllers.Api
{
    [ApiController]
    [Route("api/products")]
    public class ProductsApiController : ControllerBase
    {
        private readonly IMongoCollection<Product> _products;
        public ProductsApiController(IMongoCollection<Product> products) => _products = products;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetAll() => await _products.Find(Builders<Product>.Filter.Empty).ToListAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetById(string id)
        {
            var p = await _products.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (p == null) return NotFound();
            return p;
        }

        [HttpPost]
        public async Task<ActionResult<Product>> Create(Product product)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            await _products.InsertOneAsync(product);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, Product product)
        {
            if (id != product.Id) return BadRequest();
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var result = await _products.ReplaceOneAsync(p => p.Id == id, product);
            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _products.DeleteOneAsync(p => p.Id == id);
            if (result.DeletedCount == 0) return NotFound();
            return NoContent();
        }
    }
}

