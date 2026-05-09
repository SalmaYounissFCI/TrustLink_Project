// ViewModels/Listing/EditListingVM.cs
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace SW_Project.ViewModels.Listing
{
    public class EditListingVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(150, MinimumLength = 5)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [MinLength(20)]
        public string Description { get; set; }

        [Required]
        [Range(1, 10000)]
        [Display(Name = "Price per Day")]
        public decimal PricePerDay { get; set; }

        [Range(0, 5000)]
        [Display(Name = "Security Deposit")]
        public decimal? Deposit { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public string Status { get; set; }

        // ✅ الصور الجديدة - اختيارية (مفيش Required)
        [Display(Name = "Add New Images (Optional)")]
        public List<IFormFile>? NewImages { get; set; }

        // ✅ دي موجودة لو عندك صور قديمة تظهر في الـ View
        // بس مش موجودة هنا في الكود اللي حطيته
        // لو عايزة تعرض الصور القديمة في الـ Edit form, أضيفي:
        public List<string>? ExistingImages { get; set; }

        // Dropdown
        public IEnumerable<SelectListItem> Categories { get; set; }
    }
}