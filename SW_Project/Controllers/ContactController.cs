using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SW_Project.Interfaces;
using SW_Project.Models;

namespace SW_Project.Controllers
{
    public class ContactController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;


        public ContactController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
 
        public async Task<IActionResult> Send(ContactMessage model)
        {
            if (!ModelState.IsValid)
                return View("Index", model);

            if (!model.AgreePrivacy)
            {
                ModelState.AddModelError("AgreePrivacy", "You must agree to the privacy policy.");
                return View("Index", model);
            }

            model.SentAt = DateTime.Now;
            model.IsRead = false;

            var adminEmail = "owner@trustlink.com";
            var admin = await _userManager.FindByEmailAsync(adminEmail);

            if (admin != null)
            {
                model.AdminId = admin.Id;
            }

            await _unitOfWork.ContactMessages.AddAsync(model);
            await _unitOfWork.CompleteAsync();

            // ? ????? ????? ??????
            if (admin != null)
            {
                var notification = new Notification
                {
                    UserId = admin.Id,
                    Message = $"?? New message from {model.FullName}: {model.Subject}",
                    Type = "ContactMessage",
                    IsRead = false,
                    CreatedAt = DateTime.Now,
                    LinkUrl = "/Admin/Messages"
                };
                await _unitOfWork.Notifications.AddAsync(notification);
                await _unitOfWork.CompleteAsync();
            }

            TempData["Success"] = "Your message has been sent successfully.";
            return RedirectToAction("Index");
        }
    }
}