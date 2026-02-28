using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FinalProjectQuang.Data;
using FinalProjectQuang.Models;
using System.Security.Claims;

namespace FinalProjectQuang.Controllers
{
    [Authorize(Roles = "Manager,Owner")]
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- Property CRUD ---

        public async Task<IActionResult> Properties(string searchString)
        {
            ViewBag.CurrentFilter = searchString;
            var query = _context.Properties.Include(p => p.Owner).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.Name.Contains(searchString) 
                                     || p.Address.Contains(searchString) 
                                     || p.City.Contains(searchString));
            }

            var properties = await query.ToListAsync();
            return View(properties);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var property = await _context.Properties
                .Include(p => p.Owner)
                .Include(p => p.Apartments)
                .FirstOrDefaultAsync(m => m.PropertyId == id);
            if (property == null) return NotFound();
            return View(property);
        }

        public IActionResult CreateProperty()
        {
            ViewData["OwnerId"] = new SelectList(_context.Users.Where(u => u.Role == UserRole.Owner), "UserId", "FullName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProperty([Bind("PropertyId,Name,Address,City,OwnerId")] Property property)
        {
            // Remove navigation properties from validation
            ModelState.Remove("Owner");
            ModelState.Remove("Apartments");

            if (ModelState.IsValid)
            {
                _context.Add(property);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Properties));
            }
            ViewData["OwnerId"] = new SelectList(_context.Users.Where(u => u.Role == UserRole.Owner), "UserId", "FullName", property.OwnerId);
            return View(property);
        }

        public async Task<IActionResult> EditProperty(int? id)
        {
            if (id == null) return NotFound();
            var property = await _context.Properties.FindAsync(id);
            if (property == null) return NotFound();
            ViewData["OwnerId"] = new SelectList(_context.Users.Where(u => u.Role == UserRole.Owner), "UserId", "FullName", property.OwnerId);
            return View(property);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProperty(int id, [Bind("PropertyId,Name,Address,City,OwnerId")] Property property)
        {
            if (id != property.PropertyId) return NotFound();

            // Remove navigation properties from validation
            ModelState.Remove("Owner");
            ModelState.Remove("Apartments");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(property);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PropertyExists(property.PropertyId)) return NotFound();
                    else throw;
                }
                TempData["StatusMessage"] = "Property details updated successfully.";
                return RedirectToAction(nameof(Properties));
            }
            ViewData["OwnerId"] = new SelectList(_context.Users.Where(u => u.Role == UserRole.Owner), "UserId", "FullName", property.OwnerId);
            return View(property);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProperty(int id)
        {
            var property = await _context.Properties
                .Include(p => p.Apartments)
                .ThenInclude(a => a.Appointments)
                .FirstOrDefaultAsync(p => p.PropertyId == id);

            if (property == null) return NotFound();

            // 1. Manually remove and save appointments first
            foreach (var apartment in property.Apartments)
            {
                if (apartment.Appointments.Any())
                {
                    _context.Appointments.RemoveRange(apartment.Appointments);
                }
            }
            await _context.SaveChangesAsync();

            // 2. Clear property link for related messages (keep history but categorize as general)
            var messages = await _context.Messages.Where(m => m.PropertyId == id).ToListAsync();
            foreach (var msg in messages)
            {
                msg.PropertyId = null;
            }
            await _context.SaveChangesAsync();

            // 3. Now delete the property (which will cascade to apartments)
            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Property has been successfully decommissioned.";
            return RedirectToAction(nameof(Properties));
        }

        private bool PropertyExists(int id)
        {
            return _context.Properties.Any(e => e.PropertyId == id);
        }

        // --- Apartment CRUD & Status ---

        public async Task<IActionResult> Apartments(int? propertyId)
        {
            var apartmentsQuery = _context.Apartments.Include(a => a.Property).AsQueryable();
            if (propertyId.HasValue)
            {
                apartmentsQuery = apartmentsQuery.Where(a => a.PropertyId == propertyId.Value);
            }
            return View(await apartmentsQuery.ToListAsync());
        }

        public IActionResult CreateApartment(int? propertyId)
        {
            ViewData["PropertyId"] = new SelectList(_context.Properties, "PropertyId", "Name", propertyId);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateApartment([Bind("ApartmentId,ApartmentNumber,Rent,Status,PropertyId")] Apartment apartment)
        {
            ModelState.Remove("Property");
            ModelState.Remove("Appointments");

            if (ModelState.IsValid)
            {
                _context.Add(apartment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Apartments), new { propertyId = apartment.PropertyId });
            }
            ViewData["PropertyId"] = new SelectList(_context.Properties, "PropertyId", "Name", apartment.PropertyId);
            return View(apartment);
        }

        public async Task<IActionResult> EditApartment(int? id)
        {
            if (id == null) return NotFound();
            var apartment = await _context.Apartments.FindAsync(id);
            if (apartment == null) return NotFound();
            ViewData["PropertyId"] = new SelectList(_context.Properties, "PropertyId", "Name", apartment.PropertyId);
            return View(apartment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditApartment(int id, [Bind("ApartmentId,ApartmentNumber,Rent,Status,PropertyId")] Apartment apartment)
        {
            if (id != apartment.ApartmentId) return NotFound();

            ModelState.Remove("Property");
            ModelState.Remove("Appointments");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(apartment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ApartmentExists(apartment.ApartmentId)) return NotFound();
                    else throw;
                }
                TempData["StatusMessage"] = "Apartment inventory updated.";
                return RedirectToAction(nameof(Apartments));
            }
            ViewData["PropertyId"] = new SelectList(_context.Properties, "PropertyId", "Name", apartment.PropertyId);
            return View(apartment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteApartment(int id)
        {
            var apartment = await _context.Apartments
                .Include(a => a.Appointments)
                .FirstOrDefaultAsync(a => a.ApartmentId == id);

            if (apartment == null) return NotFound();

            int? propertyId = apartment.PropertyId;

            // 1. Remove appointments first to satisfy RESTRICT constraint
            if (apartment.Appointments != null && apartment.Appointments.Any())
            {
                _context.Appointments.RemoveRange(apartment.Appointments);
                await _context.SaveChangesAsync();
            }

            // 2. Remove the apartment unit
            _context.Apartments.Remove(apartment);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Apartment unit removed from inventory.";
            return RedirectToAction(nameof(Apartments), new { propertyId });
        }

        private bool ApartmentExists(int id)
        {
            return _context.Apartments.Any(e => e.ApartmentId == id);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateApartmentStatus(int apartmentId, ApartmentStatus status)
        {
            var apartment = await _context.Apartments.FindAsync(apartmentId);
            if (apartment != null)
            {
                apartment.Status = status;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Apartments));
        }

        // --- Appointments ---

        public async Task<IActionResult> Appointments()
        {
            var managerEmail = User.Identity?.Name;
            var manager = await _context.Users.FirstOrDefaultAsync(u => u.Email == managerEmail);

            var appointments = await _context.Appointments
                .Include(a => a.Tenant)
                .Include(a => a.Apartment)
                .ThenInclude(ap => ap.Property)
                .Where(a => manager == null || a.ManagerId == manager.UserId)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            return View(appointments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmAppointment(int appointmentId)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment != null)
            {
                appointment.IsConfirmed = true;
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Appointment confirmed successfully.";
            }
            return RedirectToAction(nameof(Appointments));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RescheduleAppointment(int appointmentId, DateTime newDate)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null) return NotFound();

            if (newDate < DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "New appointment date must be in the future.";
                return RedirectToAction(nameof(Appointments));
            }

            appointment.AppointmentDate = newDate;
            appointment.IsConfirmed = false; // Reset confirmation since date changed
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Appointment rescheduled successfully.";
            return RedirectToAction(nameof(Appointments));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAppointment(int appointmentId)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Appointment has been cancelled and removed.";
            }
            return RedirectToAction(nameof(Appointments));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportToOwner(int propertyId, string reportContent)
        {
            var managerEmail = User.Identity?.Name;
            var manager = await _context.Users.FirstOrDefaultAsync(u => u.Email == managerEmail);
            
            var property = await _context.Properties.FindAsync(propertyId);
            if (property == null || manager == null) return NotFound();

            if (string.IsNullOrWhiteSpace(reportContent))
            {
                TempData["ErrorMessage"] = "Report content cannot be empty.";
                return RedirectToAction(nameof(Details), new { id = propertyId });
            }

            var report = new Message
            {
                SenderId = manager.UserId,
                ReceiverId = property.OwnerId,
                PropertyId = propertyId,
                Content = $"[PROPERTY REPORT]: {reportContent}",
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(report);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Event report has been successfully transmitted to the property owner.";
            return RedirectToAction(nameof(Details), new { id = propertyId });
        }

        // --- Messages ---

        public async Task<IActionResult> Messages()
        {
            var managerEmail = User.Identity?.Name;
            var manager = await _context.Users.FirstOrDefaultAsync(u => u.Email == managerEmail);

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Include(m => m.Property)
                .Where(m => manager == null || m.ReceiverId == manager.UserId || m.SenderId == manager.UserId)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();

            ViewBag.CurrentManagerId = manager?.UserId;
            return View(messages);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyMessage(int originalMessageId, string replyContent, int? propertyId)
        {
            var originalMessage = await _context.Messages.FindAsync(originalMessageId);
            if (originalMessage == null) return NotFound();

            var managerEmail = User.Identity?.Name;
            var manager = await _context.Users.FirstOrDefaultAsync(u => u.Email == managerEmail);

            if (manager == null) return Unauthorized();

            var reply = new Message
            {
                SenderId = manager.UserId,
                ReceiverId = originalMessage.SenderId,
                PropertyId = propertyId,
                Content = $"RE: {originalMessage.Content}\n\n{replyContent}",
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            originalMessage.IsRead = true;
            _context.Messages.Add(reply);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Reply sent successfully.";
            return RedirectToAction(nameof(Messages));
        }
    }
}
