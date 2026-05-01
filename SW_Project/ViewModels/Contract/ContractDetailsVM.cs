using System;
using System.ComponentModel.DataAnnotations;

namespace SW_Project.ViewModels.Contract
{
    public class ContractDetailsVM
    {
        public int Id { get; set; }

        [Display(Name = "Contract Title")]
        public string Title { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; }

        [Display(Name = "Terms & Conditions")]
        public string Terms { get; set; }

        [Display(Name = "PDF Document")]
        public string PdfPath { get; set; }

        [Display(Name = "Created Date")]
        [DisplayFormat(DataFormatString = "{0:MMMM dd, yyyy}")]
        public DateTime CreatedAt { get; set; }

        // Booking Details
        [Display(Name = "Booking ID")]
        public int BookingId { get; set; }

        [Display(Name = "Item Title")]
        public string ListingTitle { get; set; }

        [Display(Name = "Description")]
        public string ListingDescription { get; set; }

        [Display(Name = "Location")]
        public string ListingLocation { get; set; }

        [Display(Name = "Price Per Day")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal PricePerDay { get; set; }

        [Display(Name = "Start Date")]
        [DisplayFormat(DataFormatString = "{0:MMMM dd, yyyy}")]
        public DateTime StartDate { get; set; }

        [Display(Name = "End Date")]
        [DisplayFormat(DataFormatString = "{0:MMMM dd, yyyy}")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Total Days")]
        public int TotalDays => (EndDate - StartDate).Days;

        [Display(Name = "Total Price")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal TotalPrice { get; set; }

        [Display(Name = "Security Deposit")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal? Deposit { get; set; }

        // Party A (Owner)
        [Display(Name = "Owner ID")]
        public string PartyAId { get; set; }

        [Display(Name = "Owner Name")]
        public string PartyAName { get; set; }

        [Display(Name = "Owner Email")]
        [EmailAddress]
        public string PartyAEmail { get; set; }

        [Display(Name = "Owner Signature")]
        public bool PartyASigned { get; set; }

        public string PartyASignatureImage { get; set; }

        [Display(Name = "Signed At")]
        [DisplayFormat(DataFormatString = "{0:MMMM dd, yyyy hh:mm tt}")]
        public DateTime? PartyASignedAt { get; set; }

        // Party B (Renter)
        [Display(Name = "Renter ID")]
        public string PartyBId { get; set; }

        [Display(Name = "Renter Name")]
        public string PartyBName { get; set; }

        [Display(Name = "Renter Email")]
        [EmailAddress]
        public string PartyBEmail { get; set; }

        [Display(Name = "Renter Signature")]
        public bool PartyBSigned { get; set; }

        public string PartyBSignatureImage { get; set; }

        [Display(Name = "Signed At")]
        [DisplayFormat(DataFormatString = "{0:MMMM dd, yyyy hh:mm tt}")]
        public DateTime? PartyBSignedAt { get; set; }

        // Current User Info
        public bool IsCurrentUserPartyA { get; set; }
        public bool IsCurrentUserPartyB { get; set; }
        public bool CurrentUserHasSigned { get; set; }
    }
}