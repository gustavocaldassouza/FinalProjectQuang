using Microsoft.EntityFrameworkCore;
using FinalProjectQuang.Models;

namespace FinalProjectQuang.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                if (!context.Users.Any())
                {
                    var owner = new User { FullName = "Owner User", Email = "owner@rent.com", Password = "123", Role = UserRole.Owner };
                    var manager = new User { FullName = "Manager Mike", Email = "manager@rent.com", Password = "123", Role = UserRole.Manager };
                    var tenant = new User { FullName = "Tenant Tom", Email = "tenant@rent.com", Password = "123", Role = UserRole.Tenant };
                    context.Users.AddRange(owner, manager, tenant);
                    await context.SaveChangesAsync();
                }

                if (!context.Properties.Any())
                {
                    var owner = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Owner);
                    if (owner != null)
                    {
                        var property = new Property { Name = "Executive Tower One", Address = "123 Luxury Blvd", City = "Montréal", OwnerId = owner.UserId };
                        context.Properties.Add(property);
                        await context.SaveChangesAsync();
                    }
                }

                if (!context.Apartments.Any())
                {
                    var property = await context.Properties.FirstAsync();
                    context.Apartments.AddRange(
                        new Apartment { ApartmentNumber = "101", Rent = 2500, Status = ApartmentStatus.Available, PropertyId = property.PropertyId },
                        new Apartment { ApartmentNumber = "202", Rent = 3500, Status = ApartmentStatus.Available, PropertyId = property.PropertyId },
                        new Apartment { ApartmentNumber = "303", Rent = 4500, Status = ApartmentStatus.Available, PropertyId = property.PropertyId }
                    );
                    await context.SaveChangesAsync();
                }

                if (!context.Appointments.Any())
                {
                    var apt = await context.Apartments.FirstAsync();
                    var tenant = await context.Users.FirstAsync(u => u.Role == UserRole.Tenant);
                    var manager = await context.Users.FirstAsync(u => u.Role == UserRole.Manager);
                    
                    context.Appointments.Add(new Appointment {
                        ApartmentId = apt.ApartmentId,
                        TenantId = tenant.UserId,
                        ManagerId = manager.UserId,
                        AppointmentDate = DateTime.Now.AddDays(2),
                        IsConfirmed = false,
                        Notes = "Demo appointment"
                    });
                    await context.SaveChangesAsync();
                }

                if (!context.Messages.Any())
                {
                    var tenant = await context.Users.FirstAsync(u => u.Role == UserRole.Tenant);
                    var manager = await context.Users.FirstAsync(u => u.Role == UserRole.Manager);

                    context.Messages.Add(new Message {
                        SenderId = tenant.UserId,
                        ReceiverId = manager.UserId,
                        Content = "Hello, I am interested in unit 101.",
                        Timestamp = DateTime.Now.AddHours(-1),
                        IsRead = false
                    });
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
