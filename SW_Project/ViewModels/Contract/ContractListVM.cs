using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SW_Project.ViewModels.Contract
{
    public class ContractListVM
    {
        public ContractListVM()
        {
            Contracts = new List<ContractListItemVM>();
        }

        public List<ContractListItemVM> Contracts { get; set; }
        public int TotalCount { get; set; }

        // Filters
        [Display(Name = "Filter by Status")]
        public string StatusFilter { get; set; }

        public string Role { get; set; } // "Owner" or "Renter"
    }

    public class ContractListItemVM
    {
        public int Id { get; set; }

        [Display(Name = "Title")]
        public string Title { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; }

        [Display(Name = "Created Date")]
        [DisplayFormat(DataFormatString = "{0:MMMM dd, yyyy}")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Item")]
        public string ListingTitle { get; set; }

        [Display(Name = "Other Party")]
        public string OtherPartyName { get; set; }

        [Display(Name = "Total Amount")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal TotalPrice { get; set; }

        public string PdfPath { get; set; }
        public bool IsSignedByMe { get; set; }
        public bool IsSignedByOther { get; set; }
    }
}