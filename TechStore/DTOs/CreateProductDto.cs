using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TechStore.DTOs
{
    public class CreateProductDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(0.01, 1000000)]
        public decimal Price { get; set; }

        [Range(0, 10000)]
        public int Stock { get; set; }

        public int CategoryId { get; set; }

        public IFormFile? Image { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string? ImageUrl { get; set; }
    }
}
