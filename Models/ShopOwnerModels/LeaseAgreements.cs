using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models
{
    public enum LeaseAgreementStatus
    {
        PendingPayment, // 0
        Active,         // 1
        Overdue,        // 2
        Completed,      // 3
        Cancelled,      // 4
        Defaulted,      // 5
        PendingApproval,// 6 After requested
        Rejected        //7 Admin rejects
    }

    public enum LeasePaymentStatus
    {
        Pending, // 0
        Paid,    // 1
        Failed   // 2
    }

    public enum LeasePaymentType
    {
        DepositAndFirstMonth, // 0
        Recurring,             // 1
        RenewalFee,              // 2 Upfront fee for renewing the lease
    }

    // Mirrors your ReturnProduct enum, adapted for lease end-of-term collection
    // instead of a retail refund/exchange flow.
    public enum LeaseReturnStatus
    {
        Not_Due,              // 0 - lease still active, not yet due for return
        Return_Pending,       // 1 - lease ending / customer requested return, awaiting scheduling
        Collection_Assigned,  // 2 - driver assigned to collect
        Item_Collected,       // 3 - picked up, being inspected
        Inspection_Complete,  // 4 - condition assessed, deposit calc finalized
        Completed             // 5 - deposit resolved, unit back in available inventory
    }

    public enum ReturnConditionAssessment
    {
        Good,       // 0 - no damage, fully refundable
        MinorWear,  // 1 - small deduction, still goes back to Available
        Moderate,   // 2 - partial deduction, still usable
        Severe      // 3 - large deduction, unit goes to Maintenance instead of Available
    }


    public class LeaseAgreement
    {
        [Key]
        public int LeaseAgreementId { get; set; }

        public int CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; }

        //Need to have shop owner Id to know which store the leased item(s) belong to
        public int OwnerId { get; set; }
        [ForeignKey("OwnerId")]
        public virtual ShopOwner ShopOwner { get; set; }


        //For signing the agreement, between company and customer for lease
        public bool CustomerAgreed { get; set; } = false;
        public DateTime? AgreedDate { get; set; }



        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TermMonths { get; set; }

        public LeaseAgreementStatus Status { get; set; } = LeaseAgreementStatus.PendingApproval;

        // ── Outbound delivery (mirrors Order — one driver/trip covers the
        // whole agreement, same as one driver per Order) ──────────────────
        [Required] public string DeliveryAddress { get; set; }
        public DeliveryType DeliveryType { get; set; } // reuses Order.cs's enum: HomeDelivery / InStorePickup

        public int? DriverId { get; set; }
        [ForeignKey("DriverId")]
        public virtual DeliveryDriver Driver { get; set; }

        public DateTime ScheduledDeliveryDate { get; set; }

        // Always generated server-side at creation (see RequestLease POST) — required.
        [Required] public string DeliveryPin { get; set; }

        public bool IsDelivered { get; set; } = false;

        //-Supporting documents (Collected at RequestLease time)
        //Always upload and validate
        [Required] public string BankStatementDocPath { get; set; }
        [Required] public string ProofOfResidenceDocPath { get; set; }

        // ── Paystack references ──────────────────────────────────────────
        // All of these are populated progressively AFTER the agreement row
        // is created (subscription setup, first charge, etc.), so they must
        // be nullable — the '?' is what tells EF to allow NULL here.
        public string? PaystackCustomerCode { get; set; }
        public string? PaystackPlanCode { get; set; }
        public string? PaystackSubscriptionCode { get; set; }
        public string? PaystackEmailToken { get; set; }        // required to disable/enable the subscription later
        public string? PaystackAuthorizationCode { get; set; }
        public string? InitialTransactionReference { get; set; } // the deposit + first month charge


        // ── Return / refund payout details ──────────────────────────────
        // Collected from the customer at ScheduleReturn time, since that's
        // the natural moment they're already interacting with the return
        // flow. Nullable because they don't exist until a return is scheduled.
        public string? ReturnBankCode { get; set; }
        public string? ReturnAccountNumber { get; set; }
        public string? ReturnAccountHolderName { get; set; }

        // Populated once CreateRecipient/InitiateTransfer actually run.
        public string? RefundRecipientCode { get; set; }
        public string? RefundTransferCode { get; set; }
        public string? RefundTransferStatus { get; set; }

        // Idempotency guard for LeaseReturnService's daily reminder — without
        // this, the same "renew or return" email fires every day for the
        // whole 14-day window instead of once.
        public DateTime? ReturnReminderSentOn { get; set; }


        public virtual ICollection<LeaseAgreementItem> Items { get; set; } = new List<LeaseAgreementItem>();
        public virtual ICollection<LeasePayment> Payments { get; set; } = new List<LeasePayment>();
    }


    // One row per physical unit on the agreement — quantity > 1 means
    // multiple rows here, each pointing at a distinct serialized LeaseUnit.
    public class LeaseAgreementItem
    {
        [Key]
        public int LeaseAgreementItemId { get; set; }

        public int LeaseAgreementId { get; set; }
        [ForeignKey("LeaseAgreementId")]
        public virtual LeaseAgreement LeaseAgreement { get; set; }

        public int LeaseUnitId { get; set; }
        [ForeignKey("LeaseUnitId")]
        public virtual LeaseUnit LeaseUnit { get; set; }

        // Snapshot at agreement time — protects existing leases if catalog pricing changes later
        public double MonthlyRateAtLease { get; set; }
        public double DepositAtLease { get; set; }

        // ── End-of-term collection (mirrors OrderItem's return/collection
        // sub-flow — per unit, since units on one agreement could be
        // collected on different visits) ───────────────────────────────────
        public LeaseReturnStatus ReturnStatus { get; set; } = LeaseReturnStatus.Not_Due;

        public int? CollectionDriverId { get; set; }
        [ForeignKey("CollectionDriverId")]
        public virtual DeliveryDriver CollectionDriver { get; set; }

        public DateTime? ScheduledCollectionDate { get; set; }
        public DateTime? ActualCollectionDate { get; set; }

        // Not known until the unit is actually returned and inspected — nullable.
        public string? ConditionAtReturn { get; set; }
        public double? DamageFee { get; set; }
        public double? DepositRefunded { get; set; }


        public double? EarlyReturnFee { get; set; }

        // ── AI-assisted damage assessment ───────────────────────────────
        // Same pattern as CollateralVerification: AI suggests, staff confirms
        // or overrides. Nullable until an inspection photo is actually
        // uploaded and analyzed.
        public string? ReturnConditionImagePath { get; set; }
        public ReturnConditionAssessment? AiDamageAssessment { get; set; }
        public int? AiConfidencePercent { get; set; }
        public double? AiSuggestedDamageFee { get; set; }
        public string? AiNotes { get; set; }
        public bool StaffOverride { get; set; } = false;
        public string? StaffOverrideReason { get; set; }
    }

    public class LeasePayment
    {
        [Key]
        public int LeasePaymentId { get; set; }

        public int LeaseAgreementId { get; set; }
        [ForeignKey("LeaseAgreementId")]
        public virtual LeaseAgreement LeaseAgreement { get; set; }

        public int? RenewalTermMonths { get; set; }

        public DateTime DueDate { get; set; }
        public double AmountDue { get; set; }
        public double AmountPaid { get; set; }
        public DateTime? PaidDate { get; set; }

        public LeasePaymentType Type { get; set; }
        public LeasePaymentStatus Status { get; set; } = LeasePaymentStatus.Pending;

        // Not known until the payment actually processes — nullable, same reasoning as above.
        public string? PaystackTransactionReference { get; set; }


        // ← add these two
        public DateTime? ReminderSentOn { get; set; }
        public bool LateFeeApplied { get; set; } = false;
    }
}