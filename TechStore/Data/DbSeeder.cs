using TechStore.Entities;
using System.Text.Json;
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

                // 1. ПРОВЕРКА: Если товаров нет, грузим их
                if (!context.Products.Any())
                {
                    Console.WriteLine("База пуста. Загружаем товары из API...");
                    using var httpClient = new HttpClient();
                    var categories = new[] { "smartphones", "laptops" };

                    foreach (var cat in categories)
                    {
                        try
                        {
                            var response = await httpClient.GetStringAsync($"https://dummyjson.com/products/category/{cat}");
                            var data = JsonSerializer.Deserialize<DummyRoot>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            if (data?.Products != null)
                            {
                                foreach (var item in data.Products)
                                {
                                    var product = new Product
                                    {
                                        Name = item.Title,
                                        Description = item.Description,
                                        Price = item.Price,
                                        Stock = item.Stock,
                                        ImageUrl = item.Thumbnail,
                                        IsActive = true
                                    };
                                    context.Products.Add(product);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка загрузки категории {cat}: {ex.Message}");
                        }
                    }

                    await context.SaveChangesAsync();
                    Console.WriteLine("--- Новые товары сохранены ---");
                }

                // 2. ЛЕЧЕНИЕ КАРТИНОК
                var productsToFix = context.Products
                    .Where(p => p.ImageUrl == null || p.ImageUrl == "" || p.ImageUrl.Contains("dummyjson"))
                    .ToList();

                if (productsToFix.Any())
                {
                    Console.WriteLine($"Найдено {productsToFix.Count} товаров с плохими картинками. Исправляем...");

                    foreach (var product in productsToFix)
                    {
                        // Ставим картинку iPhone
                        product.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/f/f5/IPhone_15_Pro_Vector.svg/200px-IPhone_15_Pro_Vector.svg.png";
                    }

                    // Сохраняем исправления
                    await context.SaveChangesAsync();
                    Console.WriteLine("--- КАРТИНКИ УСПЕШНО ОБНОВЛЕНЫ ---");
                }
                else
                {
                    Console.WriteLine("Все картинки в порядке, исправлять нечего.");
                }
            }
        }

        private class DummyRoot { public List<DummyProduct> Products { get; set; } }
        private class DummyProduct
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public int Stock { get; set; }
            public string Thumbnail { get; set; }
        }
    }
}