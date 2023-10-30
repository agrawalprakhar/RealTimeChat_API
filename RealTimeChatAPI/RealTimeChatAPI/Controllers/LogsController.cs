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

        // GetLogs Method
        // Description: This method handles the GET request to retrieve logs within a specified time range.
        // It is secured with authorization, allowing only authenticated users to access log data.
        // The method queries the logs repository for logs falling within the provided start and end time,
        // returning the logs as a response if found.
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
