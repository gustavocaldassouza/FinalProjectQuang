using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FinalProjectQuang.Data;
using FinalProjectQuang.Models;
using System.Security.Claims;

namespace FinalProjectQuang.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- Property CRUD ---

        public async Task<IActionResult> Properties()
        {
            var properties = await _context.Properties.Include(p => p.Owner).ToListAsync();
            return View(properties);
        }

        public async Task<IActionResult> PropertyDetails(int? id)
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
            if (ModelState.IsValid)
            {
                _context.Update(property);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Properties));
            }
            ViewData["OwnerId"] = new SelectList(_context.Users.Where(u => u.Role == UserRole.Owner), "UserId", "FullName", property.OwnerId);
            return View(property);
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
            if (ModelState.IsValid)
            {
                _context.Update(apartment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Apartments));
            }
            ViewData["PropertyId"] = new SelectList(_context.Properties, "PropertyId", "Name", apartment.PropertyId);
            return View(apartment);
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
            // Assuming Manager ID is retrieved from claims. For demo, we might need a way to identify "This" manager.
            // In a real app: var managerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            // To make it functional for the user without complex auth setup:
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
    }
}
