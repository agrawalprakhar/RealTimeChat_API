﻿using DataAnnotationsExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeChat.Domain.Models
{
    public  class UserRegistration
    {
        public string Name { get; set; }

        [Email]
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
