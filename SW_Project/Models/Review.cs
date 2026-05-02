using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SW_Project.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public Booking Booking { get; set; }

        [Required]
        public string ReviewerId { get; set; }

        [ForeignKey("ReviewerId")]
        public ApplicationUser Reviewer { get; set; }

        [Required]
        public string RevieweeId { get; set; }

        [ForeignKey("RevieweeId")]
        public ApplicationUser Reviewee { get; set; }

        [Required]
        [Range(1, 5)]
        public byte Rating { get; set; }

        [StringLength(500)]
        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}