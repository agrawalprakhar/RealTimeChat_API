using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeChat.Domain.Models
{
    public class Logs
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string IP { get; set; }
        public string RequestBody { get; set; }
        public string Username { get; set; }
    }
}
