using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.DTOs;
using TechStore.Entities;
using TechStore.Services;

namespace TechStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _productService;

        private readonly ILogger<ProductsController> _logger; // Логгер для отладки

        public ProductsController(ProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<ProductDto>>> GetAll([FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            // Проверка правильности знч.
            if (page < 1) page = 1;
            if (size < 1 || size > 100) size = 10;

            var products = await _productService.GetAllAsync(page, size);
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetById(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] // Только админ!
        public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
        {
            var product = await _productService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductDto dto)
        {
            // add log
            _logger.LogInformation("Запрос на обновление товара ID: {Id}. Новая картинка: {Url}", id, dto.ImageUrl);

            try
            {
                // Вызов сервиса
                var updatedProduct = await _productService.UpdateAsync(id, dto);
                //var updatedProduct = await _productService.UpdateAsync(id, dto);

                //if (updatedProduct == null)
                //    return NotFound(new { Message = $"Продукт с ID {id} не найден" });

                //return Ok(updatedProduct);

                if (updatedProduct == null)
                {
                    _logger.LogWarning("Товар ID: {Id} не найден при попытке обновления", id);
                    return NotFound(new { Message = $"Продукт с ID {id} не найден" });
                }

                return Ok(updatedProduct);
            }

            catch (Exception ex)
            {

                _logger.LogError(ex, "Ошибка при обновлении товара ID: {Id}", id);
                return StatusCode(500, "Внутренняя ошибка сервера: " + ex.Message);
            }

            
        }



        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Только админ!
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _productService.DeleteAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }
    }
}
