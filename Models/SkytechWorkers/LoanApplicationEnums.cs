namespace SkyTech.Models.SkytechWorkers
{
    /// <summary>
    /// Result of the automated eligibility check (UC-03, step 3-4).
    /// </summary>
    public enum EligibilityResult
    {
        Pending = 0,
        Eligible = 1,
        NotEligible = 2
    }

    /// <summary>
    /// Overall lifecycle state of a loan application.
    /// Staff-side transitions (UnderStaffReview -> Approved/Rejected -> Disbursed)
    /// are modeled here so the enum is future-proof, but no controller actions
    /// in this phase move a record into those states — that is explicitly
    /// out of scope per the UC-03 recap (step 8).
    /// </summary>
    public enum LoanApplicationStatus
    {
        Started = 0,
        AwaitingCollateral = 1,
        Submitted = 2,
        UnderStaffReview = 3,
        Approved = 4,
        Rejected = 5,
        Disbursed = 6
    }

    /// Verdict returned by the AI vision scan for a single collateral photo.
    /// NeedsManualReview is distinct from Pending: Pending means "not yet
    /// scanned"; NeedsManualReview means "scanned, but the model's
    /// confidence was below the 60% threshold", so neither Accepted nor
    /// Rejected can be trusted and a human must decide.

    public enum AiScanResult
    {
        Pending = 0,
        Accepted = 1,
        Rejected = 2,
        NeedsManualReview = 3
    }

    /// <summary>
    /// Physical condition the applicant self-declares for a collateral item.
    /// </summary>
    public enum CollateralCondition
    {
        Damaged = 0,
        UsedButGood = 1,
        Excellent = 2
    }

    /// <summary>
    /// Criminal-record clearance status looked up (mocked) by NationalId.
    /// </summary>
    public enum ClearanceStatus
    {
        Clear = 0,
        Flagged = 1,
        UnderReview = 2
    }

    /// <summary>
    /// Condition the customer claims for a collateral item, self-declared
    /// at upload time (UC-03 step 5).
    /// </summary>
    public enum ItemCondition
    {
        Damaged = 0,
        UsedButGood = 1,
        Excellent = 2
    }

    /// <summary>
    /// ASSUMPTION: a simple, optional purpose enum. Not specified in the
    /// design notes beyond "(optional enum)" — flagged for confirmation.
    /// </summary>
    public enum LoanPurpose
    {
        Other = 0,
        HomeImprovement = 1,
        VehiclePurchase = 2,
        Education = 3,
        MedicalExpenses = 4,
        DebtConsolidation = 5,
        BusinessCapital = 6
    }


    //New enums for loan assessment

    
        /// <summary>
        /// Verdict from comparing the staff-captured verification photo against
        /// the customer's originally submitted collateral photo (UC-04 assess step).
        /// </summary>
        public enum CollateralVerificationResult
        {
            Pending = 0,
            Match = 1,
            Mismatch = 2,
            ConditionDegraded = 3,
            NeedsManualReview = 4
        }

        public enum FraudFlagType
        {
            DuplicateCollateral = 0,
            ValueMismatch = 1,
            ConcurrentActiveLoans = 2,
            PriorDefault = 3
        }

        public enum FraudFlagSeverity
        {
            Info = 0,
            Warning = 1,
            Critical = 2
        }

    /// <summary>
    /// Lifecycle status of a Loan's repayment (UC-05), distinct from
    /// LoanApplicationStatus — that enum tracks the application up to
    /// Disbursed; this one picks up from there.
    /// </summary>
    public enum LoanRepaymentStatus
    {
        Active = 0,
        Settled = 1,
        Defaulted = 2
    }

    public enum PaymentMethod
    {
        Cash = 0,
        Card = 1,
        EFT = 2
    }

}