using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SW_Project.Data;
using SW_Project.Models;
using SW_Project.ViewModels.Dashboard;   
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SW_Project.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            var activeListingsCount = await _context.Listings
                .CountAsync(l => l.OwnerId == userId && l.Status == "Available" && !l.IsDeleted);

            var activeContractsCount = await _context.Contracts
                .CountAsync(c => (c.PartyAId == userId || c.PartyBId == userId) && c.Status == "Active");

            var totalBookingsCount = await _context.Bookings
                .CountAsync(b => b.RenterId == userId || b.Listing.OwnerId == userId);

            var userRating = user?.Rating ?? 0;

            // ✅ استخدم RecentBookingDTO من الـ ViewModel
            var recentBookings = await _context.Bookings
                .Include(b => b.Listing)
                .Where(b => b.RenterId == userId || b.Listing.OwnerId == userId)
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
                })
                .ToListAsync();

            var recentContracts = await _context.Contracts
                .Include(c => c.Booking)
                    .ThenInclude(b => b.Listing)
                .Include(c => c.Booking.Renter)
                .Where(c => c.PartyAId == userId || c.PartyBId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .Select(c => new RecentContractDTO
                {
                    Id = c.Id,
                    Title = c.Title,
                    CreatedAt = c.CreatedAt,
                    Status = c.Status,
                    OtherPartyName = c.PartyAId == userId ?
                        (c.Booking.Renter.Name ?? "Unknown") :
                        (c.Booking.Listing.Owner.Name ?? "Unknown")
                })
                .ToListAsync();

            var viewModel = new DashboardViewModel
            {
                UserFirstName = user?.Name?.Split(' ').FirstOrDefault() ?? "User",
                ActiveListingsCount = activeListingsCount,
                ActiveContractsCount = activeContractsCount,
                TotalBookingsCount = totalBookingsCount,
                UserRating = userRating,
                RecentBookings = recentBookings ?? new List<RecentBookingDTO>(),
                RecentContracts = recentContracts ?? new List<RecentContractDTO>()
            };
            //  جلب آخر 5 تقييمات كتبها المستخدم
            var recentReviews = await _context.Reviews
                .Include(r => r.Booking)
                    .ThenInclude(b => b.Listing)
                .Include(r => r.Reviewee)
                .Where(r => r.ReviewerId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .Select(r => new RecentReviewDTO
                {
                    Id = r.Id,
                    ListingTitle = r.Booking.Listing.Title,
                    ReviewerName = r.Reviewer.Name ?? "You",
                    RevieweeName = r.Reviewee.Name ?? "User",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    Role = "Given"
                })
                .ToListAsync();

            //مان جيب آخر 5 تقييمات استقبلها المستخدم )
            var recentReceivedReviews = await _context.Reviews
                .Include(r => r.Booking)
                    .ThenInclude(b => b.Listing)
                .Include(r => r.Reviewer)
                .Where(r => r.RevieweeId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(3)
                .Select(r => new RecentReviewDTO
                {
                    Id = r.Id,
                    ListingTitle = r.Booking.Listing.Title,
                    ReviewerName = r.Reviewer.Name ?? "Someone",
                    RevieweeName = r.Reviewee.Name ?? "You",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    Role = "Received"
                })
                .ToListAsync();

            viewModel.RecentReviews = recentReviews;

            return View(viewModel);

        }

    }
}