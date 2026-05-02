using System.Collections.Generic;

namespace SW_Project.ViewModels.Dashboard
{
    public class DashboardViewModel
    {
        public string UserFirstName { get; set; }
        public int ActiveListingsCount { get; set; }
        public int ActiveContractsCount { get; set; }
        public int TotalBookingsCount { get; set; }
        public decimal UserRating { get; set; }

        public List<RecentBookingDTO> RecentBookings { get; set; }
        public List<RecentContractDTO> RecentContracts { get; set; }
        public List<RecentReviewDTO> RecentReviews { get; set; }

    }

    public class RecentBookingDTO
    {
        public string ListingTitle { get; set; }
        public System.DateTime StartDate { get; set; }
        public System.DateTime EndDate { get; set; }
        public string Status { get; set; }
        public decimal TotalPrice { get; set; }
        public string Role { get; set; } // "Renter" or "Owner"
    }

    public class RecentContractDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public string OtherPartyName { get; set; }
    }
    public class RecentReviewDTO
    {
        public int Id { get; set; }
        public string ListingTitle { get; set; }
        public string ReviewerName { get; set; }
        public string RevieweeName { get; set; }
        public byte Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Role { get; set; } // "Given" or "Received"
    }
}