namespace SW_Project.ViewModels.Payment
{
    public class PaymentResponseDTO
    {
        public string SessionId { get; set; }
        public string SessionUrl { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}