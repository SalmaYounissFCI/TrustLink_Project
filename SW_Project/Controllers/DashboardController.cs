using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SW_Project.Interfaces;
using SW_Project.Models;
using SW_Project.ViewModels.Dashboard;

namespace SW_Project.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            var activeListingsCount = await _unitOfWork.Listings.CountAsync(
                l => l.OwnerId == userId && l.Status == "Available" && !l.IsDeleted);

            var activeContractsCount = await _unitOfWork.Contracts.CountAsync(
                c => (c.PartyAId == userId || c.PartyBId == userId) && c.Status == "Active");

            var totalBookingsCount = await _unitOfWork.Bookings.CountAsync(
                b => b.RenterId == userId || b.Listing.OwnerId == userId);

            var userRating = user?.Rating ?? 0;

            // Recent bookings
            var recentBookingsList = await _unitOfWork.Bookings.FindAllAsync(
                b => b.RenterId == userId || b.Listing.OwnerId == userId,
                b => b.Listing);

            var recentBookings = recentBookingsList
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .Select(b => new RecentBookingDTO
                {
                    ListingTitle = b.Listing.Title,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    Status = b.Status,
                    TotalPrice = b.TotalPrice,
                    Role = b.RenterId == userId ? "Renter" : "Owner"
                }).ToList();

            // Recent contracts
            var recentContractsList = await _unitOfWork.Contracts.FindAllAsync(
                c => c.PartyAId == userId || c.PartyBId == userId,
                c => c.Booking,
                c => c.Booking.Listing,
                c => c.Booking.Renter);

            var recentContracts = recentContractsList
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .Select(c => new RecentContractDTO
                {
                    Id = c.Id,
                    Title = c.Title,
                    CreatedAt = c.CreatedAt,
                    Status = c.Status,
                    OtherPartyName = c.PartyAId == userId ?
                        (c.Booking.Renter?.Name ?? "Unknown") :
                        (c.Booking.Listing.Owner?.Name ?? "Unknown")
                }).ToList();

            // Recent reviews given
            var recentReviewsGiven = await _unitOfWork.Reviews.FindAllAsync(
                r => r.ReviewerId == userId,
                r => r.Booking,
                r => r.Booking.Listing,
                r => r.Reviewee);

            var recentReviews = recentReviewsGiven
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .Select(r => new RecentReviewDTO
                {
                    Id = r.Id,
                    ListingTitle = r.Booking.Listing.Title,
                    ReviewerName = r.Reviewer?.Name ?? "You",
                    RevieweeName = r.Reviewee?.Name ?? "User",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    Role = "Given"
                }).ToList();

            // Recent reviews received
            var recentReviewsReceivedList = await _unitOfWork.Reviews.FindAllAsync(
                r => r.RevieweeId == userId,
                r => r.Booking,
                r => r.Booking.Listing,
                r => r.Reviewer);

            var recentReviewsReceived = recentReviewsReceivedList
                .OrderByDescending(r => r.CreatedAt)
                .Take(3)
                .Select(r => new RecentReviewDTO
                {
                    Id = r.Id,
                    ListingTitle = r.Booking.Listing.Title,
                    ReviewerName = r.Reviewer?.Name ?? "Someone",
                    RevieweeName = r.Reviewee?.Name ?? "You",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    Role = "Received"
                }).ToList();

            recentReviews.AddRange(recentReviewsReceived);

            var viewModel = new DashboardViewModel
            {
                UserFirstName = user?.Name?.Split(' ').FirstOrDefault() ?? "User",
                ActiveListingsCount = activeListingsCount,
                ActiveContractsCount = activeContractsCount,
                TotalBookingsCount = totalBookingsCount,
                UserRating = userRating,
                RecentBookings = recentBookings,
                RecentContracts = recentContracts,
                RecentReviews = recentReviews
            };

            return View(viewModel);
        }
    }
}