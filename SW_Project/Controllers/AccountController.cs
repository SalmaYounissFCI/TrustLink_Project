using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SW_Project.Data;
using SW_Project.Models;
using System.Linq;
using System.Threading.Tasks;
using SW_Project.ViewModels.Account;

namespace SW_Project.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                TempData["Success"] = $"Welcome back, {user.Name}!";

                // ✅ لو Admin يروح على Admin Dashboard
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }

                // ✅ لو مستخدم عادي يروح على Profile بتاعه
                return RedirectToAction("Profile", "Account");
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (!model.AgreeToTerms)
            {
                ModelState.AddModelError("AgreeToTerms", "You must agree to the terms.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name,
                Location = model.Location,
                PhoneNumber = model.PhoneNumber,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["Success"] = $"Welcome to TrustLink, {user.Name}!";
                return RedirectToAction("Profile", "Account");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    System.Diagnostics.Debug.WriteLine($"ERROR: {error.Description}");
                }
                return View(model);
            }
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            // ✅ لو Admin يروح على صفحة Profile خاصة بالـ Admin
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return View("~/Views/Admin/Profile.cshtml");
            }

            // المستخدم العادي يروح على Profile العادي
            var profileVM = await MapToProfileVM(user);
            return View(profileVM);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UpdateProfileVM model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please check the form fields.";
                return RedirectToAction("Profile");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            bool changed = false;

            if (!string.IsNullOrEmpty(model.Name) && user.Name != model.Name)
            {
                user.Name = model.Name;
                changed = true;
            }

            if (model.PhoneNumber != null && user.PhoneNumber != model.PhoneNumber)
            {
                user.PhoneNumber = model.PhoneNumber;
                changed = true;
            }

            if (model.Location != null && user.Location != model.Location)
            {
                user.Location = model.Location;
                changed = true;
            }

            if (changed)
            {
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["Success"] = $"Welcome back, {user.Name}!";

                    // لو المستخدم Admin يروح على Admin Dashboard
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToAction("Index", "Admin");
                    }

                    return RedirectToAction("Profile", "Account");
                }
                else
                {
                    TempData["Error"] = "Failed to update profile.";
                }
            }
            else
            {
                TempData["Info"] = "No changes were made.";
            }

            return RedirectToAction("Profile");
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["Success"] = "Logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        // Private helper method to map ApplicationUser to ProfileVM
        // Private helper method to map ApplicationUser to ProfileVM
        private async Task<ProfileVM> MapToProfileVM(ApplicationUser user)
        {
            // جلب إعلانات المستخدم
            var listings = await _context.Listings
                .Include(l => l.Category)
                .Include(l => l.ListingImages)
                .Where(l => l.OwnerId == user.Id && (l.IsDeleted == false || l.IsDeleted == null))
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            // جلب الحجوزات (كمستأجر)
            var bookingsAsRenter = await _context.Bookings
                .Include(b => b.Listing)
                .ThenInclude(l => l.ListingImages)
                .Include(b => b.Listing)
                .ThenInclude(l => l.Owner)
                .Where(b => b.RenterId == user.Id)
                .ToListAsync();

            // جلب الحجوزات (كمالك)
            var bookingsAsOwner = await _context.Bookings
                .Include(b => b.Listing)
                .Include(b => b.Renter)
                .Where(b => b.Listing.OwnerId == user.Id)
                .ToListAsync();

            // جلب العقود
            var contracts = await _context.Contracts
                .Include(c => c.Booking)
                .ThenInclude(b => b.Listing)
                .Where(c => c.PartyAId == user.Id || c.PartyBId == user.Id)
                .ToListAsync();

            // حساب الإحصائيات
            int activeListingsCount = listings.Count(l => l.Status == "Available");
            int totalBookingsCount = bookingsAsRenter.Count + bookingsAsOwner.Count;
            int activeContractsCount = contracts.Count(c => c.Status == "Active");

            // تحويل الـ Listings إلى ListingCardVM
            var listingCards = listings.Select(l => new ListingCardVM
            {
                Id = l.Id,
                Title = l.Title,
                Location = l.Location,
                PricePerDay = l.PricePerDay,
                Status = l.Status,
                MainImageUrl = l.ListingImages?.FirstOrDefault(i => i.IsMain == true)?.ImagePath
                               ?? l.ListingImages?.FirstOrDefault()?.ImagePath,
                CategoryIcon = l.Category?.Icon ?? "bi-box",
                CreatedAt = l.CreatedAt
            }).ToList();

            // تحويل الـ Bookings إلى BookingCardVM
            var bookingCards = new List<BookingCardVM>();

            foreach (var b in bookingsAsRenter)
            {
                bookingCards.Add(new BookingCardVM
                {
                    Id = b.Id,
                    ListingId = b.ListingId,
                    ListingTitle = b.Listing?.Title,
                    ListingImageUrl = b.Listing?.ListingImages?.FirstOrDefault()?.ImagePath,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    TotalPrice = b.TotalPrice,
                    Status = b.Status,
                    IsOwner = false,
                    OtherPartyName = b.Listing?.Owner?.Name
                });
            }

            foreach (var b in bookingsAsOwner)
            {
                bookingCards.Add(new BookingCardVM
                {
                    Id = b.Id,
                    ListingId = b.ListingId,
                    ListingTitle = b.Listing?.Title,
                    ListingImageUrl = b.Listing?.ListingImages?.FirstOrDefault()?.ImagePath,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    TotalPrice = b.TotalPrice,
                    Status = b.Status,
                    IsOwner = true,
                    OtherPartyName = b.Renter?.Name
                });
            }

            bookingCards = bookingCards.OrderByDescending(b => b.StartDate).ToList();

            // تحويل الـ Contracts إلى ContractCardVM
            var contractCards = contracts.Select(c => new ContractCardVM
            {
                Id = c.Id,
                ContractNumber = c.Id.ToString(),
                ListingTitle = c.Booking?.Listing?.Title,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                OtherPartyName = c.PartyAId == user.Id ?
                    _context.Users.Find(c.PartyBId)?.Name :
                    _context.Users.Find(c.PartyAId)?.Name,
                IsSigned = c.Status == "Active"
            }).ToList();

            return new ProfileVM
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Location = user.Location,
                Rating = (double)user.Rating,
                CreatedAt = user.CreatedAt,
                ProfileImage = user.ProfileImage,
                ActiveListingsCount = activeListingsCount,
                TotalBookingsCount = totalBookingsCount,
                ActiveContractsCount = activeContractsCount,
                FavoritesCount = 0,
                MyListings = listingCards,
                MyBookings = bookingCards,
                MyContracts = contractCards
            };
        }
    }
}