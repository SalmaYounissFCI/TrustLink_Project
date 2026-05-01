using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using SW_Project.Data;
using SW_Project.Models;
using SW_Project.ViewModels.Contract;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SW_Project.Controllers
{
    [Authorize]
    public class ContractsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ContractsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        // عرض تفاصيل العقد
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var contract = await _context.Contracts
                .Include(c => c.Booking)
                    .ThenInclude(b => b.Listing)
                        .ThenInclude(l => l.Category)
                .Include(c => c.ContractSignatures)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (contract == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            var userIsParty = userId == contract.PartyAId || userId == contract.PartyBId;
            if (!User.Identity.IsAuthenticated || !userIsParty)
                return Forbid();

            var signatures = await _context.ContractSignatures
                .Where(s => s.ContractId == id)
                .ToListAsync();

            var partyASig = signatures.FirstOrDefault(s => s.UserId == contract.PartyAId);
            var partyBSig = signatures.FirstOrDefault(s => s.UserId == contract.PartyBId);

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
                PartyAName = (await _userManager.FindByIdAsync(contract.PartyAId))?.Name ?? "Owner",
                PartyAEmail = (await _userManager.FindByIdAsync(contract.PartyAId))?.Email,
                PartyASigned = partyASig != null,
                PartyASignatureImage = partyASig?.SignatureImage,
                PartyASignedAt = partyASig?.SignedAt,
                PartyBId = contract.PartyBId,
                PartyBName = (await _userManager.FindByIdAsync(contract.PartyBId))?.Name ?? "Renter",
                PartyBEmail = (await _userManager.FindByIdAsync(contract.PartyBId))?.Email,
                PartyBSigned = partyBSig != null,
                PartyBSignatureImage = partyBSig?.SignatureImage,
                PartyBSignedAt = partyBSig?.SignedAt,
                IsCurrentUserPartyA = userId == contract.PartyAId,
                IsCurrentUserPartyB = userId == contract.PartyBId,
                CurrentUserHasSigned = (userId == contract.PartyAId && partyASig != null) ||
                                       (userId == contract.PartyBId && partyBSig != null)
            };

            return View(viewModel);
        }

        // GET: صفحة التوقيع
        public async Task<IActionResult> Sign(int id)
        {
            var contract = await _context.Contracts
                .Include(c => c.Booking)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (contract == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            if (userId != contract.PartyAId && userId != contract.PartyBId)
                return Forbid();

            var existingSig = await _context.ContractSignatures.AnyAsync(s => s.ContractId == id && s.UserId == userId);
            if (existingSig)
                return RedirectToAction("Details", new { id });

            var viewModel = new ContractSignVM
            {
                ContractId = contract.Id,
                Title = contract.Title,
                Terms = contract.Terms,
                PartyName = userId == contract.PartyAId ?
                    (await _userManager.FindByIdAsync(contract.PartyAId))?.Name :
                    (await _userManager.FindByIdAsync(contract.PartyBId))?.Name,
                PartyRole = userId == contract.PartyAId ? "Owner" : "Renter"
            };

            return View(viewModel);
        }

        // POST: حفظ التوقيع
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sign(ContractSignVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var contract = await _context.Contracts
                .Include(c => c.Booking)
                    .ThenInclude(b => b.Listing)
                .FirstOrDefaultAsync(c => c.Id == model.ContractId);
            if (contract == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            if (userId != contract.PartyAId && userId != contract.PartyBId)
                return Forbid();

            var existing = await _context.ContractSignatures.AnyAsync(s => s.ContractId == model.ContractId && s.UserId == userId);
            if (existing)
                return BadRequest("Already signed.");

            var signature = new ContractSignature
            {
                ContractId = model.ContractId,
                UserId = userId,
                SignatureImage = model.SignatureBase64,
                SignedAt = DateTime.Now
            };
            _context.ContractSignatures.Add(signature);
            await _context.SaveChangesAsync();

            var signaturesCount = await _context.ContractSignatures.CountAsync(s => s.ContractId == model.ContractId);
            if (signaturesCount == 2)
            {
                contract.Status = "Active";
                await _context.SaveChangesAsync();
                await GenerateAndSendContractPdf(contract.Id);
            }

            TempData["Success"] = "Your signature has been saved.";
            return RedirectToAction("Details", new { id = model.ContractId });
        }

        // إنشاء PDF وإرساله بالبريد
        private async Task GenerateAndSendContractPdf(int contractId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("===== GenerateAndSendContractPdf START =====");

                var contract = await _context.Contracts
                    .Include(c => c.Booking)
                        .ThenInclude(b => b.Listing)
                            .ThenInclude(l => l.Owner)
                    .Include(c => c.Booking.Renter)
                    .Include(c => c.ContractSignatures)
                    .FirstOrDefaultAsync(c => c.Id == contractId);

                if (contract == null)
                {
                    System.Diagnostics.Debug.WriteLine("Contract not found!");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Generating PDF for contract ID: {contractId}");

                // ✅ تأكدي من وجود ملف _ContractPdf.cshtml في مجلد Views/Contracts
                var pdfBytes = await new ViewAsPdf("_ContractPdf", contract)
                {
                    PageSize = Rotativa.AspNetCore.Options.Size.A4,
                    PageMargins = { Left = 15, Right = 15, Top = 20, Bottom = 20 }
                }.BuildFile(ControllerContext);

                System.Diagnostics.Debug.WriteLine($"PDF generated, size: {pdfBytes.Length} bytes");

                var pdfDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "contracts");
                if (!Directory.Exists(pdfDir))
                    Directory.CreateDirectory(pdfDir);

                var pdfPath = Path.Combine(pdfDir, $"contract_{contractId}.pdf");
                System.IO.File.WriteAllBytes(pdfPath, pdfBytes);
                contract.PdfPath = $"/contracts/contract_{contractId}.pdf";
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"PDF saved at: {contract.PdfPath}");

                // ✅ إرسال الإيميل (اختياري – لو عايزة تشتغل)
                try
                {
                    var partyA = await _userManager.FindByIdAsync(contract.PartyAId);
                    var partyB = await _userManager.FindByIdAsync(contract.PartyBId);
                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    var pdfLink = $"{baseUrl}{contract.PdfPath}";
                    var subject = $"Your contract is ready – {contract.Title}";
                    var body = $@"<p>Dear {partyA?.Name ?? "Party A"} / {partyB?.Name ?? "Party B"},</p>
                         <p>The contract for <strong>{contract.Booking.Listing.Title}</strong> has been signed by both parties.</p>
                         <p>You can download the final PDF here: <a href='{pdfLink}'>Download Contract</a></p>
                         <p>Thank you for using TrustLink.</p>";

                    if (partyA?.Email != null)
                        await _emailSender.SendEmailAsync(partyA.Email, subject, body);
                    if (partyB?.Email != null)
                        await _emailSender.SendEmailAsync(partyB.Email, subject, body);

                    System.Diagnostics.Debug.WriteLine("Email sent successfully");
                }
                catch (Exception emailEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Email error: {emailEx.Message}");
                    // لا نوقف العملية لو فشل الإيميل – المهم الـ PDF اتحفظ
                }

                System.Diagnostics.Debug.WriteLine("===== GenerateAndSendContractPdf END =====");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in GenerateAndSendContractPdf: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        // GET: /Contracts/MyContracts
        [Authorize]
        public async Task<IActionResult> MyContracts()
        {
            var userId = _userManager.GetUserId(User);

            var contracts = await _context.Contracts
                .Include(c => c.Booking)
                    .ThenInclude(b => b.Listing)
                        .ThenInclude(l => l.Category)
                .Include(c => c.ContractSignatures)
                .Where(c => c.PartyAId == userId || c.PartyBId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            // تحويل البيانات إلى ViewModel
            var viewModel = contracts.Select(c => new MyContractListItemVM
            {
                Id = c.Id,
                Title = c.Title,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                ListingTitle = c.Booking.Listing.Title,
                ListingLocation = c.Booking.Listing.Location,
                TotalPrice = c.Booking.TotalPrice,
                PdfPath = c.PdfPath,
                OtherPartyName = c.PartyAId == userId ?
                    (_context.Users.Find(c.PartyBId)?.Name ?? "Renter") :
                    (_context.Users.Find(c.PartyAId)?.Name ?? "Owner"),
                OtherPartyRole = c.PartyAId == userId ? "Renter" : "Owner",
                IsSignedByMe = c.ContractSignatures.Any(s => s.UserId == userId),
                IsSignedByOther = c.ContractSignatures.Count() == 2,
                SignedAt = c.ContractSignatures.FirstOrDefault(s => s.UserId == userId)?.SignedAt
            }).ToList();

            ViewBag.TotalContracts = contracts.Count;
            ViewBag.ActiveContracts = contracts.Count(c => c.Status == "Active");
            ViewBag.PendingContracts = contracts.Count(c => c.Status == "Draft");
            ViewBag.CompletedContracts = contracts.Count(c => c.Status == "Completed");

            return View(viewModel);
        }

    }

}