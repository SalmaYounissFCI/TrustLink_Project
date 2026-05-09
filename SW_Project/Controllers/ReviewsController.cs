using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SW_Project.Interfaces;
using SW_Project.Models;

namespace SW_Project.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewsController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Create(int bookingId, string revieweeId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == revieweeId)
            {
                TempData["Error"] = "You cannot review yourself.";
                return RedirectToAction("MyBookings", "Bookings");
            }
            var booking = await _unitOfWork.Bookings.FindAsync(
                b => b.Id == bookingId,
                b => b.Listing);

            if (booking == null)
                return NotFound();

           

            var isEffectivelyCompleted = booking.Status == "Completed" || booking.EndDate < DateTime.Today;
            if (!isEffectivelyCompleted)
            {
                TempData["Error"] = "You can only review completed bookings.";
                return RedirectToAction("MyBookings", "Bookings");
            }

            var existingReview = await _unitOfWork.Reviews.ExistsAsync(r => r.BookingId == bookingId && r.ReviewerId == userId);

            if (existingReview)
            {
                TempData["Error"] = "You have already reviewed this booking.";
                return RedirectToAction("MyBookings", "Bookings");
            }

            var reviewee = await _unitOfWork.Users.GetByIdAsync(revieweeId);

            ViewBag.Booking = booking;
            ViewBag.RevieweeId = revieweeId;
            ViewBag.RevieweeName = reviewee?.Name ?? "User";

            return View(new Review { BookingId = bookingId, RevieweeId = revieweeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Review model)
        {
            ModelState.Remove("Booking");
            ModelState.Remove("Reviewer");
            ModelState.Remove("Reviewee");
            ModelState.Remove("ReviewerId");

            if (!ModelState.IsValid)
            {
                var booking = await _unitOfWork.Bookings.FindAsync(
                    b => b.Id == model.BookingId,
                    b => b.Listing);
                ViewBag.Booking = booking;
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            model.ReviewerId = userId;
            model.CreatedAt = DateTime.Now;
            if (userId == model.RevieweeId)
            {
                TempData["Error"] = "You cannot review yourself.";
                return RedirectToAction("MyBookings", "Bookings");
            }

            var existing = await _unitOfWork.Reviews.ExistsAsync(r => r.BookingId == model.BookingId && r.ReviewerId == userId);

            if (existing)
            {
                TempData["Error"] = "You have already reviewed this booking.";
                return RedirectToAction("MyBookings", "Bookings");
            }

            await _unitOfWork.Reviews.AddAsync(model);
            await _unitOfWork.CompleteAsync();

            var allReviews = await _unitOfWork.Reviews.FindAllAsync(r => r.RevieweeId == model.RevieweeId);
            var avgRating = allReviews.Average(r => (decimal)r.Rating);

            var reviewee = await _unitOfWork.Users.GetByIdAsync(model.RevieweeId);
            if (reviewee != null)
            {
                reviewee.Rating = avgRating;
                _unitOfWork.Users.Update(reviewee);
                await _unitOfWork.CompleteAsync();
            }

            TempData["Success"] = "Your review has been submitted. Thank you!";
            return RedirectToAction("MyBookings", "Bookings");
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var givenReviews = await _unitOfWork.Reviews.FindAllAsync(
                r => r.ReviewerId == userId,
                r => r.Booking,
                r => r.Booking.Listing,
                r => r.Reviewee);

            var receivedReviews = await _unitOfWork.Reviews.FindAllAsync(
                r => r.RevieweeId == userId,
                r => r.Booking,
                r => r.Booking.Listing,
                r => r.Reviewer);

            ViewBag.GivenReviews = givenReviews.OrderByDescending(r => r.CreatedAt).ToList();
            ViewBag.ReceivedReviews = receivedReviews.OrderByDescending(r => r.CreatedAt).ToList();

            return View();
        }
    }
}