using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Controllers;
using TechStore.Data;
using TechStore.DTOs;
using TechStore.Services;
using Xunit;

namespace TechStore.Tests
{
    public class OrdersControllerTests
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

        // Преобразование обычного контроллера в "авторизиванный"
        private void SetupControllerWithUser(OrdersController controller, string userId, string role)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Email, "test@example.com")
            }, "mock"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task Create_Should_ReturnOk_When_OrderCreated()
        {
            var context = GetInMemoryDbContext();
            // нужен товар, чтобы создать заказ
            var product = new Entities.Product { Name = "Phone", Price = 100, Stock = 10, IsActive = true };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new OrderService(context);

            var logService = new ActionLogService(context);

            var controller = new OrdersController(service, logService);

            // ВАЖНО!
            // "Входим" как пользователь "User1" - customer
            SetupControllerWithUser(controller, "User1", "Customer");

            var orderDto = new CreateOrderDto
            {
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto { ProductId = product.Id, Quantity = 1 }
                }
            };

            var result = await controller.Create(orderDto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedOrder = Assert.IsType<OrderDto>(okResult.Value);

            Assert.Equal(100, returnedOrder.TotalAmount);
        }

        [Fact]
        public async Task GetMyOrders_Should_Return_Only_Mine()
        {
            var context = GetInMemoryDbContext();

            // Заказ МОЙ
            context.Orders.Add(new Entities.Order { UserId = "Me", TotalAmount = 500, Status = Entities.OrderStatus.Created, OrderDate = DateTime.Now });
            // Заказ ЧУЖОЙ
            context.Orders.Add(new Entities.Order { UserId = "SomeoneElse", TotalAmount = 1000, Status = Entities.OrderStatus.Created, OrderDate = DateTime.Now });
            await context.SaveChangesAsync();

            var service = new OrderService(context);

            var logService = new ActionLogService(context);

            var controller = new OrdersController(service, logService);

            // Вход как "Me"
            SetupControllerWithUser(controller, "Me", "Customer");

            var result = await controller.GetMyOrders();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var orders = Assert.IsType<List<OrderDto>>(okResult.Value);

            Assert.Single(orders); // Должен вернуть только 1 заказ
            Assert.Equal(500, orders[0].TotalAmount); // И это должен быть заказ на 500
        }

        [Fact]
        public async Task UpdateStatus_Should_Work_For_Admin()
        {
            var context = GetInMemoryDbContext();
            var order = new Entities.Order { UserId = "Client", Status = Entities.OrderStatus.Created };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new OrderService(context);

            var logService = new ActionLogService(context);

            var controller = new OrdersController(service, logService);

            // Вход как "Admin"
            SetupControllerWithUser(controller, "AdminUser", "Admin");

            // Смена статуса на "Delivered"
            var result = await controller.UpdateStatus(order.Id, "Delivered");

            Assert.IsType<NoContentResult>(result); // 204 No Content

            var dbOrder = await context.Orders.FindAsync(order.Id);
            Assert.Equal(Entities.OrderStatus.Delivered, dbOrder.Status); // Статус должен быть обновлен
        }
    }
}
