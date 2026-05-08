using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SW_Project.Interfaces;
using SW_Project.Models;
using SW_Project.ViewModels.Report;

namespace SW_Project.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

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

            var report = new Report
            {
                ReporterUserId = currentUser.Id,
                ReportedListingId = vm.ListingId,
                ReportedUserId = vm.ReportedUserId,
                Reason = vm.Reason,
                CreatedAt = DateTime.UtcNow,
                IsResolved = false
            };

            await _unitOfWork.Reports.AddAsync(report);
            await _unitOfWork.CompleteAsync();

            TempData["SuccessMessage"] = "تم إرسال البلاغ بنجاح، سيقوم فريق الإدارة بمراجعته.";
            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var reports = await _unitOfWork.Reports.FindAllAsync(
                r => true,
                r => r.ReporterUser,
                r => r.ReportedUser,
                r => r.ReportedListing,
                r => r.ReportedListing.Category);

            var orderedReports = reports.OrderByDescending(r => r.CreatedAt);

            var vm = orderedReports.Select(r => new ReportIndexVM
            {
                Id = r.Id,
                ReporterEmail = r.ReporterUser?.Email ?? "Unknown",
                ReporterName = r.ReporterUser?.Name ?? "Unknown",
                Reason = r.Reason,
                CreatedAt = r.CreatedAt,
                IsResolved = r.IsResolved,
                ResolvedAt = r.ResolvedAt,
                ResolvedByAdminEmail = null, // Would need to load admin user
                ReportedListingId = r.ReportedListingId,
                ReportedListingTitle = r.ReportedListing?.Title,
                ReportedUserEmail = r.ReportedUser?.Email,
                Type = r.ReportedListingId.HasValue ? "Listing" : (r.ReportedUserId != null ? "User" : "Other"),
                ReportedInfo = r.ReportedListingId.HasValue ? $"إعلان: {r.ReportedListing?.Title}" :
                              (r.ReportedUserId != null ? $"مستخدم: {r.ReportedUser?.Email}" : "عام")
            }).ToList();

            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id)
        {
            var report = await _unitOfWork.Reports.GetByIdAsync(id);
            if (report == null)
            {
                return NotFound();
            }

            var admin = await _userManager.GetUserAsync(User);
            report.IsResolved = true;
            report.ResolvedAt = DateTime.UtcNow;
            report.ResolvedByAdminId = admin?.Id;

            _unitOfWork.Reports.Update(report);
            await _unitOfWork.CompleteAsync();

            TempData["SuccessMessage"] = "تم حل البلاغ بنجاح";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _unitOfWork.Reports.DeleteByIdAsync(id);
            await _unitOfWork.CompleteAsync();

            TempData["SuccessMessage"] = "تم حذف البلاغ بنجاح";
            return RedirectToAction(nameof(Index));
        }
    }
}