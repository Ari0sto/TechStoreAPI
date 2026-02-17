namespace TechStore.Entities
{
    public class ActionLog
    {
        public int Id { get; set; }

        public string AdminEmail { get; set; } = string.Empty; // Кто сделал

        public string Action { get; set; } = string.Empty; // "Created", "Updated", "Deleted", "StatusChanged"

        public string EntityName { get; set; } = string.Empty; // "Product", "Order"

        public string EntityId { get; set; } = string.Empty; // ID товара или заказа

        public string Details { get; set; } = string.Empty; // "Изменил цену с 100 на 200" или JSON

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    }
}
