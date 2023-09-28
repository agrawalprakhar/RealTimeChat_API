using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RealTimeChat.DAL.Repository;
using RealTimeChat.DAL.Repository.IRepository;

namespace RealTimeChatAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogsController : ControllerBase
    {
        private readonly ILogs _logsRepository;

        public LogsController(ILogs logsRepository)
        {
            _logsRepository = logsRepository;
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetLogs([FromQuery] DateTime? startTime = null, [FromQuery] DateTime? endTime = null)
        {
            try
            {
                var logs = _logsRepository.GetLogs(startTime, endTime);

                if (logs.Count == 0)
                {
                    return NotFound(new { error = "No logs found." });
                }

                return Ok(new { Logs = logs });
            }
            catch (Exception ex)
            {
                return BadRequest($"Bad Request: {ex.Message}");
            }
        }
    }
}
