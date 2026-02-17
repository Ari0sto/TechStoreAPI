using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.DTOs;
using TechStore.Data;
using TechStore.Entities;
using TechStore.Services;
using System.Security.Claims;

namespace TechStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _productService;

        private readonly ILogger<ProductsController> _logger; // Логгер для отладки

        // для работы с файлами (картинками)
        private readonly IWebHostEnvironment _appEnvironment;

        // log service
        private readonly ActionLogService _logService;

        public ProductsController(ProductService productService, ILogger<ProductsController> logger, IWebHostEnvironment appEnvironment, ActionLogService logService)
        {
            _productService = productService;
            _logger = logger;
            _appEnvironment = appEnvironment;
            _logService = logService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<ProductDto>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int size = 10,
            [FromQuery] int? categoryId = null)
        {
            // Проверка правильности знч.
            if (page < 1) page = 1;
            if (size < 1 || size > 100) size = 10;
            if (size > 50) size = 50;

            var result = await _productService.GetAllAsync(page, size, categoryId);
            return Ok(result);
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
        public async Task<ActionResult<ProductDto>> Create([FromForm] CreateProductDto dto)
        {
            try
            {
                if (dto.Image != null)
                {
                    dto.ImageUrl = await SaveImage(dto.Image);
                }

                else
                {
                    // Если картинки нет, заглушка
                    dto.ImageUrl = "/images/default.png";
                }

                var product = await _productService.CreateAsync(dto);

                var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity.Name;
                await _logService.LogActionAsync(email, "Created", "Product", product.Id.ToString(), $"Name: {product.Name}, Price: {product.Price}");
                

                return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании товара");
                return StatusCode(500, "Ошибка сервера: " + ex.Message);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductDto>> Update(int id, [FromForm] UpdateProductDto dto)
        {
            // add log
            _logger.LogInformation("Запрос на обновление товара ID: {Id}.", id);

            try
            {
                if (dto.Image != null)
                {
                    dto.ImageUrl = await SaveImage(dto.Image);
                }

                // Вызов сервиса
                var updatedProduct = await _productService.UpdateAsync(id, dto);
                

                if (updatedProduct == null)
                {
                    _logger.LogWarning("Товар ID: {Id} не найден при попытке обновления", id);
                    return NotFound(new { Message = $"Продукт с ID {id} не найден" });
                }

                var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity.Name;
                await _logService.LogActionAsync(email, "Updated", "Product", id.ToString(), $"Updated Name: {dto.Name}, Price: {dto.Price}");

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

            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity.Name;
            await _logService.LogActionAsync(email, "Deleted", "Product", id.ToString(), "Soft deleted product");

            return NoContent();
        }

        // Вспомогательный метод для сохранения изображения
        private async Task<string> SaveImage(IFormFile image)
        {
            // Создаем уникальное имя файла
            string uniqueName = Guid.NewGuid().ToString() + "_" + image.FileName;

            // Путь для сохранения (wwwroot/images)
            string relativePath = "/images/products/" + uniqueName;

            // Полный путь на сервере
            string fullPath = _appEnvironment.WebRootPath + relativePath;

            // Создаем папку, если ее нет
            var directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Копируем файл
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            // Возвращаем путь для сохранения в БД
            return relativePath;

        }
    }
}
