using System.Net;
using TechStore.Middleware;

namespace TechStore.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Передача запроса к контроллерам
                await _next(context);
            }
            catch (Exception ex)
            {
                // Перехват ошибок
                _logger.LogError(ex, ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";

            // По умолчанию 500 статус-код
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new ApiError
            {
                StatusCode = context.Response.StatusCode,
                Message = "Internal Server Error from the custom middleware.",
                // Если режим разработки (Development), показываем детали ошибки
                Details = _env.IsDevelopment() ? ex.StackTrace?.ToString() : null
            };

            response.Message = ex.Message;

            if (ex.Message.Contains("Недостаточно товара на складе") || ex.Message.Contains("не найдено"))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusCode = 400;
            }

            await context.Response.WriteAsync(response.ToString());
        }

    }
}
