using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SW_Project.Interfaces;
using SW_Project.Models;
using SW_Project.ViewModels.Listing;

namespace SW_Project.Controllers
{
    [Authorize]
    public class ListingsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ListingsController(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
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

            var query = (await _unitOfWork.Listings.FindAllAsync(
                l => l.Status == "Available" && !l.IsDeleted,
                l => l.Category,
                l => l.ListingImages)).AsQueryable();

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

            var total = query.Count();
            var listings = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var categories = await _unitOfWork.Categories.GetAllAsync();

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

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                var favorites = await _unitOfWork.Favorites.FindAllAsync(f => f.UserId == userId);
                var favoritedIds = favorites.Select(f => f.ListingId).ToList();
                ViewBag.FavoritedIds = favoritedIds;
            }
            else
            {
                ViewBag.FavoritedIds = new List<int>();
            }

            var today = DateTime.Today;
            var activeBookings = await _unitOfWork.Bookings.FindAllAsync(b =>
                (b.Status == "Accepted" || b.Status == "Active") &&
                b.StartDate <= today && b.EndDate >= today);

            var activeBookingIds = activeBookings.Select(b => b.ListingId).ToList();
            ViewBag.ActiveBookedIds = activeBookingIds;

            return View(listings);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var listing = await _unitOfWork.Listings.FindAsync(
                l => l.Id == id && !l.IsDeleted,
                l => l.Category,
                l => l.Owner,
                l => l.ListingImages);

            if (listing == null)
                return NotFound();

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
                IsOwner = User.Identity?.IsAuthenticated == true && listing.OwnerId == _userManager.GetUserId(User)
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Create()
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            var categories = await _unitOfWork.Categories.GetAllAsync();

            var viewModel = new CreateListingVM
            {
                Categories = categories.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateListingVM viewModel)
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            ModelState.Remove("Categories");

            // ✅ التحقق من وجود صورة في الإضافة الجديدة
            if (viewModel.Images == null || !viewModel.Images.Any())
            {
                ModelState.AddModelError("Images", "Please upload at least one image");
                var categories = await _unitOfWork.Categories.GetAllAsync();
                viewModel.Categories = categories.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();
                return View(viewModel);
            }

            if (!ModelState.IsValid)
            {
                var categories = await _unitOfWork.Categories.GetAllAsync();
                viewModel.Categories = categories.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();
                return View(viewModel);
            }

            var userId = _userManager.GetUserId(User);

            var listing = new Listing
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

            await _unitOfWork.Listings.AddAsync(listing);
            await _unitOfWork.CompleteAsync();

