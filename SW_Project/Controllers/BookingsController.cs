using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.SqlServer.Server;
using SW_Project.Data;
using SW_Project.Models;
using System;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace SW_Project.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);
            var bookings = await _context.Bookings
                .Include(b => b.Listing)
                .ThenInclude(l => l.Category)
                .Include(b => b.Listing.ListingImages)
                .Where(b => b.RenterId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            return View(bookings);
        }

        public async Task<IActionResult> ReceivedBookings()
        {
            var userId = _userManager.GetUserId(User);
            var bookings = await _context.Bookings
                .Include(b => b.Listing)
                .ThenInclude(l => l.Category)
                .Include(b => b.Renter)
                .Where(b => b.Listing.OwnerId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var bookingIds = bookings.Select(b => b.Id).ToList();
            var contracts = await _context.Contracts
                .Where(c => bookingIds.Contains(c.BookingId))
                .ToDictionaryAsync(c => c.BookingId, c => c.Id);

            ViewBag.ContractIds = contracts;
            return View(bookings);
        }

        public async Task<IActionResult> Create(int listingId)
        {
            var listing = await _context.Listings
                .Include(l => l.Category)
                .FirstOrDefaultAsync(l => l.Id == listingId);
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

            var listing = await _context.Listings.FindAsync(booking.ListingId);
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


            var conflictingBooking = await _context.Bookings
                .Where(b => b.ListingId == booking.ListingId &&
                            b.Status != "Rejected" &&
                            b.Status != "Cancelled" &&
                            b.Status != "Completed" &&
                            ((booking.StartDate >= b.StartDate && booking.StartDate < b.EndDate) ||
                             (booking.EndDate > b.StartDate && booking.EndDate <= b.EndDate) ||
                             (booking.StartDate <= b.StartDate && booking.EndDate >= b.EndDate)))
                .FirstOrDefaultAsync();

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

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // إرسال إشعار للمالك
            var ownerId = listing.OwnerId;
            var notification = new Notification
            {
                UserId = ownerId,
                Message = $"New booking request for '{listing.Title}' from {booking.StartDate:dd/MM/yyyy} to {booking.EndDate:dd/MM/yyyy}.",
                Type = "BookingRequest",
                LinkUrl = "/Bookings/ReceivedBookings",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your booking request has been sent. The owner will review it.";
            return RedirectToAction("MyBookings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var booking = await _context.Bookings
                .Include(b => b.Listing)
                .Include(b => b.Renter)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            if (booking.Listing.OwnerId != userId)
                return Forbid();

            // ✅ التحقق من أن status صحيح
            if (status != "Accepted" && status != "Rejected")
                return BadRequest();

            // ✅ تحديث حالة الحجز أولاً
            booking.Status = status;
            await _context.SaveChangesAsync();

            // ✅ إذا كان القبول، قم بإنشاء العقد
            if (status == "Accepted")
            {
                var existingContract = await _context.Contracts
                    .FirstOrDefaultAsync(c => c.BookingId == booking.Id);

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
                        _context.Contracts.Add(contract);
                        await _context.SaveChangesAsync();

                        // إشعارات للطرفين
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
                        _context.Notifications.AddRange(notif1, notif2);
                        await _context.SaveChangesAsync();

                        TempData["ContractCreated"] = "Contract has been created successfully.";
                    }
                    catch (Exception ex)
                    {
                        // تسجيل الخطأ للتصحيح
                        System.Diagnostics.Debug.WriteLine($"Error creating contract: {ex.Message}");
                        TempData["Error"] = "Contract could not be created. Please contact support.";
                    }
                }
            }

            TempData["Success"] = $"Booking has been {status.ToLower()}.";
            return RedirectToAction("ReceivedBookings");
        }


        private string GenerateContractTerms(Booking booking)
        {
            var listing = booking.Listing;
            var days = (booking.EndDate - booking.StartDate).Days;

            return $@"
                This Rental Agreement is made on {DateTime.Now:MMMM dd, yyyy} between:
                Owner:{listing.Owner?.Name ?? "Owner"}  
                Renter:{booking.Renter?.Name ?? "Renter"}
                Property: '{listing.Title}'- '{listing.Description}'
                Location:{listing.Location}
                
                Term:From {booking.StartDate:MMMM dd, yyyy} to {booking.EndDate:MMMM dd, yyyy} ({days} days)
                Rental Fee: ${listing.PricePerDay} per day → Total: ${booking.TotalPrice}
                Deposit: ${listing.Deposit ?? 0}
               Terms & Conditions:
                1. The Renter agrees to use the item responsibly.
                2. The Owner confirms the item is in good working condition.
                3. Any damage beyond normal wear and tear will be deducted from the deposit.
                4. Both parties agree to the terms outlined in this digital contract.

                This document is legally binding upon electronic signature by both parties";
            


            

        }
    }
}