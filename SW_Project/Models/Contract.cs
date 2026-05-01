using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SW_Project.Models
{
    public class Contract
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Booking ID is required.")]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public Booking Booking { get; set; }

        [Required(ErrorMessage = "Party A (Owner) ID is required.")]
        public string PartyAId { get; set; }

        [Required(ErrorMessage = "Party B (Renter) ID is required.")]
        public string PartyBId { get; set; }

        [Required(ErrorMessage = "Contract title is required.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters.")]
        [Display(Name = "Contract Title")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Contract terms are required.")]
        [StringLength(5000, MinimumLength = 20, ErrorMessage = "Terms must be between 20 and 5000 characters.")]
        [Display(Name = "Terms & Conditions")]
        public string Terms { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Contract Status")]
        public string Status { get; set; } = "Draft";

        [Display(Name = "PDF Document")]
        public string? PdfPath { get; set; }

        [Display(Name = "Signatures")]
        public ICollection<ContractSignature> ContractSignatures { get; set; } = new List<ContractSignature>();

        [Display(Name = "Created Date")]
        [DisplayFormat(DataFormatString = "{0:MMMM dd, yyyy}")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}