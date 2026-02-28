using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProjectQuang.Models
{
    public class Apartment
    {
        [Key]
        public int ApartmentId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ApartmentNumber { get; set; } = string.Empty;

        [Required]
        public decimal Rent { get; set; }

        [Required]
        public ApartmentStatus Status { get; set; }

        // Foreign Key to Property
        public int PropertyId { get; set; }

        [ForeignKey("PropertyId")]
        public Property Property { get; set; } = null!;

        // Navigation Property for Appointments
        public ICollection<Appointment> Appointments { get; set; }

        public Apartment()
        {
            Appointments = new HashSet<Appointment>();
            Status = ApartmentStatus.Available;
        }
    }
}
