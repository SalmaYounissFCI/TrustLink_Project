using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SW_Project.Data;
using SW_Project.Models;
using SW_Project.ViewModels.Listing;
using System.Collections.Generic;   // ✅ أضيف لاستخدام List<int>

namespace SW_Project.Controllers
{
    [Authorize]
    public class ListingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ListingsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(
            int page = 1,
            string search = "",
            int? category = null,
            string location = "",
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string sort = "newest")
        {
            int pageSize = 9;
            var query = _context.Listings
                .Include(l => l.Category)
                .Include(l => l.ListingImages)
                .Where(l => l.Status == "Available" && !l.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(l => l.Title.Contains(search) || l.Description.Contains(search) || l.Location.Contains(search));
            if (category.HasValue)
                query = query.Where(l => l.CategoryId == category.Value);
            if (!string.IsNullOrWhiteSpace(location))
                query = query.Where(l => l.Location.Contains(location));
            if (minPrice.HasValue)
                query = query.Where(l => l.PricePerDay >= minPrice);
            if (maxPrice.HasValue)
                query = query.Where(l => l.PricePerDay <= maxPrice);

            sort = sort?.ToLower() ?? "newest";
            query = sort switch
            {
                "price-asc" => query.OrderBy(l => l.PricePerDay),
                "price-desc" => query.OrderByDescending(l => l.PricePerDay),
                _ => query.OrderByDescending(l => l.CreatedAt)
            };

            int total = await query.CountAsync();
            var listings = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var categories = await _context.Categories.ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.TotalListings = total;
            ViewBag.Search = search;
            ViewBag.SelectedCategory = category;
            ViewBag.Location = location;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Sort = sort;
            ViewBag.Categories = categories;

            // ✅ إضافة قائمة المفضلة للمستخدم الحالي
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                var favoritedIds = await _context.Favorites
                    .Where(f => f.UserId == userId)
                    .Select(f => f.ListingId)
                    .ToListAsync();
                ViewBag.FavoritedIds = favoritedIds;
            }
            else
            {
                ViewBag.FavoritedIds = new List<int>();
            }
            var today = DateTime.Today;
            var activeBookingIds = await _context.Bookings
                .Where(b => (b.Status == "Accepted" || b.Status == "Active") &&
                            b.StartDate <= today && b.EndDate >= today)
                .Select(b => b.ListingId)
                .ToListAsync();

            ViewBag.ActiveBookedIds = activeBookingIds;

            return View(listings);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var listing = await _context.Listings
                .Include(l => l.Category)
                .Include(l => l.Owner)
                .Include(l => l.ListingImages)
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

            if (listing == null)
                return NotFound();

            // ✅ Fixed: Handle nullable Rating properly
            double ownerRating = 0;
            if (listing.Owner != null && listing.Owner.Rating > 0)
            {
                ownerRating = (double)listing.Owner.Rating;
            }

            var viewModel = new ListingDetailsVM
            {
                Id = listing.Id,
                Title = listing.Title,
                Description = listing.Description,
                PricePerDay = listing.PricePerDay,
                Deposit = listing.Deposit,
                Location = listing.Location,
                Status = listing.Status,
                CreatedAt = listing.CreatedAt,
                CategoryName = listing.Category?.Name ?? "General",
                CategoryIcon = listing.Category?.Icon ?? "bi-box",
                OwnerId = listing.OwnerId,
                OwnerName = listing.Owner?.Name ?? "Unknown",
                OwnerEmail = listing.Owner?.Email ?? "",
                OwnerLocation = listing.Owner?.Location ?? "Not provided",
                OwnerRating = ownerRating,
                OwnerJoinedAt = listing.Owner?.CreatedAt ?? DateTime.Now,
                OwnerAvatar = !string.IsNullOrEmpty(listing.Owner?.Name) ? listing.Owner.Name.Substring(0, 1).ToUpper() : "?",
                ImageUrls = listing.ListingImages?.Select(i => i.ImagePath).ToList() ?? new List<string>(),
                MainImageUrl = listing.ListingImages?.FirstOrDefault(i => i.IsMain)?.ImagePath ?? listing.ListingImages?.FirstOrDefault()?.ImagePath,
                IsAvailable = listing.Status == "Available",
                IsOwner = User.Identity.IsAuthenticated && listing.OwnerId == _userManager.GetUserId(User)
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Create()
        {
            var viewModel = new CreateListingVM
            {
                Categories = await _context.Categories
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    }).ToListAsync()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateListingVM viewModel)
        {
            ModelState.Remove("Categories");

            if (!ModelState.IsValid)
            {
                viewModel.Categories = await _context.Categories
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    }).ToListAsync();
                return View(viewModel);
            }

