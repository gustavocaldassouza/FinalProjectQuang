using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProjectQuang.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Foreign Key to Tenant (User)
        public int TenantId { get; set; }

        [ForeignKey("TenantId")]
        public User Tenant { get; set; } = null!;

        // Foreign Key to Manager (User)
        public int ManagerId { get; set; }

        [ForeignKey("ManagerId")]
        public User Manager { get; set; } = null!;

        // Foreign Key to Apartment
        public int ApartmentId { get; set; }

        [ForeignKey("ApartmentId")]
        public Apartment Apartment { get; set; } = null!;

        [Required]
        public bool IsConfirmed { get; set; } = false;
    }
}
