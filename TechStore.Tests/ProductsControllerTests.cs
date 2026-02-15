using Microsoft.AspNetCore.Hosting; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

            context.Products.Add(new Entities.Product
            {
                Name = "Test",
                Price = 10,
                IsActive = true,
                CategoryId = 1
            });
            await context.SaveChangesAsync();
            var product = await context.Products.FirstAsync();

            var service = new ProductService(context);

            var controller = new ProductsController(service, null, null);

            var result = await controller.GetById(product.Id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnProduct = Assert.IsType<ProductDto>(okResult.Value);
            Assert.Equal("Test", returnProduct.Name);
        }

        [Fact]
        public async Task GetById_Should_Return_NotFound_When_Product_Missing()
        {
            var context = GetInMemoryDbContext();
            var service = new ProductService(context);

            var controller = new ProductsController(service, null, null);

            var result = await controller.GetById(999);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Create_Should_Return_Created()
        {
            var context = GetInMemoryDbContext();

            // Предварительно добавим категорию, иначе Service упадет с ошибкой "Категория не найдена"
            context.Categories.Add(new Entities.Category { Id = 1, Name = "Electronics" });
            await context.SaveChangesAsync();

            var service = new ProductService(context);

            var controller = new ProductsController(service, null, null);

            var newProduct = new CreateProductDto
            {
                Name = "New",
                Price = 100,
                Stock = 5,
                CategoryId = 1 // Обязательно указываем существующую категорию!
            };

            var result = await controller.Create(newProduct);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var productDto = Assert.IsType<ProductDto>(createdResult.Value);
            Assert.Equal("New", productDto.Name);
        }
    }
}
