
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using System;
using System.Linq;
using System.Threading.Tasks;


namespace SkyTech.Services
{
    public interface ILeaseBillingNotifier
    {
        void PaymentReminder(string toEmail, string firstName, double amountDue, DateTime dueDate);
        void LateFeeWarning(string toEmail, string firstName, double lateFeeAmount, DateTime dueDate);
    }

    public class LeaseBillingService
    {
        private readonly ApplicationDbContext db;
        private readonly ILeaseBillingNotifier notifier;

        private const int ReminderWindowDays = 3; // Days before due date to send reminder
        private const int LateFeeWindowDays = 5; // Days after due date to apply late fee


        public LeaseBillingService(ApplicationDbContext context, ILeaseBillingNotifier notifier)
        {
            db = context;
            this.notifier = notifier;
        }

        // Registered as a Hangfire recurring job
        public async Task RunDailyBillingCheckAsync()
        {
            var activeAgreements = await db.LeaseAgreements
                .Include(a => a.Customer)
                .Include(a => a.Items)
                    .ThenInclude(i => i.LeaseUnit)
                        .ThenInclude(u => u.LeaseProduct)

                .Include(a=>a.Payments)
                .Where(a=>a.Status==LeaseAgreementStatus.Active)
                .ToListAsync();

            foreach (var agreement in activeAgreements)
            {
                var leaseProduct = agreement.Items.FirstOrDefault()?.LeaseUnit?.LeaseProduct;
                if (leaseProduct == null) continue; // shouldn't happen for an Active lease, but don't blow up the whole batch on one bad row


                GenerateUpcomingCycleIfDue(agreement, leaseProduct);
                ApplyLateFeesWhereOverdue(agreement, leaseProduct);

            }


            await db.SaveChangesAsync();
        }


        private void GenerateUpcomingCycleIfDue(LeaseAgreement agreement, LeaseProduct leaseProduct)
        {
            int cycleSoFar = agreement.Payments.Count(p=>p.Type == LeasePaymentType.Recurring);
            int nextCycle = cycleSoFar + 1;
            DateTime nextDueDate = agreement.StartDate.AddMonths(nextCycle);


            if(nextDueDate> agreement.EndDate) return;// lease doesn't run this far (yet — a later renewal will push EndDate out and this will pick back up)
            if (nextDueDate > DateTime.Now.AddDays(ReminderWindowDays)) return;// too early to raise this cycle yet

            bool alreadyExists = agreement. Payments.Any(p=>p.Type == LeasePaymentType.Recurring && p.DueDate == nextDueDate.Date);
            if(alreadyExists) return; // already generated this cycle

            double amountDue = agreement.Items.Sum(i => i.MonthlyRateAtLease);

            var payment = new LeasePayment
            {
                LeaseAgreementId = agreement.LeaseAgreementId,
                DueDate = nextDueDate,
                AmountDue = amountDue,
                AmountPaid = 0,
                Type = LeasePaymentType.Recurring,
                Status = LeasePaymentStatus.Pending,
                ReminderSentOn = DateTime.Now
            };

            db.LeasePayments.Add(payment);
            agreement.Payments.Add(payment); // keep the in-memory collection consistent so late-fee pass below sees it if ever re-run same batch

            notifier.PaymentReminder(agreement.Customer.Email, agreement.Customer.FirstName, amountDue, nextDueDate);
        }

        private void ApplyLateFeesWhereOverdue(LeaseAgreement agreement, LeaseProduct leaseProduct)
        {
            var overdueUnpaid = agreement.Payments.Where(p =>
                p.Type == LeasePaymentType.Recurring &&
                p.Status == LeasePaymentStatus.Pending &&
                !p.LateFeeApplied &&
                p.DueDate <= DateTime.Now.AddDays(-LateFeeWindowDays));

            foreach (var overdue in overdueUnpaid)
            {
                overdue.AmountDue += leaseProduct.LateFeeAmount;
                overdue.LateFeeApplied = true;

                notifier.LateFeeWarning(agreement.Customer.Email, agreement.Customer.FirstName,
                    leaseProduct.LateFeeAmount, overdue.DueDate);
            }
        }
    }
}
