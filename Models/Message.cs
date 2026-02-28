using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProjectQuang.Models
{
    public class Message
    {
        [Key]
        public int MessageId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Foreign Key to Sender (User)
        public int SenderId { get; set; }

        [ForeignKey("SenderId")]
        public User Sender { get; set; }

        // Foreign Key to Receiver (User)
        public int ReceiverId { get; set; }

        [ForeignKey("ReceiverId")]
        public User Receiver { get; set; }

        public bool IsRead { get; set; } = false;
    }
}
