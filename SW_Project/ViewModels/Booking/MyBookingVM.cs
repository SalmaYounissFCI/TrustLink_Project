using System;

namespace SW_Project.ViewModels.Booking
{
    public class MyBookingVM
    {
        public int Id { get; set; }
        public string ListingTitle { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
        public int ListingId { get; set; }
        public string RenterId { get; set; }
        public string OwnerId { get; set; }
        public bool HasReviewed { get; set; }  // ✅ أضيفيها هنا

        public bool IsExpired => EndDate < DateTime.Today && (Status == "Accepted" || Status == "Active");
        public bool IsActive => (Status == "Accepted" || Status == "Active") && EndDate >= DateTime.Today;
        public string DisplayStatus => IsExpired ? "Completed" : Status;
        public string StatusColor => IsExpired ? "#3498db" : Status switch
        {
            "Pending" => "#f39c12",
            "Accepted" => "#2ecc71",
            "Active" => "#2ecc71",
            "Completed" => "#3498db",
            "Rejected" => "#e74c3c",
            _ => "#95a5a6"
        };
    }
}