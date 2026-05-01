using System;

namespace SW_Project.ViewModels.Contract
{
    public class MyContractListItemVM
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ListingTitle { get; set; }
        public string ListingLocation { get; set; }
        public decimal TotalPrice { get; set; }
        public string PdfPath { get; set; }
        public string OtherPartyName { get; set; }
        public string OtherPartyRole { get; set; }
        public bool IsSignedByMe { get; set; }
        public bool IsSignedByOther { get; set; }
        public DateTime? SignedAt { get; set; }

    
        public string StatusColor => Status switch
        {
            "Active" => "#2ecc71",
            "Draft" => "#f39c12",
            "Completed" => "#3498db",
            _ => "#95a5a6"
        };

        public string StatusIcon => Status switch
        {
            "Active" => "bi-check-circle-fill",
            "Draft" => "bi-clock-history",
            "Completed" => "bi-check-all",
            _ => "bi-question-circle"
        };
    }
}