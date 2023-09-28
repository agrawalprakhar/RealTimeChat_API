using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeChat.Domain.Models
{
    public class sendMessageRequest
    {
        [Key]
        [Required]
        public string ReceiverId { get; set; }
        [Required]
        public string Content { get; set; }
    }
}
