namespace SW_Project.ViewModels.Payment
{
    public class PaymentRequestDTO
    {
        public int ContractId { get; set; }
        public string UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "usd";
        public string ItemName { get; set; }
        public string SuccessUrl { get; set; }
        public string CancelUrl { get; set; }
    }
}