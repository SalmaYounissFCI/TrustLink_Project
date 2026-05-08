namespace SW_Project.ViewModels.Admin
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalListings { get; set; }
        public int ActiveListings { get; set; }
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int TotalContracts { get; set; }
        public int PendingReports { get; set; }
        public int TotalReviews { get; set; }
    }
}