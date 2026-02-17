using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStore.Services;

namespace TechStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "BusinessOwner")] // Только бизнес-владельцы могут видеть логи
    public class LogsController : ControllerBase
    {
        private readonly ActionLogService _logService;

        public LogsController(ActionLogService logService)
        {
            _logService = logService;
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs()
        {
            var logs = await _logService.GetLogsAsync();
            return Ok(logs);
        }
    }
}
