using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkyTech.Services
{
    // New notifier methods needed on EmailServices (add alongside the lease
    // billing ones from before):
    //   void ReturnScheduled(string toEmail, string firstName, DateTime collectionDate);
    //   void EquipmentReceived(string toEmail, string firstName);
    //   void InspectionComplete(string toEmail, string firstName, double damageFee, double depositRefund);
    //   void DepositRefunded(string toEmail, string firstName, double amount, string transferStatus);
    //   void ReturnOrRenewReminder(string toEmail, string firstName, DateTime endDate); // the 3A reminder
    public class LeaseReturnService
    {
        private readonly ApplicationDbContext db;
        private readonly EmailServices emailService;
        private readonly PayStackTransferService transferService;
        private readonly ILeaseDamageAssessmentService damageAssessmentService;
        private readonly bool simulateTransfers;

        private const int ReturnReminderWindowDays = 14; // same window as the renewal banner, deliberately — this is the same decision point (renew vs return)

        public LeaseReturnService(
            ApplicationDbContext context,
            EmailServices emailService,
            PayStackTransferService transferService,
            ILeaseDamageAssessmentService damageAssessmentService,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            db = context;
            this.emailService = emailService;
            this.transferService = transferService;
            this.damageAssessmentService = damageAssessmentService;

            // Paystack Starter accounts can't use the Transfers API ("You cannot
            // initiate third party payouts as a starter business"). Setting
            // "PayStack:SimulateTransfers": true in appsettings.json skips the
            // real payout and records the refund as simulated instead. Off by
            // default so a real failure is never silently hidden.
            simulateTransfers = configuration.GetValue<bool>("PayStack:SimulateTransfers");
        }

        // ── Step 1: Customer schedules a return AND submits condition photos.
        // The AI runs immediately here — the customer never sees the result,
        // it's stored for the shop owner to review once the item physically
        // arrives (Step 3). conditionPhotos must be in the same order as
        // agreement.Items ends up in below (LeaseAgreementItemId ascending) —
        // the controller is responsible for that ordering. ──
        public async Task<(bool Success, string Error)> ScheduleReturnAsync(
            int leaseAgreementId, int customerId, DateTime scheduledDate,
            string bankCode, string accountNumber, string accountHolderName,
            List<(byte[] Bytes, string ContentType, string SavedPath)> conditionPhotos)
        {
            var agreement = await db.LeaseAgreements
                .Include(a => a.Items.OrderBy(i => i.LeaseAgreementItemId))
                    .ThenInclude(i => i.LeaseUnit)
                        .ThenInclude(u => u.LeaseProduct)
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.LeaseAgreementId == leaseAgreementId && a.CustomerId == customerId);

            if (agreement == null) return (false, "Lease agreement not found.");
            if (agreement.Status != LeaseAgreementStatus.Active) return (false, "Only active leases can be returned.");
            if (agreement.Items.Any(i => i.ReturnStatus != LeaseReturnStatus.Not_Due))
                return (false, "A return has already been scheduled for this lease.");

            var items = agreement.Items.OrderBy(i => i.LeaseAgreementItemId).ToList();
            if (conditionPhotos == null || conditionPhotos.Count != items.Count)
                return (false, $"Please attach one condition photo for each of the {items.Count} item(s) on this lease.");

            // Real recipient creation — this is the same PayStackTransferService
            // already used for driver/shop-owner payouts, just pointed at the
            // customer this time.
            var recipient = await transferService.CreateRecipient(accountHolderName, accountNumber, bankCode);
            if (!recipient.Success)
                return (false, $"Could not verify those bank details: {recipient.Message}");

            agreement.ReturnBankCode = bankCode;
            agreement.ReturnAccountNumber = accountNumber;
            agreement.ReturnAccountHolderName = accountHolderName;
            agreement.RefundRecipientCode = recipient.RecipientCode;

            // Early-return fee: 5% (or whatever the product's set to) of the
            // rent still owed for the months left on the term. Only applies
            // if the chosen collection date is actually before EndDate —
            // returning on or after EndDate isn't "early".
            double totalEarlyFee = 0;
            int remainingMonths = MonthsBetween(scheduledDate, agreement.EndDate);

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var photo = conditionPhotos[i];

                item.ReturnStatus = LeaseReturnStatus.Return_Pending;
                item.ScheduledCollectionDate = scheduledDate;

                if (remainingMonths > 0)
                {
                    double pct = item.LeaseUnit?.LeaseProduct?.EarlyReturnFeePercentage ?? 5;
                    double fee = (pct / 100.0) * remainingMonths * item.MonthlyRateAtLease;
                    item.EarlyReturnFee = fee;
                    totalEarlyFee += fee;
                }

                // AI assessment runs now, off the customer's own photo — not
                // shown to them anywhere. The shop owner sees this once they
                // open InspectReturn after logging the item as received.
                string itemName = item.LeaseUnit?.LeaseProduct?.LeaseProdName ?? "leased equipment";
                var assessment = await damageAssessmentService.AnalyzeAsync(photo.Bytes, photo.ContentType, itemName, item.DepositAtLease);

                item.ReturnConditionImagePath = photo.SavedPath;
                item.AiDamageAssessment = assessment.Condition;
                item.AiConfidencePercent = assessment.ConfidencePercent;
                item.AiSuggestedDamageFee = assessment.SuggestedDamageFee;
                item.AiNotes = assessment.Notes;

                // Pre-fill with the AI's number — ConfirmInspectionAsync is
                // where the shop owner either accepts this or overrides it.
                item.DamageFee = assessment.SuggestedDamageFee;
                item.ConditionAtReturn = assessment.Condition.ToString();
            }

            await db.SaveChangesAsync();

            // Deliberately generic — no condition/fee details, regardless of
            // what the AI found.
            emailService.ReturnScheduled(agreement.Customer.Email, agreement.Customer.FirstName, scheduledDate, totalEarlyFee);

            return (true, null);
        }

        // Whole calendar months between two dates, rounding a partial month
        // UP — e.g. 10 months and 3 days remaining counts as 11 months owed,
        // not 10, since that partial month of rent was still going to be paid.
        private static int MonthsBetween(DateTime from, DateTime to)
        {
            if (to <= from) return 0;
            int months = (to.Year - from.Year) * 12 + (to.Month - from.Month);
            if (to.Day > from.Day) months++;
            return Math.Max(0, months);
        }

        // ── Step 2: Staff logs the equipment as received ────────────────
        public async Task<(bool Success, string Error)> LogReturnedAsync(int leaseAgreementItemId, int ownerId)
        {
            var item = await db.LeaseAgreementItems
                .Include(i => i.LeaseAgreement)
                    .ThenInclude(a => a.Customer)
                .FirstOrDefaultAsync(i => i.LeaseAgreementItemId == leaseAgreementItemId
                                       && i.LeaseAgreement.OwnerId == ownerId);

            if (item == null) return (false, "Item not found.");
            if (item.ReturnStatus != LeaseReturnStatus.Return_Pending)
                return (false, "This item isn't awaiting collection.");

            item.ReturnStatus = LeaseReturnStatus.Item_Collected;
            item.ActualCollectionDate = DateTime.Now;

            await db.SaveChangesAsync();

            emailService.EquipmentReceived(item.LeaseAgreement.Customer.Email, item.LeaseAgreement.Customer.FirstName);

            return (true, null);
        }

        // ── Step 3/4: Shop owner reviews — AI assessment already ran at
        // ScheduleReturnAsync, off the customer's own photo. Nothing to
        // trigger here; see ConfirmInspectionAsync below. ──

        // ── Step 3/4: shop owner confirms (or overrides) the AI's suggested
        // fee — computed back in ScheduleReturnAsync off the customer's own
        // photo — and closes out this item's inspection. Requires the item to
        // actually be physically received first; the AI read alone isn't
        // enough to finalize a fee. ──
        public async Task<(bool Success, string Error)> ConfirmInspectionAsync(
            int leaseAgreementItemId, int ownerId, double finalDamageFee, string overrideReason)
        {
            var item = await db.LeaseAgreementItems
                .Include(i => i.LeaseAgreement)
                    .ThenInclude(a => a.Customer)
                .FirstOrDefaultAsync(i => i.LeaseAgreementItemId == leaseAgreementItemId
                                       && i.LeaseAgreement.OwnerId == ownerId);

            if (item == null) return (false, "Item not found.");
            if (item.ReturnStatus != LeaseReturnStatus.Item_Collected)
                return (false, "This item hasn't been logged as received yet.");

            bool overridden = item.AiSuggestedDamageFee.HasValue &&
                Math.Abs(finalDamageFee - item.AiSuggestedDamageFee.Value) > 0.01;

            item.DamageFee = finalDamageFee;
            item.StaffOverride = overridden;
            item.StaffOverrideReason = overridden ? overrideReason : null;
            item.ReturnStatus = LeaseReturnStatus.Inspection_Complete;

            await db.SaveChangesAsync();

            double depositRefund = Math.Max(0, item.DepositAtLease - finalDamageFee);
            emailService.InspectionComplete(item.LeaseAgreement.Customer.Email, item.LeaseAgreement.Customer.FirstName,
                finalDamageFee, depositRefund);

            return (true, null);
        }

        // ── Step 5/6: Admin processes the refund, closes the agreement, and
        // returns fit units to inventory ──────────────────────────────────
        public async Task<(bool Success, string Error)> ProcessDepositRefundAsync(int leaseAgreementId)
        {
            var agreement = await db.LeaseAgreements
                .Include(a => a.Customer)
                .Include(a => a.Items)
                    .ThenInclude(i => i.LeaseUnit)
                .FirstOrDefaultAsync(a => a.LeaseAgreementId == leaseAgreementId);

            if (agreement == null) return (false, "Lease agreement not found.");
            if (agreement.Items.Any(i => i.ReturnStatus != LeaseReturnStatus.Inspection_Complete))
                return (false, "Every item must finish inspection before a refund can be processed.");
            if (string.IsNullOrEmpty(agreement.RefundRecipientCode))
                return (false, "No payout recipient on file for this agreement.");

            double totalRefund = agreement.Items.Sum(i =>
                Math.Max(0, i.DepositAtLease - (i.DamageFee ?? 0) - (i.EarlyReturnFee ?? 0)));

            if (totalRefund > 0)
            {
                var reference = $"LEASE-REFUND-{agreement.LeaseAgreementId}-{DateTime.Now.Ticks}";

                if (simulateTransfers)
                {
                    // No API call — Starter accounts can't make payouts. The
                    // refund is recorded in full (amount, reference, status) so
                    // the rest of the flow and the audit trail behave normally,
                    // but the status makes it plain that no money actually moved.
                    agreement.RefundTransferCode = $"SIMULATED-{reference}";
                    agreement.RefundTransferStatus = "simulated";
                }
                else
                {
                    var transfer = await transferService.InitiateTransfer(
                        agreement.RefundRecipientCode, totalRefund, "Lease deposit refund", reference);

                    if (!transfer.Success)
                        return (false, $"Transfer failed (SimulateTransfers seen as: {simulateTransfers}): {transfer.Message}");

                    agreement.RefundTransferCode = transfer.TransferCode;
                    agreement.RefundTransferStatus = transfer.Status;
                }
            }
            else
            {
                agreement.RefundTransferStatus = "not_required"; // damage fees consumed the entire deposit
            }

            foreach (var item in agreement.Items)
            {
                item.DepositRefunded = Math.Max(0, item.DepositAtLease - (item.DamageFee ?? 0) - (item.EarlyReturnFee ?? 0));
                item.ReturnStatus = LeaseReturnStatus.Completed;

                if (item.LeaseUnit != null)
                {
                    // Severe damage keeps the unit out of circulation; anything
                    // else goes straight back to Available.
                    item.LeaseUnit.Status = item.AiDamageAssessment == ReturnConditionAssessment.Severe
                        ? LeaseUnitStatus.Maintenance
                        : LeaseUnitStatus.Available;
                }
            }

            agreement.Status = LeaseAgreementStatus.Completed;

            await db.SaveChangesAsync();

            emailService.DepositRefunded(agreement.Customer.Email, agreement.Customer.FirstName,
                totalRefund, agreement.RefundTransferStatus);

            return (true, null);
        }

        // ── Registered as a second Hangfire recurring job (see setup notes) —
        // the "renew or return" nudge, same 14-day window as the renewal
        // banner since it's the same decision point for the customer. ──
        public async Task RunReturnReminderCheckAsync()
        {
            var candidates = await db.LeaseAgreements
                .Include(a => a.Customer)
                .Include(a => a.Items)
                .Where(a => a.Status == LeaseAgreementStatus.Active)
                .ToListAsync();

            foreach (var agreement in candidates)
            {
                bool withinWindow = (agreement.EndDate - DateTime.Now).TotalDays <= ReturnReminderWindowDays
                    && agreement.EndDate > DateTime.Now;
                bool returnAlreadyScheduled = agreement.Items.Any(i => i.ReturnStatus != LeaseReturnStatus.Not_Due);

                if (withinWindow && !returnAlreadyScheduled && agreement.ReturnReminderSentOn == null)
                {
                    emailService.ReturnOrRenewReminder(agreement.Customer.Email, agreement.Customer.FirstName, agreement.EndDate);
                    agreement.ReturnReminderSentOn = DateTime.Now;
                }
            }

            await db.SaveChangesAsync();
        }
    }
}