using System.ComponentModel.DataAnnotations;

namespace SW_Project.ViewModels.Report
{
    public class CreateReportVM
    {
        public int? ListingId { get; set; }

        public string? ReportedUserId { get; set; }

        [Required(ErrorMessage = "Report reason is required")]
        [MinLength(5, ErrorMessage = "Please provide at least 5 characters")]
        [MaxLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters")]
        public string Reason { get; set; }
    }
}