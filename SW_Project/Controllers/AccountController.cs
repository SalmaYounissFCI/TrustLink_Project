using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SW_Project.Interfaces;
using SW_Project.Models;
using SW_Project.ViewModels.Account;

namespace SW_Project.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUnitOfWork _unitOfWork;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _unitOfWork = unitOfWork;
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

                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }

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

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return View("~/Views/Admin/Profile.cshtml");
            }

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

        private async Task<ProfileVM> MapToProfileVM(ApplicationUser user)
        {
            // جلب إعلانات المستخدم
            var listings = await _unitOfWork.Listings
                .FindAllAsync(l => l.OwnerId == user.Id && (l.IsDeleted == false || l.IsDeleted == null),
                    l => l.Category, l => l.ListingImages);

            // جلب الحجوزات كمستأجر
            var bookingsAsRenter = await _unitOfWork.Bookings
                .FindAllAsync(b => b.RenterId == user.Id,
                    b => b.Listing, b => b.Listing.ListingImages, b => b.Listing.Owner);

            // جلب الحجوزات كمالك
            var bookingsAsOwner = await _unitOfWork.Bookings
                .FindAllAsync(b => b.Listing.OwnerId == user.Id,
                    b => b.Listing, b => b.Renter);

            // جلب العقود
            var contracts = await _unitOfWork.Contracts
                .FindAllAsync(c => c.PartyAId == user.Id || c.PartyBId == user.Id,
                    c => c.Booking, c => c.Booking.Listing);

            // حساب الإحصائيات
            int activeListingsCount = listings.Count(l => l.Status == "Available");
            int totalBookingsCount = bookingsAsRenter.Count() + bookingsAsOwner.Count();
            int activeContractsCount = contracts.Count(c => c.Status == "Active");

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

            var contractCards = contracts.Select(c => new ContractCardVM
            {
                Id = c.Id,
                ContractNumber = c.Id.ToString(),
                ListingTitle = c.Booking?.Listing?.Title,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                OtherPartyName = c.PartyAId == user.Id ?
                    (_unitOfWork.Users.GetByIdAsync(c.PartyBId).Result?.Name) :
                    (_unitOfWork.Users.GetByIdAsync(c.PartyAId).Result?.Name),
                IsSigned = c.Status == "Active"
            }).ToList();

            var favoritesCount = await _unitOfWork.Favorites.CountAsync(f => f.UserId == user.Id);

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
                FavoritesCount = favoritesCount,
                MyListings = listingCards,
                MyBookings = bookingCards,
                MyContracts = contractCards
            };
        }
    }
}