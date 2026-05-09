namespace SW_Project.ViewModels.Payment
{
    public class PaymentCheckoutVM
    {
        public int ContractId { get; set; }
        public string ContractTitle { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "usd";
        public string StripePublishableKey { get; set; }
    }
}