            // ✅ حفظ الصور (لازم هنا لأن Create)
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
                        await _unitOfWork.ListingImages.AddAsync(listingImage);
                        isFirst = false;
                    }
                }
                await _unitOfWork.CompleteAsync();
            }

            TempData["Success"] = "Your listing has been posted!";
            return RedirectToAction("Details", new { id = listing.Id });
        }

        [Authorize]
        public async Task<IActionResult> MyListings()
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            var userId = _userManager.GetUserId(User);

            var listings = await _unitOfWork.Listings.FindAllAsync(
                l => l.OwnerId == userId && !l.IsDeleted,
                l => l.Category,
                l => l.ListingImages);

            // ✅ إضافة ToList() لتجنب خطأ OrderedEnumerable
            var orderedListings = listings.OrderByDescending(l => l.CreatedAt).ToList();

            var viewModel = new MyListingsVM
            {
                Listings = orderedListings.Select(l => new ListingCardVM
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
                TotalCount = listings.Count()
            };

            return View(viewModel);
        }

        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            var userId = _userManager.GetUserId(User);

            var listing = await _unitOfWork.Listings.FindAsync(
                l => l.Id == id && l.OwnerId == userId,
                l => l.Category,
                l => l.ListingImages);

            if (listing == null)
            {
                TempData["Error"] = "Listing not found or you don't have permission.";
                return RedirectToAction(nameof(MyListings));
            }

            var today = DateTime.Today;
            var hasActiveBooking = await _unitOfWork.Bookings.ExistsAsync(b =>
                b.ListingId == id &&
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

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);

            var listing = await _unitOfWork.Listings.FindAsync(l => l.Id == id && l.OwnerId == userId);

            if (listing == null)
            {
                TempData["Error"] = "Listing not found or you don't have permission.";
                return RedirectToAction(nameof(MyListings));
            }

            var today = DateTime.Today;
            var hasActiveBooking = await _unitOfWork.Bookings.ExistsAsync(b =>
                b.ListingId == id &&
                (b.Status == "Accepted" || b.Status == "Active") &&
                b.StartDate <= today && b.EndDate >= today);

            if (hasActiveBooking)
            {
                TempData["Error"] = "Cannot delete this listing because it has an active booking.";
                return RedirectToAction(nameof(MyListings));
            }

            listing.Status = "Hidden";
            listing.IsDeleted = true;
            listing.DeletedAt = DateTime.Now;

            _unitOfWork.Listings.Update(listing);
            await _unitOfWork.CompleteAsync();

            TempData["Success"] = "Your listing has been deleted successfully.";
            return RedirectToAction(nameof(MyListings));
        }

        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            var userId = _userManager.GetUserId(User);

            var listing = await _unitOfWork.Listings.FindAsync(
                l => l.Id == id && l.OwnerId == userId,
                l => l.Category,
                l => l.ListingImages);

            if (listing == null)
            {
                TempData["Error"] = "Listing not found or you don't have permission.";
                return RedirectToAction(nameof(MyListings));
            }

            var categories = await _unitOfWork.Categories.GetAllAsync();

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
                Categories = categories.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                    Selected = c.Id == listing.CategoryId
                }).ToList()
            };

            return View(viewModel);
        }

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

            var listing = await _unitOfWork.Listings.FindAsync(
                l => l.Id == id && l.OwnerId == userId,
                l => l.ListingImages);

            if (listing == null)
            {
                TempData["Error"] = "Listing not found or you don't have permission.";
                return RedirectToAction(nameof(MyListings));
            }

            // ✅ إزالة التحقق من NewImages لأنها مش مطلوبة في التعديل
            ModelState.Remove("Categories");
            ModelState.Remove("ExistingImages");
            ModelState.Remove("NewImages");  // 🔥 دي المهمة جداً

            if (!ModelState.IsValid)
            {
                var categories = await _unitOfWork.Categories.GetAllAsync();
                viewModel.Categories = categories.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                    Selected = c.Id == viewModel.CategoryId
                }).ToList();
                return View(viewModel);
            }

            // ✅ تحديث البيانات الأساسية
            listing.Title = viewModel.Title;
            listing.Description = viewModel.Description;
            listing.PricePerDay = viewModel.PricePerDay;
            listing.Deposit = viewModel.Deposit;
            listing.Location = viewModel.Location;
            listing.CategoryId = viewModel.CategoryId;
            listing.Status = viewModel.Status;

            // ✅ فقط لو رفع صور جديدة (اختياري) - نضيفها جنب القديمة
            if (viewModel.NewImages != null && viewModel.NewImages.Any())
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "listings");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var existingImages = listing.ListingImages?.ToList() ?? new List<ListingImage>();
                bool hasImages = existingImages.Any();

                foreach (var img in viewModel.NewImages)
                {
                    if (img != null && img.Length > 0)
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
                            IsMain = !hasImages,
                            ListingId = listing.Id
                        };
                        await _unitOfWork.ListingImages.AddAsync(listingImage);
                        hasImages = true;
                    }
                }
            }

            _unitOfWork.Listings.Update(listing);
            await _unitOfWork.CompleteAsync();

            TempData["Success"] = "Your listing has been updated successfully!";
            return RedirectToAction("Details", new { id = listing.Id });
        }
    }
}