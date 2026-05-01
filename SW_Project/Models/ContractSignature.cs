using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SW_Project.Models
{
    public class ContractSignature
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Contract ID is required.")]
        public int ContractId { get; set; }

        [ForeignKey("ContractId")]
        public Contract Contract { get; set; }

        [Required(ErrorMessage = "User ID is required.")]
        public string UserId { get; set; }

        [Required(ErrorMessage = "Signature image is required.")]
        [Display(Name = "Signature")]
        public string SignatureImage { get; set; }

        [Display(Name = "Signed On")]
        [DisplayFormat(DataFormatString = "{0:MMMM dd, yyyy hh:mm tt}")]
        public DateTime SignedAt { get; set; } = DateTime.Now;
    }
}