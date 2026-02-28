using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProjectQuang.Models
{
    public class Property
    {
        [Key]
        public int PropertyId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        // Foreign Key to User (Owner)
        public int OwnerId { get; set; }
        
        [ForeignKey("OwnerId")]
        public User Owner { get; set; } = null!;

        // Navigation Property for Apartments
        public ICollection<Apartment> Apartments { get; set; }

        public Property()
        {
            Apartments = new HashSet<Apartment>();
        }
    }
}
