using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SW_Project.Data;
using SW_Project.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SW_Project.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: عرض صفحة إضافة تقييم
        public async Task<IActionResult> Create(int bookingId, string revieweeId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Listing)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);

            // ✅ التحقق من إن الحجز مكتمل أو انتهت مدته
            var isEffectivelyCompleted = booking.Status == "Completed" || booking.EndDate < DateTime.Today;
            if (!isEffectivelyCompleted)
            {
                TempData["Error"] = "You can only review completed bookings.";
                return RedirectToAction("MyBookings", "Bookings");
            }

            // التحقق من إن المستخدم لسة ما قيمش
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.BookingId == bookingId && r.ReviewerId == userId);

            if (existingReview != null)
            {
                TempData["Error"] = "You have already reviewed this booking.";
                return RedirectToAction("MyBookings", "Bookings");
            }

            var reviewee = await _userManager.FindByIdAsync(revieweeId);

            ViewBag.Booking = booking;
            ViewBag.RevieweeId = revieweeId;
            ViewBag.RevieweeName = reviewee?.Name ?? "User";

            return View(new Review { BookingId = bookingId });
        }

        // POST: حفظ التقييم
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Review model)
        {
            ModelState.Remove("Booking");
            ModelState.Remove("Reviewer");
            ModelState.Remove("Reviewee");
            ModelState.Remove("ReviewerId");
            ModelState.Remove("RevieweeId");


            if (!ModelState.IsValid)
            {
                var booking = await _context.Bookings
                    .Include(b => b.Listing)
                    .FirstOrDefaultAsync(b => b.Id == model.BookingId);
                ViewBag.Booking = booking;
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            model.ReviewerId = userId;
            model.CreatedAt = DateTime.Now;

            var existing = await _context.Reviews
                .AnyAsync(r => r.BookingId == model.BookingId && r.ReviewerId == userId);

            if (existing)
            {
                TempData["Error"] = "You have already reviewed this booking.";
                return RedirectToAction("MyBookings", "Bookings");
            }

            _context.Reviews.Add(model);
            await _context.SaveChangesAsync();

            var avgRating = await _context.Reviews
                .Where(r => r.RevieweeId == model.RevieweeId)
                .AverageAsync(r => (decimal)r.Rating);

            var reviewee = await _userManager.FindByIdAsync(model.RevieweeId);
            reviewee.Rating = avgRating;
            await _userManager.UpdateAsync(reviewee);

            TempData["Success"] = "Your review has been submitted. Thank you!";
            return RedirectToAction("MyBookings", "Bookings");
        }
        // GET: عرض تقييماتي
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var givenReviews = await _context.Reviews
                .Include(r => r.Booking)
                    .ThenInclude(b => b.Listing)
                .Include(r => r.Reviewee)
                .Where(r => r.ReviewerId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var receivedReviews = await _context.Reviews
                .Include(r => r.Booking)
                    .ThenInclude(b => b.Listing)
                .Include(r => r.Reviewer)
                .Where(r => r.RevieweeId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.GivenReviews = givenReviews;
            ViewBag.ReceivedReviews = receivedReviews;

            return View();
        }
    }
}