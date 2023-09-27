using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeChat.Domain.Models
{
    public class LoginResponse
    {
        public string Token { get; set; }

        public UserProfile Profile { get; set; }
    }
}
