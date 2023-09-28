using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeChat.Domain.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; }

   
        [Required]
        public string ReceiverId { get; set; }
        [Required]
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }


        // Navigation properties
     
    }
}
