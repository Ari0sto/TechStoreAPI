using Microsoft.EntityFrameworkCore;
using TechStore.Data;
using TechStore.DTOs;
using TechStore.Entities;
using TechStore.Services;
using Xunit;

namespace TechStore.Tests
{
    public class OrderServiceTests
    {
        // такая же тема с БД
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
        public async Task CreateOrder_Should_ThrowException_When_Quantity_Is_Negative()
        {
            var context = GetInMemoryDbContext();
            var product = new Entities.Product { Name = "Test", Price = 100, Stock = 10, IsActive = true };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new OrderService(context);

            // создание "плохого" заказа с отрицательным количеством
            var orderDto = new CreateOrderDto
            {
                Items = new List<CreateOrderItemDto>
        {
            new CreateOrderItemDto { ProductId = product.Id, Quantity = -5 }
        }
            };

            var ex = await Assert.ThrowsAsync<Exception>(async () =>
            {
                await service.CreateOrderAsync("user1", orderDto);
            });

            Assert.Contains("должно быть больше 0", ex.Message);
        }

        [Fact]
        public async Task CreateOrder_Should_Throw_Exception_When_Stock_Is_Not_Enough()
        {
            var context = GetInMemoryDbContext();

            // add product which are only 5 pieces in stock
            var product = new Product()
            {
                Name = "IPhone",
                Price = 1000,
                Stock = 5, // ONLY 5 in stock
                IsActive = true,
                IsDeleted = false
            };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new OrderService(context);

            // Create order on 10 pcs
            // (i have only 5!)
            var orderDto = new CreateOrderDto
            {
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto { ProductId = product.Id, Quantity = 10}
                }
            };

            var exception = await Assert.ThrowsAsync<Exception>(async () =>
            {
                await service.CreateOrderAsync("user123", orderDto);
            });

            Assert.Contains("Недостаточное количество товара", exception.Message);
        }

        [Fact]
        public async Task CreateOrder_Should_CreateOrder_ReduceStock_And_CalculateTotal()
        {
            var context = GetInMemoryDbContext();

            // create product
            var product = new Product
            {
                Name = "Laptop",
                Price = 100,
                Stock = 10,
                IsActive = true,
                IsDeleted = false
            };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new OrderService(context);

            // запрос на 2 ноута
            var orderDto = new CreateOrderDto()
            {
                Items = new List<CreateOrderItemDto>()
                {
                    new CreateOrderItemDto { ProductId = product.Id, Quantity = 2 }
                }
            };

            var resultOrder = await service.CreateOrderAsync("user_test_id", orderDto);

            // Проверки
            // проверка самого заказа
            Assert.NotNull(resultOrder);
            Assert.Equal("Created", resultOrder.Status); // Статус должен быть Created


            // проверка правильности счета 2 * 100 = 200
            Assert.Equal(200, resultOrder.TotalAmount);

            // проверка что уменьшился склад
            var productInDb = await context.Products.FindAsync(product.Id);
            Assert.Equal(8, productInDb.Stock); // Было 10 купили 2 ожидается что будет 8
        }

        [Fact]
        public async Task CreateOrder_Should_ThrowException_When_ProductNotFound()
        {
            
            var context = GetInMemoryDbContext();
            var service = new OrderService(context);
            var orderDto = new CreateOrderDto
            {
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto { ProductId = 999, Quantity = 1 } // Такого ID нет
                }
            };

            var ex = await Assert.ThrowsAsync<Exception>(async () =>
            {
                await service.CreateOrderAsync("user1", orderDto);
            });

            Assert.Contains("не найден", ex.Message);
        }

        [Fact]
        public async Task CreateOrder_Should_ThrowException_When_ProductIsNotActive()
        {
            
            var context = GetInMemoryDbContext();
            var product = new Entities.Product { Name = "Old Phone", IsActive = false, IsDeleted = false, Stock = 10, Price = 100 };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new OrderService(context);
            var orderDto = new CreateOrderDto
            {
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto { ProductId = product.Id, Quantity = 1 }
                }
            };

            
            var ex = await Assert.ThrowsAsync<Exception>(async () =>
            {
                await service.CreateOrderAsync("user1", orderDto);
            });

            Assert.Contains("не доступен", ex.Message);
        }

        [Fact]
        public async Task GetUserOrders_Should_ReturnOnlyUserOrders()
        {
            
            var context = GetInMemoryDbContext();
            // Заказ пользователя А
            context.Orders.Add(new Entities.Order { UserId = "UserA", TotalAmount = 100, OrderDate = DateTime.Now, Status = Entities.OrderStatus.Created });
            // Заказ пользователя Б
            context.Orders.Add(new Entities.Order { UserId = "UserB", TotalAmount = 200, OrderDate = DateTime.Now, Status = Entities.OrderStatus.Created });
            await context.SaveChangesAsync();

            var service = new OrderService(context);

            
            var result = await service.GetUserOrdersAsync("UserA");

            
            Assert.Single(result); // Должен найтись только 1 заказ
            Assert.Equal(100, result[0].TotalAmount); // Именно заказ пользователя А
        }

        [Fact]
        public async Task UpdateStatus_Should_UpdateStatus_When_Valid()
        {
            
            var context = GetInMemoryDbContext();
            var order = new Entities.Order { Status = Entities.OrderStatus.Created, UserId = "u1" };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new OrderService(context);

            
            // Замена статуса на "Shipped" (Отправлен)
            var result = await service.UpdateStatusAsync(order.Id, "Shipped");

            
            Assert.True(result);

            var dbOrder = await context.Orders.FindAsync(order.Id);
            Assert.Equal(Entities.OrderStatus.Shipped, dbOrder.Status);
        }

        [Fact]
        public async Task UpdateStatus_Should_ThrowException_When_OrderIsCompleted()
        {
            var context = GetInMemoryDbContext();
            // Заказ уже доставлен (Delivered)
            var order = new Entities.Order { Status = Entities.OrderStatus.Delivered, UserId = "u1" };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new OrderService(context);

            // Попытка вернуть его "В обработку" (Created)
            var ex = await Assert.ThrowsAsync<Exception>(async () =>
            {
                await service.UpdateStatusAsync(order.Id, "Created");
            });

            Assert.Contains("Нельзя изменить статус", ex.Message);
        }
    }
}
