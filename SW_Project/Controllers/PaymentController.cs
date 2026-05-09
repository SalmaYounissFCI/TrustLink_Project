using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using SW_Project.Interfaces;
using SW_Project.ViewModels.Payment;
using System.Security.Claims;
using Stripe;

namespace SW_Project.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _configuration;

        public PaymentController(IUnitOfWork unitOfWork, IPaymentService paymentService, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _paymentService = paymentService;
            _configuration = configuration;
        }

        // عرض صفحة الدفع
        [HttpGet]
        public async Task<IActionResult> Checkout(int contractId)
        {
            var contract = await _unitOfWork.Contracts.GetByIdAsync(contractId);

            if (contract == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (contract.PartyBId != userId)
                return Forbid();

            //  اتأكدي إن العقد Active
            if (contract.Status != "Active")
            {
                TempData["Error"] = "Contract must be signed by both parties before payment.";
                return RedirectToAction("Details", "Contracts", new { id = contractId });
            }

            // اتأكدي إن العقد لسه مدفوعش
            var isPaid = await _paymentService.IsContractPaidAsync(contractId);
            if (isPaid)
            {
                TempData["Error"] = "This contract has already been paid.";
                return RedirectToAction("Details", "Contracts", new { id = contractId });
            }


            var contractWithBooking = await _unitOfWork.Contracts.FindAsync(
                c => c.Id == contractId,
                c => c.Booking);

            var viewModel = new PaymentCheckoutVM
            {
                ContractId = contractId,
                ContractTitle = contract.Title,
                Amount = contractWithBooking?.Booking?.TotalPrice ?? 0,
                Currency = "usd",
                StripePublishableKey = _configuration["Stripe:PublishableKey"]
            };

            return View(viewModel);
        }

        // إنشاء جلسة دفع وتحويل المستخدم لـ Stripe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCheckoutSession(int contractId)
        {
            var contract = await _unitOfWork.Contracts.FindAsync(
                c => c.Id == contractId,
                c => c.Booking);

            if (contract == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var request = new PaymentRequestDTO
            {
                ContractId = contractId,
                UserId = userId,
                Amount = contract.Booking?.TotalPrice ?? 0,
                Currency = "usd",
                ItemName = contract.Title,
                SuccessUrl = Url.Action("Success", "Payment",
    new { contractId = contractId, session_id = "{CHECKOUT_SESSION_ID}" },
    Request.Scheme),
                CancelUrl = Url.Action("Cancel", "Payment",
                    new { contractId = contractId }, Request.Scheme)
            };

            var response = await _paymentService.CreateCheckoutSessionAsync(request);

            if (response.Success)
            {
                return Redirect(response.SessionUrl);
            }

            TempData["Error"] = response.Message;
            return RedirectToAction("Checkout", new { contractId });
        }
        // صفحة نجاح الدفع
        [HttpGet]
        public async Task<IActionResult> Success(int contractId)
        {
            // جيب آخر دفعة معلقة (مش محتاجة session_id)
            var payments = await _unitOfWork.Payments
                .FindAllAsync(p => p.ContractId == contractId && p.Status == "Pending");
            var payment = payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault();

            if (payment != null)
            {
                payment.Status = "Paid";
                payment.PaidAt = DateTime.Now;
                _unitOfWork.Payments.Update(payment);

                var contract = await _unitOfWork.Contracts.GetByIdAsync(contractId);
                if (contract != null)
                {
                    contract.Status = "Active";
                    _unitOfWork.Contracts.Update(contract);
                }
                await _unitOfWork.CompleteAsync();
            }

            return RedirectToAction("Details", "Contracts", new { id = contractId });
        }
        // صفحة إلغاء الدفع
        [HttpGet]
        public async Task<IActionResult> Cancel(int contractId)
        {
            var contract = await _unitOfWork.Contracts.GetByIdAsync(contractId);
            ViewBag.ContractTitle = contract?.Title;
            ViewBag.ContractId = contractId;
            return View();
        }

       
    }
}