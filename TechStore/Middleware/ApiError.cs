using System.Text.Json;

namespace TechStore.Middleware
{
    public class ApiError
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; } 

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
