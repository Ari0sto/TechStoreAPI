namespace TechStore.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public required string Name { get; set; }

        public string? Description { get; set; } // string? - допуск. null значения

        public decimal Price { get; set; } // decimal лучше для финансов и иже с ними
        
        public int Stock { get; set; } // Остаток на складе

        // *Новая фича* Мягкое удаление как сказал Глеб
        public bool IsDeleted { get; set; } = false;

        // Доступность товара (этот параметр меняет админ)
        public bool IsActive { get; set; } = true;

        // Ссылка на картинку
        public string? ImageUrl { get; set; } 
    }
}
