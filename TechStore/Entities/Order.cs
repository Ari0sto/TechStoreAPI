namespace TechStore.Entities
{
    public enum OrderStatus
    {
        Created = 0,
        Processing = 1,
        Shipped = 2,
        Delivered = 3,
        Cancelled = 4
    }

    public class Order
    {
        public int Id { get; set; }
        public required string UserId { get; set; } // ID пользователя которому принадлежит заказ
        public DateTime OrderDate { get; set; } = DateTime.UtcNow; // дата заказа

        public decimal TotalAmount { get; set; } // сумма заказа

        public OrderStatus Status { get; set; } = OrderStatus.Created;

        // Если в заказе не одна позиция 
        public List<OrderItem> Items { get; set; } = new();
    }
}
