using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using SW_Project.Interfaces;
using SW_Project.Models;
using SW_Project.ViewModels.Payment;

namespace SW_Project.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public PaymentService(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;

          
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public async Task<PaymentResponseDTO> CreateCheckoutSessionAsync(PaymentRequestDTO request)
        {
            try
            {
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = request.Currency.ToLower(),
                                UnitAmount = (long)(request.Amount * 100),
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = request.ItemName,
                                    Description = $"Contract #{request.ContractId}"
                                },
                            },
                            Quantity = 1,
                        },
                    },
                    Mode = "payment",
                    SuccessUrl = request.SuccessUrl,
                    CancelUrl = request.CancelUrl,
                    Metadata = new Dictionary<string, string>
                    {
                        { "contract_id", request.ContractId.ToString() },
                        { "user_id", request.UserId }
                    }
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                // حفظ معلومات الدفع
                var payment = new Payment
                {
                    ContractId = request.ContractId,
                    UserId = request.UserId,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    StripeSessionId = session.Id,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.Payments.AddAsync(payment);
                await _unitOfWork.CompleteAsync();

                return new PaymentResponseDTO
                {
                    SessionId = session.Id,
                    SessionUrl = session.Url,
                    Success = true
                };
            }
            catch (StripeException ex)
            {
                return new PaymentResponseDTO
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<bool> HandleSuccessfulPaymentAsync(string sessionId)
        {
            var payments = await _unitOfWork.Payments.FindAllAsync(p => p.StripeSessionId == sessionId);
            var payment = payments.FirstOrDefault();

            if (payment == null) return false;

            if (payment.Status == "Paid") return true;

            payment.Status = "Paid";
            payment.PaidAt = DateTime.Now;

            var sessionService = new SessionService();
            var session = await sessionService.GetAsync(sessionId);
            payment.StripePaymentIntentId = session.PaymentIntentId;

            _unitOfWork.Payments.Update(payment);

            // تحديث حالة العقد لـ Active
            var contract = await _unitOfWork.Contracts.GetByIdAsync(payment.ContractId);
            if (contract != null)
            {
                contract.Status = "Active";
                _unitOfWork.Contracts.Update(contract);
            }

            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<Payment?> GetPaymentBySessionIdAsync(string sessionId)
        {
            var payments = await _unitOfWork.Payments.FindAllAsync(p => p.StripeSessionId == sessionId);
            return payments.FirstOrDefault();
        }

        public async Task<Payment?> GetPaymentByContractIdAsync(int contractId)
        {
            var payments = await _unitOfWork.Payments.FindAllAsync(p => p.ContractId == contractId);
            return payments.FirstOrDefault();
        }

        public async Task<bool> IsContractPaidAsync(int contractId)
        {
            return await _unitOfWork.Payments.ExistsAsync(p => p.ContractId == contractId && p.Status == "Paid");
        }
    }
}