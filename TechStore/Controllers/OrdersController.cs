using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStore.DTOs;
using TechStore.Services;

namespace TechStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // <-- Доступ только с токеном
    public class OrdersController : ControllerBase
    {
        private readonly OrderService _orderService;
        private readonly ActionLogService _logService;

        public OrdersController(OrderService orderService, ActionLogService logService)
        {
            _orderService = orderService;
            _logService = logService;
        }

        [HttpPost]
        public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // Если внутри метода возникнет Exception, он полетит 
            // прямо в Middleware.
            var order = await _orderService.CreateOrderAsync(userId, dto);
            return Ok(order);
        }

        [HttpGet("my-orders")]
        public async Task<ActionResult<List<OrderDto>>> GetMyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var orders = await _orderService.GetUserOrdersAsync(userId);
            return Ok(orders);
        }

        // НИЖЕ ЗАПРОСЫ ДЛЯ АДМИНА

        [HttpGet] // GET /api/orders
        [Authorize(Roles = "Admin")] // Только админ видит всё
        public async Task<ActionResult<List<OrderDto>>> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(orders);
        }

        [HttpPatch("{id}/status")] // PATCH /api/orders/status
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var result = await _orderService.UpdateStatusAsync(id, status);
            if (!result) return BadRequest("Неправильный статус заказа, или он не найден");

            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity.Name;
            await _logService.LogActionAsync(email, "StatusChanged", "Order", id.ToString(), $"New Status: {status}");

            return NoContent();
        }
    }
}
