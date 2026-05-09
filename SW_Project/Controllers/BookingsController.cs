using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SW_Project.Interfaces;
using SW_Project.Models;
using SW_Project.ViewModels.Booking;

namespace SW_Project.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingsController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);
            var today = DateTime.Today;

            var bookings = await _unitOfWork.Bookings.FindAllAsync(
                b => b.RenterId == userId,
                b => b.Listing);

            // ✅ إضافة ToList()
            var orderedBookings = bookings.OrderByDescending(b => b.CreatedAt).ToList();

            var userReviews = new HashSet<int>();
            var reviews = await _unitOfWork.Reviews.FindAllAsync(r => r.ReviewerId == userId);
            foreach (var review in reviews)
            {
                userReviews.Add(review.BookingId);
            }

            var viewModel = orderedBookings.Select(b => new MyBookingVM
            {
                Id = b.Id,
                ListingTitle = b.Listing.Title,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                TotalPrice = b.TotalPrice,
                Status = b.Status,
                ListingId = b.ListingId,
                RenterId = b.RenterId,
                OwnerId = b.Listing.OwnerId,
                HasReviewed = userReviews.Contains(b.Id)
            }).ToList();

            ViewBag.UserId = userId;
            return View(viewModel);
        }

        public async Task<IActionResult> ReceivedBookings()
        {
            var userId = _userManager.GetUserId(User);

            var bookings = await _unitOfWork.Bookings.FindAllAsync(
                b => b.Listing.OwnerId == userId,
                b => b.Listing,
                b => b.Listing.Category,
                b => b.Renter);

            // ✅ إضافة ToList()
            var orderedBookings = bookings.OrderByDescending(b => b.CreatedAt).ToList();

            var bookingIds = orderedBookings.Select(b => b.Id).ToList();
            var contracts = new Dictionary<int, int>();

            foreach (var id in bookingIds)
            {
                var contract = await _unitOfWork.Contracts.FindAsync(c => c.BookingId == id);
                if (contract != null)
                {
                    contracts[id] = contract.Id;
                }
            }

            ViewBag.ContractIds = contracts;
            return View(orderedBookings);
        }

        public async Task<IActionResult> Create(int listingId)
        {
            var listing = await _unitOfWork.Listings.FindAsync(
                l => l.Id == listingId,
                l => l.Category);

            if (listing == null)
                return NotFound();

            ViewBag.Listing = listing;
            return View(new Booking { ListingId = listingId, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(1) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking booking)
        {
            ModelState.Remove("Listing");
            ModelState.Remove("Renter");
            ModelState.Remove("TotalPrice");
            ModelState.Remove("RenterId");

            var listing = await _unitOfWork.Listings.GetByIdAsync(booking.ListingId);
            if (listing == null)
                return NotFound();

            if (booking.StartDate < DateTime.Today)
            {
                ModelState.AddModelError("StartDate", "Start date cannot be in the past.");
            }
            if (booking.EndDate <= booking.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date.");
            }

            var allBookings = await _unitOfWork.Bookings.FindAllAsync(b => b.ListingId == booking.ListingId);
            var conflictingBooking = allBookings.FirstOrDefault(b =>
                b.Status != "Rejected" && b.Status != "Cancelled" && b.Status != "Completed" &&
                ((booking.StartDate >= b.StartDate && booking.StartDate < b.EndDate) ||
                 (booking.EndDate > b.StartDate && booking.EndDate <= b.EndDate) ||
                 (booking.StartDate <= b.StartDate && booking.EndDate >= b.EndDate)));

            if (conflictingBooking != null)
            {
                ModelState.AddModelError(string.Empty, "This item is already booked for the selected dates. Please choose different dates.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Listing = listing;
                return View(booking);
            }

            int days = (booking.EndDate - booking.StartDate).Days;
            booking.TotalPrice = listing.PricePerDay * days;
            booking.DepositPaid = listing.Deposit;
            booking.RenterId = _userManager.GetUserId(User);
            booking.Status = "Pending";
            booking.CreatedAt = DateTime.Now;

            await _unitOfWork.Bookings.AddAsync(booking);
            await _unitOfWork.CompleteAsync();

            // Send notification to owner
            var ownerNotification = new Notification
            {
                UserId = listing.OwnerId,
                Message = $"New booking request for '{listing.Title}' from {booking.StartDate:dd/MM/yyyy} to {booking.EndDate:dd/MM/yyyy}.",
                Type = "BookingRequest",
                LinkUrl = "/Bookings/ReceivedBookings",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            await _unitOfWork.Notifications.AddAsync(ownerNotification);
            await _unitOfWork.CompleteAsync();

            TempData["Success"] = "Your booking request has been sent. The owner will review it.";
            return RedirectToAction("MyBookings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var booking = await _unitOfWork.Bookings.FindAsync(
                b => b.Id == id,
                b => b.Listing,
                b => b.Renter);

            if (booking == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            if (booking.Listing.OwnerId != userId)
                return Forbid();

            if (status != "Accepted" && status != "Rejected")
                return BadRequest();

            booking.Status = status;
            _unitOfWork.Bookings.Update(booking);
            await _unitOfWork.CompleteAsync();

            if (status == "Accepted")
            {
                var renterNotification = new Notification
                {
                    UserId = booking.RenterId,
                    Message = $"Your booking for '{booking.Listing.Title}' has been accepted! The contract is now being prepared.",
                    Type = "BookingAccepted",
                    LinkUrl = "/Bookings/MyBookings",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                await _unitOfWork.Notifications.AddAsync(renterNotification);

                var existingContract = await _unitOfWork.Contracts.FindAsync(c => c.BookingId == booking.Id);

                if (existingContract == null)
                {
                    try
                    {
                        var terms = GenerateContractTerms(booking);
                        var contract = new Contract
                        {
                            BookingId = booking.Id,
                            PartyAId = booking.Listing.OwnerId,
                            PartyBId = booking.RenterId,
                            Title = $"Rental Agreement: {booking.Listing.Title}",
                            Terms = terms,
                            Status = "Draft",
                            CreatedAt = DateTime.Now
                        };
                        await _unitOfWork.Contracts.AddAsync(contract);
                        await _unitOfWork.CompleteAsync();

                        var notif1 = new Notification
                        {
                            UserId = contract.PartyAId,
                            Message = $"A new contract is ready for your signature for '{booking.Listing.Title}'.",
                            Type = "ContractReady",
                            LinkUrl = $"/Contracts/Details/{contract.Id}",
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        };
                        var notif2 = new Notification
                        {
                            UserId = contract.PartyBId,
                            Message = $"A new contract is ready for your signature for '{booking.Listing.Title}'.",
                            Type = "ContractReady",
                            LinkUrl = $"/Contracts/Details/{contract.Id}",
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        };
                        await _unitOfWork.Notifications.AddRangeAsync(new[] { notif1, notif2 });
                        await _unitOfWork.CompleteAsync();

                        TempData["ContractCreated"] = "Contract has been created successfully.";
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error creating contract: {ex.Message}");
                        TempData["Error"] = "Contract could not be created. Please contact support.";
                    }
                }
            }
            else if (status == "Rejected")
            {
                var renterNotification = new Notification
                {
                    UserId = booking.RenterId,
                    Message = $"Your booking for '{booking.Listing.Title}' has been rejected.",
                    Type = "BookingRejected",
                    LinkUrl = "/Bookings/MyBookings",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                await _unitOfWork.Notifications.AddAsync(renterNotification);
                await _unitOfWork.CompleteAsync();
            }

            TempData["Success"] = $"Booking has been {status.ToLower()}.";
            return RedirectToAction("ReceivedBookings");
        }

        private string GenerateContractTerms(Booking booking)
        {
            var days = (booking.EndDate - booking.StartDate).Days;

            return $@"
                This Rental Agreement is made on {DateTime.Now:MMMM dd, yyyy}
                
                Property: '{booking.Listing.Title}'
                Location: {booking.Listing.Location}
                
                Term: From {booking.StartDate:MMMM dd, yyyy} to {booking.EndDate:MMMM dd, yyyy} ({days} days)
                Rental Fee: ${booking.Listing.PricePerDay} per day → Total: ${booking.TotalPrice}
                Deposit: ${booking.Listing.Deposit ?? 0}
                
                Terms & Conditions:
                1. The Renter agrees to use the item responsibly.
                2. The Owner confirms the item is in good working condition.
                3. Any damage beyond normal wear and tear will be deducted from the deposit.
                4. Both parties agree to the terms outlined in this digital contract.
                
                This document is legally binding upon electronic signature by both parties";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var booking = await _unitOfWork.Bookings.FindAsync(
                b => b.Id == id,
                b => b.Listing);

            if (booking == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (booking.RenterId != userId && booking.Listing.OwnerId != userId)
                return Forbid();

            if (booking.Status != "Accepted" && booking.Status != "Active")
            {
                TempData["Error"] = "Only accepted or active bookings can be completed.";
                return RedirectToAction("MyBookings");
            }

            booking.Status = "Completed";
            _unitOfWork.Bookings.Update(booking);
            await _unitOfWork.CompleteAsync();

            TempData["Success"] = "Booking marked as completed. You can now leave a review.";
            return RedirectToAction("MyBookings");
        }


        // GET: /Bookings/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _unitOfWork.Bookings.GetByIdAsync(id);

            if (booking == null)
            {
                return NotFound();
            }

            // Redirect to the listing details page
            return RedirectToAction("Details", "Listings", new { id = booking.ListingId });
        }
    }
}