using Microsoft.EntityFrameworkCore;
using TechStore.Data;
using TechStore.DTOs;
using TechStore.Entities;

namespace TechStore.Services
{
    public class ProductService
    {
        // ЛОГИКА - ВОЗВРАТ ТОЛЬКО АКТИВНЫХ ТОВАРОВ И КОТОРЫЕ НЕ УДАЛЕНЫ

        private readonly ApplicationDbContext _context;

        public ProductService(ApplicationDbContext context)
        {
            _context = context;
        }

        // получить все товары (для покупателей)
        public async Task<List<ProductDto>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await _context.Products
                .Where(p => !p.IsDeleted && p.IsActive) // filtered by SoftDelete
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Stock = p.Stock,
                    ImageUrl = p.ImageUrl
                })
                .ToListAsync();
        }

        // получить только ОДИН товар
        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.IsActive);

            if (product == null) return null;

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock
            };
        }

        // создать товар (для АДМИНА)
        public async Task<ProductDto> CreateAsync(CreateProductDto dto)
        {
            if (dto.Price < 0) throw new Exception("Цена не может быть отрицательной");
            if (dto.Stock < 0) throw new Exception("Количество не может быть отрицательным");

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock,
                IsActive = true,
                IsDeleted = false
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // возврат созданного объекта
            // с ID
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock
            };
        }

        // обновить товар
        public async Task<ProductDto?> UpdateAsync(int id, UpdateProductDto dto)
        {
            if (dto.Price < 0) throw new Exception("Цена не может быть отрицательной");
            if (dto.Stock < 0) throw new Exception("Количество не может быть отрицательным");
            
            var product = await _context.Products.FindAsync(id);
            if (product == null) return null;

            // обновляем поля
            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            product.IsActive = dto.IsActive;
            product.IsDeleted = dto.IsDeleted;

            product.ImageUrl = dto.ImageUrl;

            await _context.SaveChangesAsync();

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                ImageUrl = product.ImageUrl
            };
        }

        // НОВАЯ ФИЧА
        // SOFT DELETE
        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;

            product.IsDeleted = true; // Метка удалено, но не удалено с БД
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
