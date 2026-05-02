using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SW_Project.Data;
using SW_Project.Models;
using SW_Project.ViewModels.Report;

namespace SW_Project.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Reports/Create
        [HttpGet]
        public IActionResult Create(int? listingId, string? reportedUserId)
        {
            var vm = new CreateReportVM
            {
                ListingId = listingId,
                ReportedUserId = reportedUserId
            };

            if (listingId.HasValue)
            {
                ViewBag.ReportType = "إعلان";
            }
            else if (!string.IsNullOrEmpty(reportedUserId))
            {
                ViewBag.ReportType = "مستخدم";
            }
            else
            {
                ViewBag.ReportType = "مخالفة";
            }

            return View(vm);
        }

        // POST: Reports/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReportVM vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var report = new Models.Report
            {
                ReporterUserId = currentUser.Id,
                ReportedListingId = vm.ListingId,
                ReportedUserId = vm.ReportedUserId,
                Reason = vm.Reason,
                CreatedAt = DateTime.UtcNow,
                IsResolved = false
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم إرسال البلاغ بنجاح، سيقوم فريق الإدارة بمراجعته.";
            return RedirectToAction("Index", "Home");
        }

        // GET: Reports/Index (Admin only)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var reports = await _context.Reports
                .Include(r => r.ReporterUser)
                .Include(r => r.ReportedUser)
                .Include(r => r.ReportedListing)
                .ThenInclude(l => l.Category)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var vm = reports.Select(r => new ReportIndexVM
            {
                Id = r.Id,
                ReporterEmail = r.ReporterUser?.Email ?? "Unknown",
                ReporterName = r.ReporterUser?.Name ?? "Unknown",
                Reason = r.Reason,
                CreatedAt = r.CreatedAt,
                IsResolved = r.IsResolved,
                ResolvedAt = r.ResolvedAt,
                ResolvedByAdminEmail = r.ResolvedByAdmin != null ? r.ResolvedByAdmin.Email : null,
                ReportedListingId = r.ReportedListingId,
                ReportedListingTitle = r.ReportedListing?.Title,
                ReportedUserEmail = r.ReportedUser?.Email,
                Type = r.ReportedListingId.HasValue ? "Listing" : (r.ReportedUserId != null ? "User" : "Other"),
                ReportedInfo = r.ReportedListingId.HasValue ? $"إعلان: {r.ReportedListing?.Title}" :
                              (r.ReportedUserId != null ? $"مستخدم: {r.ReportedUser?.Email}" : "عام")
            }).ToList();

            return View(vm);
        }

        // POST: Reports/Resolve/5 (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null)
            {
                return NotFound();
            }

            var admin = await _userManager.GetUserAsync(User);
            report.IsResolved = true;
            report.ResolvedAt = DateTime.UtcNow;
            report.ResolvedByAdminId = admin?.Id;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم حل البلاغ بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // POST: Reports/Delete/5 (Admin only) - optional
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null)
            {
                return NotFound();
            }

            _context.Reports.Remove(report);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم حذف البلاغ بنجاح";
            return RedirectToAction(nameof(Index));
        }
    }
}