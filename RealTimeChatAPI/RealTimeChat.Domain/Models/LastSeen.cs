using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeChat.Domain.Models
{
     public  class LastSeen
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}



