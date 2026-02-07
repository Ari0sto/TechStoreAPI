using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Controllers;
using TechStore.Data;
using TechStore.DTOs;
using TechStore.Services;
using Xunit;

namespace TechStore.Tests
{
    public class ProductsControllerTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task GetById_Should_Return_Ok_With_Product()
        {
            var context = GetInMemoryDbContext();
            context.Products.Add(new Entities.Product { Name = "Test", Price = 10, IsActive = true });
            await context.SaveChangesAsync();
            var product = await context.Products.FirstAsync(); // Получаем ID

            var service = new ProductService(context);
            var controller = new ProductsController(service);

            var result = await controller.GetById(product.Id);

            // Проверка что результат - OkObjectResult (200 статус)
            var okResult = Assert.IsType<OkObjectResult>(result.Result);

            var returnProduct = Assert.IsType<ProductDto>(okResult.Value);
            Assert.Equal("Test", returnProduct.Name);
        }

        [Fact]
        public async Task GetById_Should_Return_NotFound_When_Product_Missing()
        {
            
            var context = GetInMemoryDbContext();
            var service = new ProductService(context);
            var controller = new ProductsController(service);

            
            var result = await controller.GetById(999); // Несуществующий ID

            
            // Проверка что вернулся NotFoundResult (код 404)
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Create_Should_Return_Created()
        {
            var context = GetInMemoryDbContext();
            var service = new ProductService(context);
            var controller = new ProductsController(service);

            var newProduct = new CreateProductDto
            {
                Name = "New",
                Price = 100,
                Stock = 5
            };

            var result = await controller.Create(newProduct);

            // CreatedAtActionResult означает код 201 Created
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var productDto = Assert.IsType<ProductDto>(createdResult.Value);
            Assert.Equal("New", productDto.Name);
        }
    }
}
