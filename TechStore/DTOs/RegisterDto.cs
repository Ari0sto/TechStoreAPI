namespace TechStore.DTOs
{
    public class RegisterDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }

        // Роль можно указать сразу (ДЛЯ ТЕСТА)
        public string Role { get; set; } = "Customer";
    }
}
