using Microsoft.EntityFrameworkCore;
using TechStore.Data;
using TechStore.DTOs;
using TechStore.Services;
using Xunit;

namespace TechStore.Tests
{
    public class ProductServiceTests
    {
        // Метод для создания "чистой" БД
        // (чтобы не засорять реальную БД-ху тестовыми данными)
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // unique name for tests
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task CreateAsync_Should_Add_Product_To_Database()
        {
            // 1) созд. чистой базы
            var context = GetInMemoryDbContext();

            // созд сервис и перед. ему эту базу
            var service = new ProductService(context);

            var newProductDto = new CreateProductDto()
            {
                Name = "Test Phone",
                Description = "Best phone",
                Price = 1000,
                Stock = 10
            };

            var result = await service.CreateAsync(newProductDto);

            // 2) Проверка
            // проверка что результат вернулся
            Assert.NotNull(result);
            Assert.Equal("Test Phone", result.Name);
            Assert.NotEqual(0, result.Id); // must been ID

            // 3) Проверка что товар реально лежит в базе
            var productInDb = await context.Products.FindAsync(result.Id);
            Assert.NotNull(productInDb);
            Assert.Equal(1000, productInDb.Price);
        }

        [Fact]
        public async Task GetAllAsync_Should_Return_Only_Active_Products()
        {
            var context = GetInMemoryDbContext();

            // Add test data
            context.Products.Add(new Entities.Product { Name = "Active Product", IsActive = true, IsDeleted = false, Price = 10 });
            context.Products.Add(new Entities.Product { Name = "Deleted Product", IsActive = true, IsDeleted = true, Price = 10 }); // Этот не должен попасть
            context.Products.Add(new Entities.Product { Name = "Inactive Product", IsActive = false, IsDeleted = false, Price = 10 }); // И этот не должен
            await context.SaveChangesAsync();

            var service = new ProductService(context);

            var result = await service.GetAllAsync(1, 10, null, null);

            Assert.Equal(1, result.Items.Count);
            var firstItem = result.Items[0];
        }

        [Fact]
        public async Task UpdateAsync_Should_UpdateProduct_When_ProductExists()
        {
            var context = GetInMemoryDbContext();
            var product = new Entities.Product { Name = "Old Name", Price = 10, Stock = 5, IsActive = true, IsDeleted = false };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new ProductService(context);
            var updateDto = new UpdateProductDto
            {
                Name = "New Name",
                Price = 20,
                Stock = 15,
                IsActive = true,
                IsDeleted = false
            };

            var result = await service.UpdateAsync(product.Id, updateDto);

            Assert.NotNull(result);
            Assert.Equal("New Name", result.Name);
            Assert.Equal(20, result.Price);

            // Проверяем в БД
            var dbProduct = await context.Products.FindAsync(product.Id);
            Assert.Equal("New Name", dbProduct.Name);
        }

        [Fact]
        public async Task UpdateAsync_Should_ReturnNull_When_ProductNotFound()
        {
            var context = GetInMemoryDbContext(); // Пустая база
            var service = new ProductService(context);

            
            var result = await service.UpdateAsync(999, new UpdateProductDto { Name = "Ghost" });

            Assert.Null(result); // Должен вернуть null
        }

        [Fact]
        public async Task DeleteAsync_Should_SoftDelete_Product()
        {
            var context = GetInMemoryDbContext();
            var product = new Entities.Product { Name = "To Delete", IsDeleted = false };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new ProductService(context);

            var result = await service.DeleteAsync(product.Id);

            Assert.True(result);

            // Главная проверка - товар остался в БД, но IsDeleted стал true
            var dbProduct = await context.Products.FindAsync(product.Id);
            Assert.NotNull(dbProduct);
            Assert.True(dbProduct.IsDeleted);
        }

        [Fact]
        public async Task DeleteAsync_Should_ReturnFalse_When_ProductNotFound()
        {
            
            var context = GetInMemoryDbContext();
            var service = new ProductService(context);

            
            var result = await service.DeleteAsync(999); // удаление несуществующего ID

            Assert.False(result);
        }

        [Fact]
        public async Task Create_Should_ThrowException_When_PriceIsNegative()
        {
            
            var context = GetInMemoryDbContext();
            var service = new ProductService(context);

            // Попытка создать товар с ценой -100
            var badProduct = new CreateProductDto
            {
                Name = "Bad Product",
                Price = -100,
                Stock = 10
            };

            
            var ex = await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.CreateAsync(badProduct);
            });

            Assert.Equal("Цена не может быть отрицательной", ex.Message);
        }

    }
}
