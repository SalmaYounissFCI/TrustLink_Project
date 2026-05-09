using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Rotativa.AspNetCore;
using SW_Project.Interfaces;
using SW_Project.Models;
using SW_Project.Services;
using SW_Project.ViewModels.Contract;

namespace SW_Project.Controllers
{
    [Authorize]
    public class ContractsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IPaymentService _paymentService;

        public ContractsController(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            IWebHostEnvironment webHostEnvironment,
            IPaymentService paymentService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _emailSender = emailSender;
            _webHostEnvironment = webHostEnvironment;
            _paymentService = paymentService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var contract = await _unitOfWork.Contracts.FindAsync(
                c => c.Id == id,
                c => c.Booking,
                c => c.Booking.Listing,
                c => c.Booking.Listing.Category,
                c => c.ContractSignatures);

            if (contract == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            var userIsParty = userId == contract.PartyAId || userId == contract.PartyBId;

            var isPaid = await _paymentService.IsContractPaidAsync(id);

            if (!User.Identity?.IsAuthenticated == true || !userIsParty)
                return Forbid();

            var signatures = await _unitOfWork.ContractSignatures.FindAllAsync(s => s.ContractId == id);

            var partyASig = signatures.FirstOrDefault(s => s.UserId == contract.PartyAId);
            var partyBSig = signatures.FirstOrDefault(s => s.UserId == contract.PartyBId);

            var partyA = await _unitOfWork.Users.GetByIdAsync(contract.PartyAId);
            var partyB = await _unitOfWork.Users.GetByIdAsync(contract.PartyBId);

            var viewModel = new ContractDetailsVM
            {
                Id = contract.Id,
                Title = contract.Title,
                Status = contract.Status,
                Terms = contract.Terms,
                PdfPath = contract.PdfPath,
                CreatedAt = contract.CreatedAt,
                BookingId = contract.BookingId,
                ListingTitle = contract.Booking.Listing.Title,
                ListingDescription = contract.Booking.Listing.Description,
                ListingLocation = contract.Booking.Listing.Location,
                PricePerDay = contract.Booking.Listing.PricePerDay,
                StartDate = contract.Booking.StartDate,
                EndDate = contract.Booking.EndDate,
                TotalPrice = contract.Booking.TotalPrice,
                Deposit = contract.Booking.Listing.Deposit,
                PartyAId = contract.PartyAId,
                PartyAName = partyA?.Name ?? "Owner",
                PartyAEmail = partyA?.Email,
                PartyASigned = partyASig != null,
                PartyASignatureImage = partyASig?.SignatureImage,
                PartyASignedAt = partyASig?.SignedAt,
                PartyBId = contract.PartyBId,
                PartyBName = partyB?.Name ?? "Renter",
                PartyBEmail = partyB?.Email,
                PartyBSigned = partyBSig != null,
                PartyBSignatureImage = partyBSig?.SignatureImage,
                PartyBSignedAt = partyBSig?.SignedAt,
                IsCurrentUserPartyA = userId == contract.PartyAId,
                IsCurrentUserPartyB = userId == contract.PartyBId,
                CurrentUserHasSigned = (userId == contract.PartyAId && partyASig != null) ||
                                       (userId == contract.PartyBId && partyBSig != null),
                                         IsCurrentUserRenter = contract.PartyBId == userId

            };
            ViewBag.IsPaid = isPaid;

            return View(viewModel);
        }

        public async Task<IActionResult> Sign(int id)
        {
            var contract = await _unitOfWork.Contracts.FindAsync(
                c => c.Id == id,
                c => c.Booking);

            if (contract == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            if (userId != contract.PartyAId && userId != contract.PartyBId)
                return Forbid();

            var existingSig = await _unitOfWork.ContractSignatures.ExistsAsync(s => s.ContractId == id && s.UserId == userId);
            if (existingSig)
                return RedirectToAction("Details", new { id });

            var party = await _unitOfWork.Users.GetByIdAsync(userId);

            var viewModel = new ContractSignVM
            {
                ContractId = contract.Id,
                Title = contract.Title,
                Terms = contract.Terms,
                PartyName = party?.Name,
                PartyRole = userId == contract.PartyAId ? "Owner" : "Renter"
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sign(ContractSignVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var contract = await _unitOfWork.Contracts.FindAsync(
                c => c.Id == model.ContractId,
                c => c.Booking,
                c => c.Booking.Listing);

            if (contract == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            if (userId != contract.PartyAId && userId != contract.PartyBId)
                return Forbid();

            var existing = await _unitOfWork.ContractSignatures.ExistsAsync(s => s.ContractId == model.ContractId && s.UserId == userId);
            if (existing)
                return BadRequest("Already signed.");

            var signature = new ContractSignature
            {
                ContractId = model.ContractId,
                UserId = userId,
                SignatureImage = model.SignatureBase64,
                SignedAt = DateTime.Now
            };

            await _unitOfWork.ContractSignatures.AddAsync(signature);
            await _unitOfWork.CompleteAsync();

            var signaturesCount = await _unitOfWork.ContractSignatures.CountAsync(s => s.ContractId == model.ContractId);

            if (signaturesCount == 2)
            {
                contract.Status = "Active";
                _unitOfWork.Contracts.Update(contract);
                await _unitOfWork.CompleteAsync();
                await GenerateAndSendContractPdf(contract.Id);
            }

            TempData["Success"] = "Your signature has been saved.";
            return RedirectToAction("Details", new { id = model.ContractId });
        }

        private async Task GenerateAndSendContractPdf(int contractId)
        {
            try
            {
                var contract = await _unitOfWork.Contracts.FindAsync(
                    c => c.Id == contractId,
                    c => c.Booking,
                    c => c.Booking.Listing,
                    c => c.Booking.Listing.Owner,
                    c => c.Booking.Renter,
                    c => c.ContractSignatures);

                if (contract == null) return;

                var pdfBytes = await new ViewAsPdf("_ContractPdf", contract)
                {
                    PageSize = Rotativa.AspNetCore.Options.Size.A4,
                    PageMargins = { Left = 15, Right = 15, Top = 20, Bottom = 20 }
                }.BuildFile(ControllerContext);

                var pdfDir = Path.Combine(_webHostEnvironment.WebRootPath, "contracts");
                if (!Directory.Exists(pdfDir))
                    Directory.CreateDirectory(pdfDir);

                var pdfPath = Path.Combine(pdfDir, $"contract_{contractId}.pdf");
                await System.IO.File.WriteAllBytesAsync(pdfPath, pdfBytes);

                contract.PdfPath = $"/contracts/contract_{contractId}.pdf";
                _unitOfWork.Contracts.Update(contract);
                await _unitOfWork.CompleteAsync();

                var partyA = await _unitOfWork.Users.GetByIdAsync(contract.PartyAId);
                var partyB = await _unitOfWork.Users.GetByIdAsync(contract.PartyBId);
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var pdfLink = $"{baseUrl}{contract.PdfPath}";
                var subject = $"Your contract is ready – {contract.Title}";
                var body = $@"<p>Dear {partyA?.Name ?? "Party A"} / {partyB?.Name ?? "Party B"},</p>
                             <p>The contract for <strong>{contract.Booking.Listing.Title}</strong> has been signed by both parties.</p>
                             <p>You can download the final PDF here: <a href='{pdfLink}'>Download Contract</a></p>";

                if (partyA?.Email != null)
                    await _emailSender.SendEmailAsync(partyA.Email, subject, body);
                if (partyB?.Email != null)
                    await _emailSender.SendEmailAsync(partyB.Email, subject, body);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR generating PDF: {ex.Message}");
            }
        }

        [Authorize]
        public async Task<IActionResult> MyContracts()
        {
            var userId = _userManager.GetUserId(User);
            var today = DateTime.Today;

            var contracts = await _unitOfWork.Contracts.FindAllAsync(
                c => c.PartyAId == userId || c.PartyBId == userId,
                c => c.Booking,
                c => c.Booking.Listing,
                c => c.Booking.Listing.Category,
                c => c.ContractSignatures);

            // Update expired contracts
            foreach (var contract in contracts.Where(c => c.Status == "Active" && c.Booking.EndDate < today))
            {
                contract.Status = "Expired";
                _unitOfWork.Contracts.Update(contract);
            }

            if (contracts.Any(c => c.Status == "Expired"))
            {
                await _unitOfWork.CompleteAsync();
            }

            var orderedContracts = contracts.OrderByDescending(c => c.CreatedAt).ToList();

            var viewModel = new List<MyContractListItemVM>();

            foreach (var c in orderedContracts)
            {
                var otherPartyId = c.PartyAId == userId ? c.PartyBId : c.PartyAId;
                var otherParty = await _unitOfWork.Users.GetByIdAsync(otherPartyId);

                viewModel.Add(new MyContractListItemVM
                {
                    Id = c.Id,
                    Title = c.Title,
                    Status = c.Status,
                    CreatedAt = c.CreatedAt,
                    ListingTitle = c.Booking.Listing.Title,
                    ListingLocation = c.Booking.Listing.Location,
                    TotalPrice = c.Booking.TotalPrice,
                    PdfPath = c.PdfPath,
                    OtherPartyName = otherParty?.Name ?? (c.PartyAId == userId ? "Renter" : "Owner"),
                    OtherPartyRole = c.PartyAId == userId ? "Renter" : "Owner",
                    IsSignedByMe = c.ContractSignatures.Any(s => s.UserId == userId),
                    IsSignedByOther = c.ContractSignatures.Count() == 2,
                    SignedAt = c.ContractSignatures.FirstOrDefault(s => s.UserId == userId)?.SignedAt,
                      IsPaid = await _paymentService.IsContractPaidAsync(c.Id),
                       IsCurrentUserRenter = c.PartyBId == userId
                });
            }

            ViewBag.TotalContracts = contracts.Count();
            ViewBag.ActiveContracts = contracts.Count(c => c.Status == "Active");
            ViewBag.PendingContracts = contracts.Count(c => c.Status == "Draft");
            ViewBag.CompletedContracts = contracts.Count(c => c.Status == "Completed");

            return View(viewModel);
        }
    }
}