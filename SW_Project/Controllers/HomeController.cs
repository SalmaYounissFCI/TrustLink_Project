using Microsoft.AspNetCore.Mvc;
using SW_Project.Interfaces;
using SW_Project.Models;
using System.Diagnostics;

namespace SW_Project.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IUnitOfWork unitOfWork, ILogger<HomeController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var recentListings = await _unitOfWork.Listings.FindAllAsync(
                l => l.Status == "Available",
                l => l.Category,
                l => l.ListingImages);

            var orderedListings = recentListings
                .OrderByDescending(l => l.CreatedAt)
                .Take(3)
                .ToList();

            return View(orderedListings);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult HowItWorks()
        {
            ViewData["Title"] = "How It Works - TrustLink";
            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Title"] = "Contact Us - TrustLink";
            return View();
        }
    }
}