            var userId = _userManager.GetUserId(User);

            var listing = new Models.Listing
            {
                Title = viewModel.Title,
                Description = viewModel.Description,
                PricePerDay = viewModel.PricePerDay,
                Deposit = viewModel.Deposit,
                Location = viewModel.Location,
                CategoryId = viewModel.CategoryId,
                OwnerId = userId,
                Status = "Available",
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };

            _context.Listings.Add(listing);
            await _context.SaveChangesAsync();

            // Save images
            if (viewModel.Images != null && viewModel.Images.Any())
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "listings");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                bool isFirst = true;
                foreach (var img in viewModel.Images)
                {
                    if (img.Length > 0)
                    {
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(img.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await img.CopyToAsync(stream);
                        }

                        var listingImage = new ListingImage
                        {
                            ImagePath = "/uploads/listings/" + uniqueFileName,
                            IsMain = isFirst,
                            ListingId = listing.Id
                        };
                        _context.ListingImages.Add(listingImage);
                        isFirst = false;
                    }
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Your listing has been posted!";
            return RedirectToAction("Details", new { id = listing.Id });
        }

        [Authorize]
        public async Task<IActionResult> MyListings()
        {
            var userId = _userManager.GetUserId(User);
            var listings = await _context.Listings
                .Include(l => l.Category)
                .Include(l => l.ListingImages)
                .Where(l => l.OwnerId == userId && !l.IsDeleted)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var viewModel = new MyListingsVM
            {
                Listings = listings.Select(l => new ListingCardVM
                {
                    Id = l.Id,
                    Title = l.Title,
                    Location = l.Location,
                    PricePerDay = l.PricePerDay,
                    Status = l.Status,
                    Deposit = l.Deposit,
                    CreatedAt = l.CreatedAt,
                    CategoryName = l.Category?.Name,
                    CategoryIcon = l.Category?.Icon ?? "bi-box",
                    MainImageUrl = l.ListingImages?.FirstOrDefault(i => i.IsMain)?.ImagePath ?? l.ListingImages?.FirstOrDefault()?.ImagePath
                }).ToList(),
                TotalCount = listings.Count
            };

            return View(viewModel);
        }

        // GET: Delete confirmation
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var listing = await _context.Listings
                .Include(l => l.Category)
                .Include(l => l.ListingImages)
                .FirstOrDefaultAsync(l => l.Id == id && l.OwnerId == userId);

            if (listing == null)
            {
                TempData["Error"] = "Listing not found or you don't have permission.";
                return RedirectToAction(nameof(MyListings));
            }

            // ✅ جيب لو فيه حجز نشط
            var today = DateTime.Today;
            var hasActiveBooking = await _context.Bookings
                .AnyAsync(b => b.ListingId == id &&
                               (b.Status == "Accepted" || b.Status == "Active") &&
                               b.StartDate <= today && b.EndDate >= today);

            ViewBag.HasActiveBooking = hasActiveBooking;

            var viewModel = new ListingCardVM
            {
                Id = listing.Id,
                Title = listing.Title,
                Location = listing.Location,
                PricePerDay = listing.PricePerDay,
                Status = listing.Status,
                Deposit = listing.Deposit,
                CreatedAt = listing.CreatedAt,
                CategoryName = listing.Category?.Name,
                MainImageUrl = listing.ListingImages?.FirstOrDefault()?.ImagePath
            };

            return View(viewModel);
        }

        // POST: Soft Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var listing = await _context.Listings
                .FirstOrDefaultAsync(l => l.Id == id && l.OwnerId == userId);

            if (listing == null)
            {
                TempData["Error"] = "Listing not found or you don't have permission.";
                return RedirectToAction(nameof(MyListings));
            }
            // ✅ منع الحذف لو فيه حجز نشط
            var today = DateTime.Today;
            var hasActiveBooking = await _context.Bookings
                .AnyAsync(b => b.ListingId == id &&
                               (b.Status == "Accepted" || b.Status == "Active") &&
                               b.StartDate <= today && b.EndDate >= today);

            if (hasActiveBooking)
            {
                TempData["Error"] = "Cannot delete this listing because it has an active booking. Please wait until the booking period ends.";
                return RedirectToAction(nameof(MyListings));
            }

            listing.Status = "Hidden";
            listing.IsDeleted = true;
            listing.DeletedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Your listing has been deleted successfully.";
            return RedirectToAction(nameof(MyListings));
        }

        // ========== EDIT ACTIONS ==========

        // GET: عرض صفحة تعديل الـ Listing
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var listing = await _context.Listings
                .Include(l => l.Category)
                .Include(l => l.ListingImages)
                .FirstOrDefaultAsync(l => l.Id == id && l.OwnerId == userId);

            if (listing == null)
            {
                TempData["Error"] = "Listing not found or you don't have permission.";
                return RedirectToAction(nameof(MyListings));
            }

            var viewModel = new EditListingVM
            {
                Id = listing.Id,
                Title = listing.Title,
                Description = listing.Description,
                PricePerDay = listing.PricePerDay,
                Deposit = listing.Deposit,
                Location = listing.Location,
                CategoryId = listing.CategoryId,
                Status = listing.Status,
                ExistingImages = listing.ListingImages?.Select(i => i.ImagePath).ToList() ?? new List<string>(),
                Categories = await _context.Categories
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name,
                        Selected = c.Id == listing.CategoryId
                    }).ToListAsync()
            };

            return View(viewModel);
        }

        // POST: تحديث الـ Listing
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, EditListingVM viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var listing = await _context.Listings
                .Include(l => l.ListingImages)
                .FirstOrDefaultAsync(l => l.Id == id && l.OwnerId == userId);

            if (listing == null)
            {
                TempData["Error"] = "Listing not found or you don't have permission.";
                return RedirectToAction(nameof(MyListings));
            }

            ModelState.Remove("Categories");
            ModelState.Remove("ExistingImages");

            if (!ModelState.IsValid)
            {
                viewModel.Categories = await _context.Categories
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name,
                        Selected = c.Id == viewModel.CategoryId
                    }).ToListAsync();
                return View(viewModel);
            }

            listing.Title = viewModel.Title;
            listing.Description = viewModel.Description;
            listing.PricePerDay = viewModel.PricePerDay;
            listing.Deposit = viewModel.Deposit;
            listing.Location = viewModel.Location;
            listing.CategoryId = viewModel.CategoryId;
            listing.Status = viewModel.Status;

            if (viewModel.NewImages != null && viewModel.NewImages.Any())
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "listings");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                foreach (var img in viewModel.NewImages)
                {
                    if (img.Length > 0)
                    {
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(img.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await img.CopyToAsync(stream);
                        }

                        var listingImage = new ListingImage
                        {
                            ImagePath = "/uploads/listings/" + uniqueFileName,
                            IsMain = !listing.ListingImages.Any(),
                            ListingId = listing.Id
                        };
                        _context.ListingImages.Add(listingImage);
                    }
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Your listing has been updated successfully!";
            return RedirectToAction("Details", new { id = listing.Id });
        }
    }
}