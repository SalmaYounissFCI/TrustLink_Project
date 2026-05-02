using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SW_Project.Models
{
    public class Report
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ReporterUserId { get; set; }

        public string? ReportedUserId { get; set; }

        public int? ReportedListingId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsResolved { get; set; } = false;

        public DateTime? ResolvedAt { get; set; }

        public string? ResolvedByAdminId { get; set; }

        // Navigation Properties
        [ForeignKey("ReporterUserId")]
        public virtual ApplicationUser ReporterUser { get; set; }

        [ForeignKey("ReportedUserId")]
        public virtual ApplicationUser? ReportedUser { get; set; }

        [ForeignKey("ReportedListingId")]
        public virtual Listing? ReportedListing { get; set; }

        [ForeignKey("ResolvedByAdminId")]
        public virtual ApplicationUser? ResolvedByAdmin { get; set; }
    }
}