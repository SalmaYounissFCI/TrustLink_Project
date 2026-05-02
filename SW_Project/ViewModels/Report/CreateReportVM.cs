using System.ComponentModel.DataAnnotations;

namespace SW_Project.ViewModels.Report
{
    public class CreateReportVM
    {
        public int? ListingId { get; set; }

        public string? ReportedUserId { get; set; }

        [Required(ErrorMessage = "سبب البلاغ مطلوب")]
        [MinLength(5, ErrorMessage = "الرجاء كتابة سبب واضح (5 أحرف على الأقل)")]
        [MaxLength(1000, ErrorMessage = "سبب البلاغ لا يتجاوز 1000 حرف")]
        public string Reason { get; set; }
    }
}