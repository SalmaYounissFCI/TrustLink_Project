using SW_Project.Models;
using SW_Project.ViewModels.Payment;

namespace SW_Project.Interfaces
{
    public interface IPaymentService
    {
   
        Task<PaymentResponseDTO> CreateCheckoutSessionAsync(PaymentRequestDTO request);

        
        Task<bool> HandleSuccessfulPaymentAsync(string sessionId);

   
        Task<Payment?> GetPaymentBySessionIdAsync(string sessionId);

 
        Task<Payment?> GetPaymentByContractIdAsync(int contractId);

        Task<bool> IsContractPaidAsync(int contractId);
    }
}