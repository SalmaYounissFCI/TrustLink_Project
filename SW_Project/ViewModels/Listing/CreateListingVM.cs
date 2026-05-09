// ViewModels/Listing/CreateListingVM.cs
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace SW_Project.ViewModels.Listing
{
    public class CreateListingVM
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(150, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 150 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [MinLength(20, ErrorMessage = "Description must be at least 20 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(1, 10000, ErrorMessage = "Price must be between $1 and $10,000")]
        [Display(Name = "Price per Day")]
        public decimal PricePerDay { get; set; }

        [Range(0, 5000, ErrorMessage = "Deposit must be between $0 and $5,000")]
        [Display(Name = "Security Deposit (optional)")]
        public decimal? Deposit { get; set; }

        [Required(ErrorMessage = "Location is required")]
        [StringLength(100)]
        public string Location { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Please upload at least one image")]
        [Display(Name = "Images")]
        public List<IFormFile> Images { get; set; }

        public IEnumerable<SelectListItem> Categories { get; set; }
    }
}