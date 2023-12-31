﻿using System;
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
        [ForeignKey("Sender")]
        [Required]
        public string SenderId { get; set; }

        [ForeignKey("Receiver")]
        [Required]
        public string ReceiverId { get; set; }
        [Required]
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }

        public bool IsRead { get; set; }

        // Navigation properties
        public User Sender { get; set; }
        public User Receiver { get; set; }

    }
}
