using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SW_Project.Interfaces;
using SW_Project.Models;

namespace SW_Project.Controllers
{
    public class ContactController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public ContactController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
            {
                return View("Index", model);
            }

            if (!model.AgreePrivacy)
            {
                ModelState.AddModelError("AgreePrivacy", "You must agree to the privacy policy.");
                return View("Index", model);
            }

            model.SentAt = DateTime.Now;
            model.IsRead = false;

            await _unitOfWork.ContactMessages.AddAsync(model);
            await _unitOfWork.CompleteAsync();

            TempData["Success"] = "Your message has been sent successfully. We'll get back to you soon.";
            return RedirectToAction("Index");
        }
    }
}