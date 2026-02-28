using Microsoft.EntityFrameworkCore;
using FinalProjectQuang.Models;

namespace FinalProjectQuang.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<Apartment> Apartments { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User - Property (One-to-Many)
            modelBuilder.Entity<Property>()
                .HasOne(p => p.Owner)
                .WithMany(u => u.OwnedProperties)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Property - Apartment (One-to-Many)
            modelBuilder.Entity<Apartment>()
                .HasOne(a => a.Property)
                .WithMany(p => p.Apartments)
                .HasForeignKey(a => a.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Appointment Relationships
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Tenant)
                .WithMany(u => u.TenantAppointments)
                .HasForeignKey(a => a.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Manager)
                .WithMany(u => u.ManagerAppointments)
                .HasForeignKey(a => a.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Apartment)
                .WithMany(ap => ap.Appointments)
                .HasForeignKey(a => a.ApartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Message Relationships
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Property)
                .WithMany()
                .HasForeignKey(m => m.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Decimal Precision for Rent
            modelBuilder.Entity<Apartment>()
                .Property(a => a.Rent)
                .HasPrecision(18, 2);
        }
    }
}
