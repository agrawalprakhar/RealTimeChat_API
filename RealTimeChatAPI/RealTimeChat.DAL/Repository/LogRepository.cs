using RealTimeChat.DAL.Data;
using RealTimeChat.DAL.Repository.IRepository;
using RealTimeChat.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeChat.DAL.Repository
{
    public class LogRepository :  ILogs
    {
        private readonly RealTimeChatContext _context;

        public LogRepository(RealTimeChatContext context)
        {
            _context = context;
        }

        // GetLogs Method
        // Description: This method retrieves log entries based on optional start and end timestamps.
        public List<Logs> GetLogs(DateTime? startTime = null, DateTime? endTime = null)
        {
            var logsQuery = _context.Logs.AsQueryable();

            if (startTime != null)
            {
                logsQuery = logsQuery.Where(l => l.Timestamp >= startTime);
            }

            if (endTime != null)
            {
                logsQuery = logsQuery.Where(l => l.Timestamp <= endTime);
            }

            var logs = logsQuery.ToList();

            logs.ForEach(l =>
            {
                string cleanedString = l.RequestBody.Replace("\r\n", "").Replace(" ", "").Replace("\\", "").Replace("\n", "");
                l.RequestBody = cleanedString;
            });

            return logs;
        }
    }
}
