using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SW_Project.Interfaces;
using SW_Project.Models;

namespace SW_Project.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public FavoritesController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var favorites = await _unitOfWork.Favorites.FindAllAsync(
                f => f.UserId == userId,
                f => f.Listing,
                f => f.Listing.Category,
                f => f.Listing.ListingImages);

            var orderedFavorites = favorites.OrderByDescending(f => f.SavedAt).ToList();

            return View(orderedFavorites);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int listingId)
        {
            var userId = _userManager.GetUserId(User);

            var existing = await _unitOfWork.Favorites.ExistsAsync(f => f.UserId == userId && f.ListingId == listingId);

            if (existing)
                return Json(new { success = false, message = "Already in favorites" });

            var favorite = new Favorite
            {
                UserId = userId,
                ListingId = listingId,
                SavedAt = DateTime.Now
            };

            await _unitOfWork.Favorites.AddAsync(favorite);
            await _unitOfWork.CompleteAsync();

            return Json(new { success = true, message = "Added to favorites" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int listingId)
        {
            var userId = _userManager.GetUserId(User);

            var favorite = await _unitOfWork.Favorites.FindAsync(f => f.UserId == userId && f.ListingId == listingId);

            if (favorite == null)
                return Json(new { success = false, message = "Not in favorites" });

            _unitOfWork.Favorites.Delete(favorite);
            await _unitOfWork.CompleteAsync();

            return Json(new { success = true, message = "Removed from favorites" });
        }
    }
}