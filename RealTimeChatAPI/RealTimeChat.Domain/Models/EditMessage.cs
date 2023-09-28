using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeChat.Domain.Models
{
    public class EditMessage
    {
        [Required]
        public string Content { get; set; }

    }
}
