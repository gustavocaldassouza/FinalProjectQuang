using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinalProjectQuang.Data;
using FinalProjectQuang.Models;
using System.Linq;
using System.Threading.Tasks;

namespace FinalProjectQuang.Controllers
{
    [Authorize(Roles = "Owner")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Index
        public async Task<IActionResult> Index()
        {
            // Only managing Managers and Tenants
            var users = await _context.Users
                .Where(u => u.Role == UserRole.Manager || u.Role == UserRole.Tenant)
                .ToListAsync();
            return View(users);
        }

        // GET: Admin/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == id);
            
            if (user == null || user.Role == UserRole.Owner) return NotFound();

            return View(user);
        }

        // GET: Admin/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,FullName,Email,Password,Role")] User user)
        {
            if (ModelState.IsValid)
            {
                // Ensure we only create Managers or Tenants here
                if (user.Role == UserRole.Owner)
                {
                    ModelState.AddModelError("Role", "Cannot create Owner accounts via this portal.");
                    return View(user);
                }

                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Admin/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null || user.Role == UserRole.Owner) return NotFound();

            return View(user);
        }

        // POST: Admin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,FullName,Email,Password,Role")] User user)
        {
            if (id != user.UserId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (user.Role == UserRole.Owner)
                    {
                        ModelState.AddModelError("Role", "Cannot promote to Owner role.");
                        return View(user);
                    }

                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Admin/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == id);
            
            if (user == null || user.Role == UserRole.Owner) return NotFound();

            return View(user);
        }

        // POST: Admin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null && user.Role != UserRole.Owner)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Reports()
        {
            var ownerEmail = User.Identity?.Name;
            var owner = await _context.Users.FirstOrDefaultAsync(u => u.Email == ownerEmail);
            if (owner == null) return NotFound();

            var reports = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Property)
                .Where(m => m.ReceiverId == owner.UserId)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();

            return View(reports);
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
