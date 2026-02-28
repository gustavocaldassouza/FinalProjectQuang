using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FinalProjectQuang.Data;
using FinalProjectQuang.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace FinalProjectQuang.Controllers
{
    [Authorize(Roles = "Tenant")]
    public class TenantController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TenantController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Search and view a list of available apartments
        public async Task<IActionResult> AvailableApartments(string searchString, decimal? minRent, decimal? maxRent)
        {
            var apartments = _context.Apartments
                .Include(a => a.Property)
                .Where(a => a.Status == ApartmentStatus.Available);

            if (!string.IsNullOrEmpty(searchString))
            {
                apartments = apartments.Where(s => s.Property.Name.Contains(searchString) || s.Property.City.Contains(searchString));
            }

            if (minRent.HasValue)
            {
                apartments = apartments.Where(a => a.Rent >= minRent.Value);
            }

            if (maxRent.HasValue)
            {
                apartments = apartments.Where(a => a.Rent <= maxRent.Value);
            }

            return View(await apartments.ToListAsync());
        }

        // 2. Submit a request to book an appointment
        public async Task<IActionResult> BookAppointment(int apartmentId)
        {
            var apartment = await _context.Apartments
                .Include(a => a.Property)
                .FirstOrDefaultAsync(a => a.ApartmentId == apartmentId);

            if (apartment == null || apartment.Status != ApartmentStatus.Available)
            {
                return NotFound();
            }

            // Get a list of managers to choose from (in a real app, maybe assigned to the property)
            ViewBag.Managers = new SelectList(_context.Users.Where(u => u.Role == UserRole.Manager), "UserId", "FullName");
            
            var appointment = new Appointment
            {
                ApartmentId = apartmentId,
                Apartment = apartment,
                AppointmentDate = DateTime.Now.AddDays(1)
            };

            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointment(Appointment appointment)
        {
            // Set the current user as the tenant
            var tenantEmail = User.Identity?.Name;
            var tenant = await _context.Users.FirstOrDefaultAsync(u => u.Email == tenantEmail);
            
            if (tenant != null)
            {
                appointment.TenantId = tenant.UserId;
                appointment.IsConfirmed = false;
                
                // Clear navigation properties to avoid EF issues on insert
                appointment.Apartment = null;
                appointment.Tenant = null;
                appointment.Manager = null;

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction("AvailableApartments");
            }
            
            return View(appointment);
        }

        // 3. Send a simple text message to a property manager
        public IActionResult SendMessage(int? managerId)
        {
            ViewBag.Managers = new SelectList(_context.Users.Where(u => u.Role == UserRole.Manager), "UserId", "FullName", managerId);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int receiverId, string content)
        {
            var senderEmail = User.Identity?.Name;
            var sender = await _context.Users.FirstOrDefaultAsync(u => u.Email == senderEmail);

            if (sender != null && !string.IsNullOrEmpty(content))
            {
                var message = new Message
                {
                    SenderId = sender.UserId,
                    ReceiverId = receiverId,
                    Content = content,
                    Timestamp = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();
                return RedirectToAction("AvailableApartments");
            }

            return View();
        }
    }
}
