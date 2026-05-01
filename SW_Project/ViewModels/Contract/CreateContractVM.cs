using System.ComponentModel.DataAnnotations;

namespace SW_Project.ViewModels.Contract
{
    public class CreateContractVM
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 5)]
        [Display(Name = "Contract Title")]
        public string Title { get; set; }

        [Required]
        [StringLength(5000, MinimumLength = 20)]
        [Display(Name = "Terms & Conditions")]
        public string Terms { get; set; }

        [Display(Name = "Additional Notes")]
        [StringLength(1000)]
        public string AdditionalNotes { get; set; }
    }
}