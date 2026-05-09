using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SW_Project.Interfaces;
using SW_Project.Models;

namespace SW_Project.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var notifications = await _unitOfWork.Notifications.FindAllAsync(n => n.UserId == userId);

            // ✅ التحويل إلى List بعد الترتيب (وده اللي كان ناقص)
            var orderedNotifications = notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToList();  

            return View(orderedNotifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = _userManager.GetUserId(User);

            var notification = await _unitOfWork.Notifications
                .FindAsync(n => n.Id == id && n.UserId == userId);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                _unitOfWork.Notifications.Update(notification);
                await _unitOfWork.CompleteAsync();
            }

            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _userManager.GetUserId(User);

            var notifications = await _unitOfWork.Notifications
                .FindAllAsync(n => n.UserId == userId && !n.IsRead);

            foreach (var n in notifications)
            {
                n.IsRead = true;
                _unitOfWork.Notifications.Update(n);
            }

            await _unitOfWork.CompleteAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            if (!User.Identity?.IsAuthenticated == true)
                return Json(0);

            var userId = _userManager.GetUserId(User);
            var count = await _unitOfWork.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
            return Json(count);
        }
    }
}