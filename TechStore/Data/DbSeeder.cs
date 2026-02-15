using TechStore.Entities;
using Microsoft.EntityFrameworkCore;

namespace TechStore.Data
{
    public static class DbSeeder
    {
        public static async Task SeedProductsAsync(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Если товаров нет - добавляем пару тестовых
                if (!context.Products.Any())
                {
                    Console.WriteLine("База пуста. Создаем тестовые товары...");

                    // Важно: предполагается что категории 1 и 2 уже созданы SQL скриптом!
                    var products = new List<Product>
                    {
                        new Product
                        {
                            Name = "Тестовый Смартфон",
                            Description = "Создан автоматически при старте",
                            Price = 9999,
                            Stock = 5,
                            CategoryId = 1, // Привязываем к Смартфонам
                            ImageUrl = "/images/default.png", // Заглушка, если файла нет
                            IsActive = true
                        },
                        new Product
                        {
                            Name = "Тестовый Ноутбук",
                            Description = "Мощный и тихий",
                            Price = 45000,
                            Stock = 3,
                            CategoryId = 2, // Привязываем к Ноутбукам
                            ImageUrl = "/images/default.png",
                            IsActive = true
                        }
                    };

                    context.Products.AddRange(products);
                    await context.SaveChangesAsync();
                    Console.WriteLine("--- Тестовые товары добавлены ---");
                }
            }
        }
    }
}