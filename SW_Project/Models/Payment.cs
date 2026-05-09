using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SW_Project.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ContractId { get; set; }

        [ForeignKey("ContractId")]
        public Contract Contract { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [StringLength(10)]
        public string Currency { get; set; } = "usd";

        [Required]
        [StringLength(100)]
        public string StripeSessionId { get; set; }

        [StringLength(100)]
        public string? StripePaymentIntentId { get; set; }


        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;


        public DateTime? PaidAt { get; set; }
    }
}