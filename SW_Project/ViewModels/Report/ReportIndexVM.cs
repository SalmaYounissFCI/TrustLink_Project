namespace SW_Project.ViewModels.Report
{
    public class ReportIndexVM
    {
        public int Id { get; set; }
        public string ReporterEmail { get; set; }
        public string ReporterName { get; set; }
        public string? ReportedInfo { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolvedByAdminEmail { get; set; }
        public string Type { get; set; } // "Listing" or "User"
        public int? ReportedListingId { get; set; }
        public string? ReportedListingTitle { get; set; }
        public string? ReportedUserEmail { get; set; }
    }
}