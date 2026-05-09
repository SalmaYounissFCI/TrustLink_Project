using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SW_Project.Interfaces;
using SW_Project.Models;
using SW_Project.ViewModels.Admin;

namespace SW_Project.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // ======================== Dashboard ========================
        public async Task<IActionResult> Index()
        {
            // إحصائيات المستخدمين
            var totalUsers = await _unitOfWork.Users.CountAsync();

            // إحصائيات الإعلانات
            var totalListings = await _unitOfWork.Listings.CountAsync();
            var activeListings = await _unitOfWork.Listings.CountAsync(l => l.Status == "Available" && !l.IsDeleted);

            // إحصائيات الحجوزات
            var totalBookings = await _unitOfWork.Bookings.CountAsync();
            var pendingBookings = await _unitOfWork.Bookings.CountAsync(b => b.Status == "Pending");

            // إحصائيات العقود
            var totalContracts = await _unitOfWork.Contracts.CountAsync();

            // إحصائيات البلاغات
            var pendingReports = await _unitOfWork.Reports.CountAsync(r => !r.IsResolved);

            // إحصائيات التقييمات
            var totalReviews = await _unitOfWork.Reviews.CountAsync();

            var totalMessages = await _unitOfWork.ContactMessages.CountAsync();

            var viewModel = new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalListings = totalListings,
                ActiveListings = activeListings,
                TotalBookings = totalBookings,
                PendingBookings = pendingBookings,
                TotalContracts = totalContracts,
                PendingReports = pendingReports,
                TotalReviews = totalReviews
            };

            return View(viewModel);
        }

        // ======================== Users Management ========================
        public async Task<IActionResult> Users()
        {
            var users = await _unitOfWork.Users.GetAllAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == id)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Users));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Delete related data
                var userContracts = await _unitOfWork.Contracts.FindAllAsync(c => c.PartyAId == id || c.PartyBId == id);
                _unitOfWork.Contracts.DeleteRange(userContracts);

                var userSignatures = await _unitOfWork.ContractSignatures.FindAllAsync(s => s.UserId == id);
                _unitOfWork.ContractSignatures.DeleteRange(userSignatures);

                var userBookings = await _unitOfWork.Bookings.FindAllAsync(b => b.RenterId == id || b.Listing.OwnerId == id);
                _unitOfWork.Bookings.DeleteRange(userBookings);

                var userListings = await _unitOfWork.Listings.FindAllAsync(l => l.OwnerId == id);
                _unitOfWork.Listings.DeleteRange(userListings);

                var reportsAsReporter = await _unitOfWork.Reports.FindAllAsync(r => r.ReporterUserId == id);
                var reportsAsReported = await _unitOfWork.Reports.FindAllAsync(r => r.ReportedUserId == id);
                _unitOfWork.Reports.DeleteRange(reportsAsReporter);
                _unitOfWork.Reports.DeleteRange(reportsAsReported);

                // حذف المفضلة
                var userFavorites = await _unitOfWork.Favorites.FindAllAsync(f => f.UserId == id);
                _unitOfWork.Favorites.DeleteRange(userFavorites);

                // حذف الإشعارات
                var userNotifications = await _unitOfWork.Notifications.FindAllAsync(n => n.UserId == id);
                _unitOfWork.Notifications.DeleteRange(userNotifications);

                // حذف رسائل المحادثات
                var userMessages = await _unitOfWork.Messages.FindAllAsync(m => m.SenderId == id || m.ReceiverId == id);
                _unitOfWork.Messages.DeleteRange(userMessages);

                await _unitOfWork.CompleteAsync();

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    await _unitOfWork.CommitTransactionAsync();
                    TempData["Success"] = $"User {user.Name} and all related data have been permanently deleted.";
                }
                else
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    TempData["Error"] = "Failed to delete user.";
                }
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                TempData["Error"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction(nameof(Users));
        }

        // ======================== Listings Management ========================
        public async Task<IActionResult> Listings()
        {
            var listings = await _unitOfWork.Listings.FindAllAsync(
                l => true,
                l => l.Category,
                l => l.Owner);

            var orderedListings = listings.OrderByDescending(l => l.CreatedAt);
            return View(orderedListings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteListing(int id)
        {
            var listing = await _unitOfWork.Listings.GetByIdAsync(id);
            if (listing == null) return NotFound();

            _unitOfWork.Listings.Delete(listing);
            await _unitOfWork.CompleteAsync();

            TempData["Success"] = "Listing deleted successfully.";
            return RedirectToAction(nameof(Listings));
        }

        // ======================== Contracts Monitoring ========================
        public async Task<IActionResult> Contracts()
        {
            var contracts = await _unitOfWork.Contracts.FindAllAsync(
                c => true,
                c => c.Booking,
                c => c.Booking.Listing,
                c => c.Booking.Renter,
                c => c.Booking.Listing.Owner);

            var orderedContracts = contracts.OrderByDescending(c => c.CreatedAt);
            return View(orderedContracts);
        }

        // ======================== Reports Management ========================
        public async Task<IActionResult> Reports()
        {
            var reports = await _unitOfWork.Reports.FindAllAsync(
                r => true,
                r => r.ReporterUser,
                r => r.ReportedUser,
                r => r.ReportedListing,
                r => r.ReportedListing.Category);

            var orderedReports = reports.OrderByDescending(r => r.CreatedAt);
            return View(orderedReports);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveReport(int id)
        {
            var report = await _unitOfWork.Reports.GetByIdAsync(id);
            if (report == null) return NotFound();

            var admin = await _userManager.GetUserAsync(User);

            report.IsResolved = true;
            report.ResolvedAt = DateTime.UtcNow;
            report.ResolvedByAdminId = admin?.Id;

            _unitOfWork.Reports.Update(report);
            await _unitOfWork.CompleteAsync();

            TempData["Success"] = "Report resolved successfully.";
            return RedirectToAction(nameof(Reports));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReport(int id)
        {
            await _unitOfWork.Reports.DeleteByIdAsync(id);
            await _unitOfWork.CompleteAsync();

            TempData["Success"] = "Report deleted successfully.";
            return RedirectToAction(nameof(Reports));
        }

        // ======================== Reviews Management (إضافي) ========================
        public async Task<IActionResult> Reviews()
        {
            var reviews = await _unitOfWork.Reviews.FindAllAsync(
                r => true,
                r => r.Reviewer,
                r => r.Reviewee,
                r => r.Booking,
                r => r.Booking.Listing);

            var orderedReviews = reviews.OrderByDescending(r => r.CreatedAt);
            return View(orderedReviews);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            await _unitOfWork.Reviews.DeleteByIdAsync(id);
            await _unitOfWork.CompleteAsync();

            TempData["Success"] = "Review deleted successfully.";
            return RedirectToAction(nameof(Reviews));
        }

        // ======================== Bookings Management (إضافي) ========================
        public async Task<IActionResult> AllBookings()
        {
            var bookings = await _unitOfWork.Bookings.FindAllAsync(
                b => true,
                b => b.Listing,
                b => b.Renter,
                b => b.Listing.Owner);

            var orderedBookings = bookings.OrderByDescending(b => b.CreatedAt);
            return View(orderedBookings);
        }
        // ======================== Contact Messages Management ========================
        public async Task<IActionResult> Messages()
        {
            var messages = await _unitOfWork.ContactMessages.FindAllAsync(m => true);
            var orderedMessages = messages.OrderByDescending(m => m.SentAt).ToList();

            return View(orderedMessages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkMessageAsRead(int id)
        {
            var message = await _unitOfWork.ContactMessages.GetByIdAsync(id);
            if (message != null)
            {
                message.IsRead = true;
                _unitOfWork.ContactMessages.Update(message);
                await _unitOfWork.CompleteAsync();
                TempData["Success"] = "Message marked as read.";
            }
            return RedirectToAction(nameof(Messages));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            await _unitOfWork.ContactMessages.DeleteByIdAsync(id);
            await _unitOfWork.CompleteAsync();
            TempData["Success"] = "Message deleted successfully.";
            return RedirectToAction(nameof(Messages));
        }
    }
}