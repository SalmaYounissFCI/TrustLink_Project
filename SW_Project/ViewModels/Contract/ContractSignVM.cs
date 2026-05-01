using System.ComponentModel.DataAnnotations;

namespace SW_Project.ViewModels.Contract
{
    public class ContractSignVM
    {
        [Required]
        public int ContractId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 5)]
        [Display(Name = "Contract Title")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Terms & Conditions")]
        public string Terms { get; set; }

        [Required]
        [Display(Name = "Your Name")]
        public string PartyName { get; set; }

        [Required]
        [Display(Name = "Your Role")]
        public string PartyRole { get; set; } // "Owner" or "Renter"

        [Required]
        [Display(Name = "Signature")]
        public string SignatureBase64 { get; set; }

        [Required]
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms before signing.")]
        [Display(Name = "I agree to the terms and conditions")]
        public bool AgreeToTerms { get; set; }
    }
}