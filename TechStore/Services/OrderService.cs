using Microsoft.EntityFrameworkCore;
using TechStore.Data;
using TechStore.DTOs;
using TechStore.Entities;

namespace TechStore.Services
{
    public class OrderService
    {
        private readonly ApplicationDbContext _context;

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Create Order
        public async Task<OrderDto> CreateOrderAsync(string userId, CreateOrderDto dto)
        {
            var order = new Order
            {
                UserId = userId,
                Status = OrderStatus.Created,
                Items = new List<OrderItem>()
            };

            decimal totalAmount = 0;

            foreach (var itemDto in dto.Items)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId);

                // Проверки
                if (product == null)
                    throw new Exception($"Продукт с ID {itemDto.ProductId} не найден");

                if (product.IsDeleted || !product.IsActive)
                    throw new Exception($"Продукт {product.Name} не доступен");

                if (product.Stock < itemDto.Quantity)
                    throw new Exception($"Недостаточное количество товара: {product.Name}");

                // Создание позиции заказа
                var orderItem = new OrderItem
                {
                    Product = product,
                    ProductId = product.Id,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price // <-- Фиксация цены!
                };

                order.Items.Add(orderItem);
                totalAmount += orderItem.Quantity * orderItem.UnitPrice;

                // -- кол-во товара на складе
                product.Stock -= itemDto.Quantity;
            }

            order.TotalAmount = totalAmount;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return await GetOrderByIdAsync(order.Id);
        }

        // Получить заказы пользователя
        public async Task<List<OrderDto>> GetUserOrdersAsync(string userId)
        {
            var orders = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(MapToDto).ToList();
        }

        // Получить все заказы
        // (только для АДМИНА)
        public async Task<List<OrderDto>> GetAllOrdersAsync()
        {
            var orders = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate) // Сначала новые
                .ToListAsync();

            return orders.Select(MapToDto).ToList();
        }

        // Изменить статус заказа
        // (только для АДМИНА)
        public async Task<bool> UpdateStatusAsync(int orderId, string newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled)
            {
                throw new Exception("Нельзя изменить статус выполненого заказа");
            }

            // попытка парсить строку в Enum
            if (Enum.TryParse<OrderStatus>(newStatus, true, out var statusEnum))
            {
                order.Status = statusEnum;
                await _context.SaveChangesAsync();
                return true;
            }

            // если непонятный статус
            return false;
        }

        // Метод для маппинга
        private async Task<OrderDto> GetOrderByIdAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                .FirstAsync(o => o.Id == id);

            return MapToDto(order);
        }

        private static OrderDto MapToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToString(),
                Items = order.Items.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? "Unknown",
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };
        }

         

    }
}

