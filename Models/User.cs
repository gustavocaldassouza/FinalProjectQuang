using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProjectQuang.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; } // Consider using ASP.NET Core Identity for production projects

        [Required]
        public UserRole Role { get; set; }

        // Navigation Properties
        
        // Properties owned by this user (if Owner)
        public ICollection<Property> OwnedProperties { get; set; }

        // Appointments as a Tenant
        [InverseProperty("Tenant")]
        public ICollection<Appointment> TenantAppointments { get; set; }

        // Appointments as a Manager
        [InverseProperty("Manager")]
        public ICollection<Appointment> ManagerAppointments { get; set; }

        // Messages sent by this user
        [InverseProperty("Sender")]
        public ICollection<Message> SentMessages { get; set; }

        // Messages received by this user
        [InverseProperty("Receiver")]
        public ICollection<Message> ReceivedMessages { get; set; }

        public User()
        {
            OwnedProperties = new HashSet<Property>();
            TenantAppointments = new HashSet<Appointment>();
            ManagerAppointments = new HashSet<Appointment>();
            SentMessages = new HashSet<Message>();
            ReceivedMessages = new HashSet<Message>();
        }
    }
}
