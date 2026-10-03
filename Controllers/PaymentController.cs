using SkyTech.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using SkyTech.Services;

namespace SkyTech.Controllers
{
    [AuthorizeUser]
    public class PaymentController : Controller
    {


        private readonly PayStackService _payStack;
        private readonly EmailServices _emailServices;
        private readonly ApplicationDbContext db;

        public PaymentController(ApplicationDbContext context, PayStackService payStack, EmailServices emailService)
        {
            db = context;
            _payStack = payStack; // ← injected, no more new PayStackService()
            _emailServices = emailService;
        }

        //--LEASE PAYMENENT
        public IActionResult LeasePaymentPage(int leaseAgreementId) //1
        {
            var agreement = db.LeaseAgreements
                .Include(a => a.Items)
                .Include(a => a.Customer)
                .FirstOrDefault(a => a.LeaseAgreementId == leaseAgreementId);

            if (agreement == null) return NotFound();

            if (agreement.Status != LeaseAgreementStatus.PendingPayment || !agreement.CustomerAgreed)
            {
                TempData["Error"] = "This lease is not ready for payment.";
                return RedirectToAction("ViewLeaseAgreement", "Customer", new { id = leaseAgreementId });
            }

            double amountDue = agreement.Items.Sum(i => i.DepositAtLease + i.MonthlyRateAtLease);

            var model = new LeasePaymentRequest
            {
                LeaseAgreementId = leaseAgreementId,
                Amount = (decimal)amountDue,
                CustomerEmail = agreement.Customer.Email,
                CustomerName = agreement.Customer.FirstName

            };
            return View(model);
        }

        // ── Additions to PaymentController — merge alongside LeasePaymentPage /
        // VerifyLeasePayment. Deliberately separate from those two: they're hard-wired
        // to the PendingPayment + CustomerAgreed + deposit-and-first-month case, which
        // a renewal (Active lease, no deposit, amount already computed) doesn't fit. ──

        // Renewal counterpart to LeasePaymentPage. Keyed by LeasePaymentId (not
        // LeaseAgreementId) since a lease can go through this more than once over
        // its life and we need to know exactly which pending fee we're collecting.
        public IActionResult RenewLeasePaymentPage(int leasePaymentId)
        {
            var payment = db.LeasePayments
                .Include(p => p.LeaseAgreement)
                    .ThenInclude(a => a.Customer)
                .FirstOrDefault(p => p.LeasePaymentId == leasePaymentId);

            if (payment == null) return NotFound();

            if (payment.Type != LeasePaymentType.RenewalFee || payment.Status != LeasePaymentStatus.Pending)
            {
                TempData["Error"] = "This renewal payment is no longer pending.";
                return RedirectToAction("ViewLeaseAgreement", "Customer", new { id = payment.LeaseAgreementId });
            }

            var agreement = payment.LeaseAgreement;
            if (agreement.Status != LeaseAgreementStatus.Active)
            {
                TempData["Error"] = "This lease is not active.";
                return RedirectToAction("ViewLeaseAgreement", "Customer", new { id = payment.LeaseAgreementId });
            }

            var model = new LeasePaymentRequest
            {
                LeaseAgreementId = agreement.LeaseAgreementId,
                LeasePaymentId = payment.LeasePaymentId,
                Amount = (decimal)payment.AmountDue,
                CustomerEmail = agreement.Customer.Email,
                CustomerName = agreement.Customer.FirstName
            };

            // NOTE: I don't have your LeasePaymentPage.cshtml, so I can't guarantee
            // this view name/markup lines up with your Paystack Pop.js snippet there.
            // Easiest path: copy that view to RenewLeasePaymentPage.cshtml and change
            // the popup's success callback to hit VerifyRenewalPayment instead of
            // VerifyLeasePayment, passing model.LeasePaymentId instead of the
            // LeaseAgreementId it currently sends.
            ViewBag.PublicKey = "pk_test_00e0ace8896dc183d605bed4ae78c29197fa3029"; // same test key ProcessPayment uses
            ViewBag.VerifyAction = "VerifyRenewalPayment"; // tells the shared view which action to call back on success
            return View(model);
        }



