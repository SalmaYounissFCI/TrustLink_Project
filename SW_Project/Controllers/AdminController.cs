using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SW_Project.Data;
using SW_Project.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SW_Project.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ======================== Dashboard ========================
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            ViewBag.TotalListings = await _context.Listings.CountAsync();
            ViewBag.TotalContracts = await _context.Contracts.CountAsync();
            ViewBag.OpenReports = await _context.Reports.CountAsync(r => !r.IsResolved);
            return View();
        }

        // ======================== إدارة المستخدمين ========================
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        // مسح مستخدم نهائياً
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // منع مسح المشرف نفسه
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser.Id == id)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Users));
            }

            // 1. حذف عقود المستخدم
            var userContracts = _context.Contracts.Where(c => c.PartyAId == id || c.PartyBId == id);
            _context.Contracts.RemoveRange(userContracts);

            // 2. حذف توقيعات العقود
            var userSignatures = _context.ContractSignatures.Where(s => s.UserId == id);
            _context.ContractSignatures.RemoveRange(userSignatures);

            // 3. حذف الحجوزات
            var userBookings = _context.Bookings.Where(b => b.RenterId == id || b.Listing.OwnerId == id);
            _context.Bookings.RemoveRange(userBookings);

            // 4. حذف الإعلانات
            var userListings = _context.Listings.Where(l => l.OwnerId == id);
            _context.Listings.RemoveRange(userListings);

            // 5. حذف البلاغات
            var reportsAsReporter = _context.Reports.Where(r => r.ReporterUserId == id);
            var reportsAsReported = _context.Reports.Where(r => r.ReportedUserId == id);
            _context.Reports.RemoveRange(reportsAsReporter);
            _context.Reports.RemoveRange(reportsAsReported);

            // حفظ التغييرات قبل حذف المستخدم
            await _context.SaveChangesAsync();

            // 6. حذف المستخدم
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = $"User {user.Name} and all related data have been permanently deleted.";
            }
            else
            {
                TempData["Error"] = "Failed to delete user.";
            }

            return RedirectToAction(nameof(Users));
        }

        // ======================== إدارة الإعلانات ========================
        public async Task<IActionResult> Listings()
        {
            var listings = await _context.Listings
                .Include(l => l.Category)
                .Include(l => l.Owner)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
            return View(listings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteListing(int id)
        {
            var listing = await _context.Listings.FindAsync(id);
            if (listing == null) return NotFound();

            _context.Listings.Remove(listing);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Listing deleted successfully.";
            return RedirectToAction(nameof(Listings));
        }

        // ======================== مراقبة العقود ========================
        public async Task<IActionResult> Contracts()
        {
            var contracts = await _context.Contracts
                .Include(c => c.Booking)
                    .ThenInclude(b => b.Listing)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return View(contracts);
        }

        // ======================== إدارة البلاغات ========================
        public async Task<IActionResult> Reports()
        {
            var reports = await _context.Reports
                .Include(r => r.ReporterUser)
                .Include(r => r.ReportedUser)
                .Include(r => r.ReportedListing)
                    .ThenInclude(l => l.Category)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reports);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveReport(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();

            var admin = await _userManager.GetUserAsync(User);

            report.IsResolved = true;
            report.ResolvedAt = DateTime.UtcNow;
            report.ResolvedByAdminId = admin?.Id;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Report resolved successfully.";
            return RedirectToAction(nameof(Reports));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReport(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();

            _context.Reports.Remove(report);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Report deleted successfully.";
            return RedirectToAction(nameof(Reports));
        }
    }
}