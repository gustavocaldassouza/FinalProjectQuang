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

        public async Task<IActionResult> AvailableApartments()
        {
            var apartments = await _context.Apartments
                .Include(a => a.Property)
                .Where(a => a.Status == ApartmentStatus.Available)
                .ToListAsync();

            return View(apartments);
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
                appointment.Apartment = null!;
                appointment.Tenant = null!;
                appointment.Manager = null!;

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Your viewing appointment request has been registered successfully.";
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
                TempData["StatusMessage"] = "Your message has been transmitted to our concierge team.";
                return RedirectToAction("AvailableApartments");
            }

            ViewBag.Managers = new SelectList(_context.Users.Where(u => u.Role == UserRole.Manager), "UserId", "FullName", receiverId);
            return View();
        }
        // 4. View scheduled appointments for the tenant
        public async Task<IActionResult> MyAppointments()
        {
            var tenantEmail = User.Identity?.Name;
            var tenant = await _context.Users.FirstOrDefaultAsync(u => u.Email == tenantEmail);

            if (tenant == null) return RedirectToAction("Login", "Account");

            var appointments = await _context.Appointments
                .Include(a => a.Apartment)
                .ThenInclude(ap => ap.Property)
                .Where(a => a.TenantId == tenant.UserId)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            return View(appointments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var tenantEmail = User.Identity?.Name;
            var tenant = await _context.Users.FirstOrDefaultAsync(u => u.Email == tenantEmail);

            if (tenant == null) return RedirectToAction("Login", "Account");

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.AppointmentId == id && a.TenantId == tenant.UserId);

            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Your viewing appointment has been successfully cancelled.";
            }

            return RedirectToAction("MyAppointments");
        }

        // 5. View sent messages (Inbox) for the tenant
        public async Task<IActionResult> MyMessages()
        {
            var tenantEmail = User.Identity?.Name;
            var tenant = await _context.Users.FirstOrDefaultAsync(u => u.Email == tenantEmail);

            if (tenant == null) return RedirectToAction("Login", "Account");

            var messages = await _context.Messages
                .Include(m => m.Receiver)
                .Where(m => m.SenderId == tenant.UserId)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();

            return View(messages);
        }
    }
}