        // Renewal counterpart to VerifyLeasePayment. Same verification call and
        // response shape as the existing one — just settles a LeasePayment row and
        // extends the agreement instead of creating the agreement's first payment.
        public async Task<IActionResult> VerifyRenewalPayment(string reference, int leasePaymentId)
        {
            var result = await _payStack.VerifyTransaction(reference);
            if (result != null && result.status && result.data.status == "success")
            {
                var payment = db.LeasePayments
                    .Include(p => p.LeaseAgreement)
                        .ThenInclude(a => a.Customer)
                    .Include(p => p.LeaseAgreement)
                        .ThenInclude(a => a.Payments)
                    .FirstOrDefault(p => p.LeasePaymentId == leasePaymentId);

                if (payment == null || payment.Type != LeasePaymentType.RenewalFee)
                    return View("Failed");

                double amountPaid = (double)(result.data.amount / 100m);

                payment.AmountPaid = amountPaid;
                payment.PaidDate = DateTime.Now;
                payment.Status = LeasePaymentStatus.Paid;
                payment.PaystackTransactionReference = reference;

                var agreement = payment.LeaseAgreement;

                // Extend from the current EndDate, not from "now" — renewing early
                // shouldn't cost the customer the days between renewal and the old
                // end date.
                if (payment.RenewalTermMonths.HasValue)
                {
                    agreement.EndDate = agreement.EndDate.AddMonths(payment.RenewalTermMonths.Value);
                    agreement.TermMonths += payment.RenewalTermMonths.Value;
                }

                // TODO: there's currently no live Paystack subscription for this
                // agreement to update — VerifyLeasePayment never calls CreatePlan /
                // CreateSubscription on the initial payment either. Once that's
                // wired up (via PayStackSubscriptionService), call it here to adjust
                // the plan/subscription so future recurring charges match.

                db.SaveChanges();

                SendPaymentConfirmation(payment, agreement);

                ViewBag.Reference = reference;
                ViewBag.Amount = result.data.amount / 100m;
                ViewBag.Email = result.data.customer.email;

                return View("Success");
            }

            return View("Failed");
        }



        // ── Monthly installment payment — same shape as RenewLeasePaymentPage/
        // VerifyRenewalPayment above, but for Type == Recurring rows. Reuses the
        // same physical view file (RenewLeasePaymentPage.cshtml) since it's already
        // generic — just keyed by LeasePaymentId with the callback action name
        // passed via ViewBag.VerifyAction. ──
        public IActionResult PayMonthlyInstallmentPage(int leasePaymentId)
        {
            var payment = db.LeasePayments
                .Include(p => p.LeaseAgreement)
                    .ThenInclude(a => a.Customer)
                .FirstOrDefault(p => p.LeasePaymentId == leasePaymentId);

            if (payment == null) return NotFound();

            if (payment.Type != LeasePaymentType.Recurring || payment.Status != LeasePaymentStatus.Pending)
            {
                TempData["Error"] = "This payment is no longer pending.";
                return RedirectToAction("ViewLeaseAgreement", "Customer", new { id = payment.LeaseAgreementId });
            }

            var agreement = payment.LeaseAgreement;
            if (agreement.Status != LeaseAgreementStatus.Active)
            {
                TempData["Error"] = "This lease is not active.";
                return RedirectToAction("ViewLeaseAgreement", "Customer", new { id = payment.LeaseAgreementId });
            }

            var model = new LeasePaymentRequest
            {
                LeaseAgreementId = agreement.LeaseAgreementId,
                LeasePaymentId = payment.LeasePaymentId,
                Amount = (decimal)payment.AmountDue,
                CustomerEmail = agreement.Customer.Email,
                CustomerName = agreement.Customer.FirstName
            };

            ViewBag.PublicKey = "pk_test_00e0ace8896dc183d605bed4ae78c29197fa3029";
            ViewBag.VerifyAction = "VerifyMonthlyPayment";
            return View("RenewLeasePaymentPage", model); // same shared view, different callback
        }


        public async Task<IActionResult> VerifyMonthlyPayment(string reference, int leasePaymentId)
        {
            var result = await _payStack.VerifyTransaction(reference);
            if (result != null && result.status && result.data.status == "success")
            {
                var payment = db.LeasePayments
                    .Include(p => p.LeaseAgreement)
                        .ThenInclude(a => a.Customer)
                    .Include(p => p.LeaseAgreement)
                        .ThenInclude(a => a.Payments)
                    .FirstOrDefault(p => p.LeasePaymentId == leasePaymentId);

                if (payment == null || payment.Type != LeasePaymentType.Recurring)
                    return View("Failed");

                double amountPaid = (double)(result.data.amount / 100m);

                payment.AmountPaid = amountPaid;
                payment.PaidDate = DateTime.Now;
                payment.Status = LeasePaymentStatus.Paid;
                payment.PaystackTransactionReference = reference;

                db.SaveChanges();

                SendPaymentConfirmation(payment, payment.LeaseAgreement);

                ViewBag.Reference = reference;
                ViewBag.Amount = result.data.amount / 100m;
                ViewBag.Email = result.data.customer.email;

                return View("Success");
            }

            return View("Failed");
        }

        // Shared by VerifyRenewalPayment and VerifyMonthlyPayment. agreement must
        // have .Payments and .Customer loaded (both callers already Include them).
        private void SendPaymentConfirmation(LeasePayment payment, LeaseAgreement agreement)
        {
            var pdfBytes = SkyTech.Services.LeaseStatementPdfService.Generate(agreement, payment);
            var fileName = $"SkyTech-Lease-{agreement.LeaseAgreementId}-Statement.pdf";

            _emailServices.LeasePaymentConfirmation(
                agreement.Customer.Email,
                agreement.Customer.FirstName,
                payment.AmountPaid,
                pdfBytes,
                fileName);
        }


        public async Task<IActionResult> VerifyLeasePayment(string reference, int LeaseAgreementId) //2
        {
            var result = await _payStack.VerifyTransaction(reference);
            if (result != null && result.status && result.data.status == "success")
            {
                var agreement = db.LeaseAgreements
                    .Include(a => a.Items)
                    .ThenInclude(i => i.LeaseUnit)
                    .FirstOrDefault(a => a.LeaseAgreementId == LeaseAgreementId);

                if (agreement == null) return View("Failed");

                double amountPaid = (double)(result.data.amount / 100m);

                db.LeasePayments.Add(new LeasePayment
                {
                    LeaseAgreementId = agreement.LeaseAgreementId,
                    DueDate = agreement.StartDate,
                    AmountDue = agreement.Items.Sum(i => i.DepositAtLease + i.MonthlyRateAtLease),
                    AmountPaid = amountPaid,
                    PaidDate = DateTime.Now,
                    Type = LeasePaymentType.DepositAndFirstMonth,
                    Status = LeasePaymentStatus.Paid,
                    PaystackTransactionReference = reference
                });

                agreement.Status = LeaseAgreementStatus.Active;
                agreement.InitialTransactionReference = reference;

                //Move items From reserved to >>> On lease once payment is cleared
                foreach (var item in agreement.Items)
                {
                    if (item.LeaseUnit != null)
                    {
                        item.LeaseUnit.Status = LeaseUnitStatus.OnLease;
                    }
                }

                db.SaveChanges();

                ViewBag.Reference = reference;
                ViewBag.Amount = result.data.amount / 100m;
                ViewBag.Email = result.data.customer.email;

                return View("Success");
            }

            return View("Failed");
        } //THEEE ENDDDDD





        public IActionResult PaymentPage(string orderIds, decimal amount,
            string customerEmail, string customerName)
        {
            var model = new PaymentRequest
            {
                OrderIds = orderIds,
                Amount = amount,
                CustomerEmail = customerEmail,
                CustomerName = customerName
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(PaymentRequest model)
        {
            string reference = $"ORDER-{model.OrderId}-{DateTime.Now.Ticks}";
            string callbackUrl = Url.Action("Verify", "Payment",
                new { reference }, Request.Scheme);

            var initResult = await _payStack.InitializeTransaction(
                model.CustomerEmail, model.Amount, reference, callbackUrl);

            if (initResult != null && initResult.status)
            {
                HttpContext.Session.SetString("paystack_reference", initResult.data.reference);
                HttpContext.Session.SetString("order_amount", model.Amount.ToString());
                HttpContext.Session.SetInt32("order_id", model.OrderId);

                ViewBag.AccessCode = initResult.data.access_code;
                ViewBag.PublicKey = "pk_test_00e0ace8896dc183d605bed4ae78c29197fa3029";
                ViewBag.Email = model.CustomerEmail;
                ViewBag.AmountKobo = (int)(model.Amount * 100);
                ViewBag.CustomerName = model.CustomerName;
                ViewBag.Reference = initResult.data.reference;
                ViewBag.CallbackUrl = callbackUrl;

                return View("PaymentPage");
            }

            TempData["Error"] = "Could not initialize payment. Please try again.";
            return RedirectToAction("Checkout");
        }


        public async Task<IActionResult> Verify(string reference, string orderIds)
{
    var result = await _payStack.VerifyTransaction(reference);
    if (result != null && result.status && result.data.status == "success")
    {
        if (string.IsNullOrWhiteSpace(orderIds))
            return View("Failed");

        // Parse all order IDs
        var ids = orderIds.Split(',')
            .Select(id => int.TryParse(id.Trim(), out int x) ? x : 0)
            .Where(id => id > 0)
            .ToList();

        if (!ids.Any())
            return View("Failed");

        // Mark all orders as paid
        var orders = db.Orders.Where(o => ids.Contains(o.OrderId)).ToList();
        if (!orders.Any())
            return View("Failed");

        foreach (var order in orders)
        {
            order.IsPaid           = true;
            order.PaymentReference = reference;
            order.Status           = OrderStatus.Pending;
        }

                // Clear the cart using CustomerId from first order
                int customerId = orders.First().CustomerId ?? 0;
                var cart = db.Carts
            .Include(c => c.CartItems)
            .FirstOrDefault(c => c.CustomerId == customerId && !c.IsCheckedOut);

        if (cart != null)
        {
            db.CartItems.RemoveRange(cart.CartItems);
            cart.IsCheckedOut = true;
        }

        db.SaveChanges();

        ViewBag.Reference = reference;
        ViewBag.Amount    = result.data.amount / 100m;
        ViewBag.Email     = result.data.customer.email;

        return View("Success");
    }

    return View("Failed");
}

        public IActionResult Failed() => View();
    }
}
