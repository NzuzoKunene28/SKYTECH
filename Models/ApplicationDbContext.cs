using Microsoft.EntityFrameworkCore; using SkyTech.Models;
using SkyTech.Models.AttendanceMods;
using SkyTech.Models.Documents;
using SkyTech.Models.Interviews;
using SkyTech.Models.JobPostServices;
using SkyTech.Models.Repair_Item_Services;
using SkyTech.Models.Repair_Services;
using SkyTech.Models.SkytechWorkers;
using SkyTech.Services;


namespace SkyTech.Models
{
    public class ApplicationDbContext : DbContext
    {
        // EF Core: receives DbContextOptions via constructor injection
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ── User tables ──────────────────────────────────────
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<ShopOwner> shopowners { get; set; }
        public DbSet<SystemAdmin> SystemAdmins { get; set; }
        public DbSet<DeliveryDriver> DeliveryDrivers { get; set; }

        // ── Document tables ──────────────────────────────────
        public DbSet<BusRegCertificate> BusRegCertificates { get; set; }
        public DbSet<DriverIdentityBook> DriverIdentityBooks { get; set; }
        public DbSet<OwnerIdentityBook> OwnerIdentityBooks { get; set; }
        public DbSet<DriversLicence> DriversLicences { get; set; }

        // ── Product / order tables ───────────────────────────
        public DbSet<AddProduct> AddProducts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        // ── Cart tables ──────────────────────────────────────
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        //public DbSet<Return> Return { get; set; }
        //public DbSet<ReturnItem> ReturnItem { get; set; }


        //New tables for pickup
        public DbSet<PickupOrder> PickupOrders { get; set; }
        public DbSet<InterBranchTransfer> InterBranchTransfers { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DbSet<ProposeItem> ProposeItems { get; set; }

        public DbSet<ReceiptPic> ReceiptPics { get; set; }
        public DbSet<ProposedProdPic> ProposedProdPic { get; set; }
        public DbSet<ProposalCheckout> ProposalCheckouts { get; set; }


        // ── Repair Items Services tables ──────────────────────────────────────

        public DbSet<Repair_Item_Category> RepairItemCategories { get; set; }
        public DbSet<Repair_Request> RepairRequests { get; set; }
        public DbSet<Technician> Technicians { get; set; }
        public DbSet<RepairAssignment> RepairAssignments { get; set; }
        public DbSet<Quotation> Quotations { get; set; }
        public DbSet<QuotationItem> QuotationItems { get; set; }
        public DbSet<Receipt> Receipts { get; set; }
        public DbSet<RepairTracking> RepairTrackings { get; set; }

        //--------POST TABLE----
        public DbSet<JobPost> JobPosts { get; set; }
        public DbSet<JobRoleKnowledge> JobRoleKnowledges { get; set; }
        public DbSet<ApplicationForm> ApplicationForms { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<StaffPosition> StaffPositions { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<HR> HRs { get; set; }


        // ── System / Reference Data ─────────────────────────
        public DbSet<PublicHoliday> PublicHolidays { get; set; }

        //--------Request Leave Tables------
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<StaffLeaveBalance> StaffLeaveBalances { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        //========ROSTER TABLES=====
        public DbSet<Roster> Rosters { get; set; }
        public DbSet<RosterShift> RosterShifts { get; set; }
        public DbSet<StaffRotationOrder> StaffRotationOrders { get; set; }
        public DbSet<ShiftChangeRequest> ShiftChangeRequests { get; set; }

        // ── Attendance (check-in/out) — 2/1 ───────────────────────────────
        // Keys off the real RosterShift/Staff tables above — no separate
        // Shift/WorkplaceLocation tables needed now that those exist.
        public DbSet<Attendance> Attendances { get; set; }


        //=========TEST TABLES=======
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        public DbSet<Assessment> Assessments { get; set; }

        //===Contract and interviews
        public DbSet<Interview> Interviews { get; set; }
        public DbSet<Contract> Contracts { get; set; }

        //Loan Application Models
        public DbSet<LoanApplication> LoanApplications { get; set; }
        public DbSet<CollateralItem> CollateralItems { get; set; }
        public DbSet<CreditRecord> CreditRecords { get; set; }
        public DbSet<CriminalRecord> CriminalRecords { get; set; }

        //loan assessment models
        public DbSet<LoanAssessment> LoanAssessments { get; set; }
        public DbSet<CollateralVerification> CollateralVerifications { get; set; }
        public DbSet<FraudFlag> FraudFlags { get; set; }
        public DbSet<LoanDisbursement> LoanDisbursements { get; set; }

        // loan settle
        public DbSet<Loan> Loans { get; set; }
        public DbSet<LoanPayment> LoanPayments { get; set; }

        // ── Lease tables ──────────────────────────────────────
        public DbSet<LeaseProduct> LeaseProducts { get; set; }
        public DbSet<LeaseUnit> LeaseUnits { get; set; }
        public DbSet<LeaseProductPicture> LeaseProductPictures { get; set; }
        public DbSet<LeaseAgreement> LeaseAgreements { get; set; }
        public DbSet<LeaseAgreementItem> LeaseAgreementItems { get; set; }
        public DbSet<LeasePayment> LeasePayments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Order → OrderItems (cascade is fine)
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Customer → Orders (no cascade)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customers)
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);  // ← stops the cycle

            // ========== FIXED: PickupOrder configuration (NO CASCADE) ==========
            modelBuilder.Entity<PickupOrder>()
                .HasOne(p => p.Order)
                .WithOne()
                .HasForeignKey<PickupOrder>(p => p.OrderId)
                .OnDelete(DeleteBehavior.NoAction);  // ← FIXED: NoAction instead of Cascade

            // ========== FIXED: InterBranchTransfer configuration ==========
            modelBuilder.Entity<InterBranchTransfer>()
                .HasOne(t => t.OrderItem)
                .WithMany()
                .HasForeignKey(t => t.OrderItemId)
                .OnDelete(DeleteBehavior.NoAction);  // ← FIXED: NoAction

            modelBuilder.Entity<InterBranchTransfer>()
                .HasOne(t => t.SourceBranch)
                .WithMany()
                .HasForeignKey(t => t.SourceBranchId)
                .OnDelete(DeleteBehavior.NoAction);  // ← FIXED: NoAction

            modelBuilder.Entity<InterBranchTransfer>()
                .HasOne(t => t.TargetBranch)
                .WithMany()
                .HasForeignKey(t => t.TargetBranchId)
                .OnDelete(DeleteBehavior.NoAction);  // ← FIXED: NoAction

            modelBuilder.Entity<InterBranchTransfer>()
                .HasOne(t => t.Driver)
                .WithMany()
                .HasForeignKey(t => t.DriverId)
                // ← FIXED: NoAction
                .OnDelete(DeleteBehavior.NoAction);

            // ShopOwner → Orders (no cascade)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.ShopOwner)
                .WithMany()
                .HasForeignKey(o => o.OwnerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<OrderItem>()
            .HasOne(i => i.DeliveryDriver)
            .WithMany()
            .HasForeignKey(i => i.DriverId)
            .OnDelete(DeleteBehavior.NoAction);

            // Fix cascade conflict on ProposeItems → Customer
            modelBuilder.Entity<ProposeItem>()
                .HasOne(p => p.Customer)
                .WithMany(c => c.ProposeItems)
                .HasForeignKey(p => p.CustomerId)
                .OnDelete(DeleteBehavior.NoAction); // ← fixes the error

            // Fix cascade conflict on ProposalCheckout → ProposeItem
            modelBuilder.Entity<ProposalCheckout>()
                .HasOne(c => c.ProposeItem)
                .WithOne(p => p.Checkout)
                .HasForeignKey<ProposalCheckout>(c => c.PropItemId)
                .OnDelete(DeleteBehavior.NoAction);

            // Fix cascade conflict on ProposalCheckout → Customer
            modelBuilder.Entity<ProposalCheckout>()
                .HasOne(c => c.Customer)
                .WithMany()
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RosterShift>()
                .HasOne(rs => rs.Staff).WithMany()
                .HasForeignKey(rs => rs.StaffId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RosterShift>()
                .HasOne(rs => rs.Position).WithMany()
                .HasForeignKey(rs => rs.PositionId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RosterShift>()
                .HasOne(rs => rs.Branch).WithMany()
                .HasForeignKey(rs => rs.BranchId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Roster>()
                .HasOne(r => r.Branch).WithMany()
                .HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StaffRotationOrder>()
                .HasOne(o => o.Staff).WithMany()
                .HasForeignKey(o => o.StaffId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StaffRotationOrder>()
                .HasOne(o => o.Branch).WithMany()
                .HasForeignKey(o => o.BranchId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StaffRotationOrder>()
                .HasOne(o => o.Position).WithMany()
                .HasForeignKey(o => o.PositionId).OnDelete(DeleteBehavior.Restrict);
            // ========== PUBLIC HOLIDAY CONFIGURATION ==========
            modelBuilder.Entity<PublicHoliday>()
                .HasIndex(h => h.HolidayDate)
                .IsUnique();


            // ============================================================
            // LEASE CONFIGURATION
            // ============================================================

            // LeaseUnit -> LeaseAgreementItems
            // Historical lease records must not be deleted when a unit is deleted.
            modelBuilder.Entity<LeaseAgreementItem>()
                .HasOne(i => i.LeaseUnit)
                .WithMany()
                .HasForeignKey(i => i.LeaseUnitId)
                .OnDelete(DeleteBehavior.NoAction);

            // LeaseAgreement -> LeaseAgreementItems
            modelBuilder.Entity<LeaseAgreementItem>()
                .HasOne(i => i.LeaseAgreement)
                .WithMany(a => a.Items)
                .HasForeignKey(i => i.LeaseAgreementId)
                .OnDelete(DeleteBehavior.Cascade);


            // LeaseAgreement -> LeasePayments
            modelBuilder.Entity<LeasePayment>()
                .HasOne(p => p.LeaseAgreement)
                .WithMany(a => a.Payments)
                .HasForeignKey(p => p.LeaseAgreementId)
                .OnDelete(DeleteBehavior.Cascade);

            // LeaseProduct -> LeaseUnits
            modelBuilder.Entity<LeaseUnit>()
                .HasOne(u => u.LeaseProduct)
                .WithMany(p => p.LeaseUnits)
                .HasForeignKey(u => u.LeaseProdId)
                .OnDelete(DeleteBehavior.NoAction);

            // LeaseProduct -> LeaseProductPictures
            modelBuilder.Entity<LeaseProductPicture>()
                .HasOne(p => p.LeaseProduct)
                .WithMany(lp => lp.LeaseProductPictures)
                .HasForeignKey(p => p.LeaseProdId)
                .OnDelete(DeleteBehavior.Cascade);

            // LeaseAgreement -> Customer
            modelBuilder.Entity<LeaseAgreement>()
                .HasOne(a => a.Customer)
                .WithMany()
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            // LeaseAgreement -> ShopOwner
            modelBuilder.Entity<LeaseAgreement>()
                .HasOne(a => a.ShopOwner)
                .WithMany()
                .HasForeignKey(a => a.OwnerId)
                .OnDelete(DeleteBehavior.NoAction);

            // LeaseAgreement -> DeliveryDriver
            modelBuilder.Entity<LeaseAgreement>()
                .HasOne(a => a.Driver)
                .WithMany()
                .HasForeignKey(a => a.DriverId)
                .OnDelete(DeleteBehavior.NoAction);

            // LeaseAgreementItem -> CollectionDriver
            modelBuilder.Entity<LeaseAgreementItem>()
                .HasOne(i => i.CollectionDriver)
                .WithMany()
                .HasForeignKey(i => i.CollectionDriverId)
                .OnDelete(DeleteBehavior.NoAction);


            //========== Request Leave Configuration ===========
            // A staff member can only have one balance per leave type per leave cycle
            modelBuilder.Entity<StaffLeaveBalance>()
                    .HasIndex(x => new
                    {
                        x.StaffId,
                        x.LeaveTypeId,
                        x.CycleStartDate
                    })
                    .IsUnique();

            // StaffLeaveBalance -> Staff
            modelBuilder.Entity<StaffLeaveBalance>()
                .HasOne(lb => lb.Staff)
                .WithMany()
                .HasForeignKey(lb => lb.StaffId)
                .OnDelete(DeleteBehavior.Restrict);

            // StaffLeaveBalance -> LeaveType
            modelBuilder.Entity<StaffLeaveBalance>()
                .HasOne(lb => lb.LeaveType)
                .WithMany()
                .HasForeignKey(lb => lb.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // LeaveRequest -> Staff
            modelBuilder.Entity<LeaveRequest>()
                .HasOne(lr => lr.Staff)
                .WithMany()
                .HasForeignKey(lr => lr.StaffId)
                .OnDelete(DeleteBehavior.Restrict);

            // LeaveRequest -> LeaveType
            modelBuilder.Entity<LeaveRequest>()
                .HasOne(lr => lr.LeaveType)
                .WithMany()
                .HasForeignKey(lr => lr.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveType>().HasData(
    new LeaveType
    {
        LeaveTypeId = 1,
        LeaveTypeName = "Annual Leave",
        RequiresDocument = false,
        MinimumNoticeDays = 7,
        MaximumConsecutiveDays = 10,
        IsPaid = true,
        RequiresBalance = true,
        CycleMonths = 12,
        MinimumEmploymentMonths = 0,
        IsActive = true
    },
    new LeaveType
    {
        LeaveTypeId = 2,
        LeaveTypeName = "Sick Leave",
        RequiresDocument = true,
        MinimumNoticeDays = 0,
        MaximumConsecutiveDays = 30,
        IsPaid = true,
        RequiresBalance = true,
        CycleMonths = 36,
        MinimumEmploymentMonths = 0,
        IsActive = true
    },
    new LeaveType
    {
        LeaveTypeId = 3,
        LeaveTypeName = "Family Responsibility Leave",
        RequiresDocument = false,
        MinimumNoticeDays = 0,
        MaximumConsecutiveDays = 3,
        IsPaid = true,
        RequiresBalance = true,
        CycleMonths = 12,
        MinimumEmploymentMonths = 4,
        IsActive = true
    },
    new LeaveType
    {
        LeaveTypeId = 4,
        LeaveTypeName = "Unpaid Leave",
        RequiresDocument = false,
        MinimumNoticeDays = 14,
        MaximumConsecutiveDays = 10,
        IsPaid = false,
        RequiresBalance = false,
        CycleMonths = null,
        MinimumEmploymentMonths = 0,
        IsActive = true
    }
);
            //============== Public Holiday Seed Data ========
            modelBuilder.Entity<PublicHoliday>().HasData(
                new PublicHoliday
                {
                    PublicHolidayId = 1,
                    HolidayName = "New Year's Day",
                    HolidayDate = new DateTime(2026, 1, 1),
                    IsActive = true
                },
                new PublicHoliday
                {
                    PublicHolidayId = 2,
                    HolidayName = "Human Rights Day",
                    HolidayDate = new DateTime(2026, 3, 21),
                    IsActive = true
                },
                new PublicHoliday
                {
                    PublicHolidayId = 3,
                    HolidayName = "Good Friday",
                    HolidayDate = new DateTime(2026, 4, 3),
                    IsActive = true
                },
                new PublicHoliday
                {
                    PublicHolidayId = 4,
                    HolidayName = "Family Day",
                    HolidayDate = new DateTime(2026, 4, 6),
                    IsActive = true
                },
                new PublicHoliday
                {
                    PublicHolidayId = 5,
                    HolidayName = "Freedom Day",
                    HolidayDate = new DateTime(2026, 4, 27),
                    IsActive = true
                },
                new PublicHoliday
                {
                    PublicHolidayId = 6,
                    HolidayName = "Workers' Day",
                    HolidayDate = new DateTime(2026, 5, 1),
                    IsActive = true
                },
                new PublicHoliday
                {
                    PublicHolidayId = 7,
                    HolidayName = "Youth Day",
                    HolidayDate = new DateTime(2026, 6, 16),
                    IsActive = true
                },
                new PublicHoliday
                {
                    PublicHolidayId = 8,
                    HolidayName = "National Women's Day",
                    HolidayDate = new DateTime(2026, 8, 9),
                    IsActive = true
                },
                new PublicHoliday
                {
                    PublicHolidayId = 9,
                    HolidayName = "National Women's Day Observed",
                    HolidayDate = new DateTime(2026, 8, 10),
                    IsActive = true
                },
                new PublicHoliday
                {
                    PublicHolidayId = 10,
                    HolidayName = "Heritage Day",
                    HolidayDate = new DateTime(2026, 9, 24),
                    IsActive = true
                },
                new PublicHoliday
                {
                    PublicHolidayId = 11,
                    HolidayName = "Day of Reconciliation",
                    HolidayDate = new DateTime(2026, 12, 16),
                    IsActive = true
                },
                new PublicHoliday
                {
                    PublicHolidayId = 12,
                    HolidayName = "Christmas Day",
                    HolidayDate = new DateTime(2026, 12, 25),
                    IsActive = true
                },
                new PublicHoliday
                {
                    PublicHolidayId = 13,
                    HolidayName = "Day of Goodwill",
                    HolidayDate = new DateTime(2026, 12, 26),
                    IsActive = true
                }
            );
            modelBuilder.Entity<RosterShift>()
                .HasOne(rs => rs.Staff).WithMany()
                .HasForeignKey(rs => rs.StaffId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RosterShift>()
                .HasOne(rs => rs.Position).WithMany()
                .HasForeignKey(rs => rs.PositionId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RosterShift>()
                .HasOne(rs => rs.Branch).WithMany()
                .HasForeignKey(rs => rs.BranchId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Roster>()
                .HasOne(r => r.Branch).WithMany()
                .HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StaffRotationOrder>()
                .HasOne(o => o.Staff).WithMany()
                .HasForeignKey(o => o.StaffId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StaffRotationOrder>()
                .HasOne(o => o.Branch).WithMany()
                .HasForeignKey(o => o.BranchId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StaffRotationOrder>()
                .HasOne(o => o.Position).WithMany()
                .HasForeignKey(o => o.PositionId).OnDelete(DeleteBehavior.Restrict);


            // ── Attendance (check-in/out) — 2/1 ─────────────────────────────
            // Deliberately unidirectional: RosterShift doesn't need to know
            // about Attendance, so RosterShift.cs (owned by the roster team)
            // doesn't need any changes for this to work.
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.RosterShift)
                .WithMany()
                .HasForeignKey(a => a.RosterShiftId)
                .OnDelete(DeleteBehavior.Restrict);

            // Enforces one attendance record per roster shift.
            modelBuilder.Entity<Attendance>()
                .HasIndex(a => a.RosterShiftId)
                .IsUnique();

            //// JobPost -> ApplicationForm
            //modelBuilder.Entity<ApplicationForm>()
            //    .HasOne(a => a.JobPosts)
            //    .WithMany(j => j.ApplicationForms)
            //    .HasForeignKey(a => a.PostId)
            //    .OnDelete(DeleteBehavior.NoAction);


            //// Customer -> ApplicationForm
            //modelBuilder.Entity<ApplicationForm>()
            //    .HasOne(a => a.Customers)
            //    .WithMany()
            //    .HasForeignKey(a => a.CustomerId)
            //    .OnDelete(DeleteBehavior.NoAction);


            //// Assessment -> ApplicationForm
            //modelBuilder.Entity<ApplicationForm>()
            //    .HasOne(a => a.Assessments)
            //    .WithMany()
            //    .HasForeignKey(a => a.AssessmentId)
            //    .OnDelete(DeleteBehavior.NoAction);

            //modelBuilder.Entity<ApplicationForm>()
            //    .HasOne(a => a.JobPosts).
            //    WithMany(j => j.ApplicationForms).
            //    HasForeignKey(a => a.PostId).
            //    OnDelete(DeleteBehavior.NoAction); 

            //// Assessment -> ApplicationForm
            //modelBuilder.Entity<ApplicationForm>() 
            //    .HasOne(a => a.Assessments) .WithMany() 
            //    .HasForeignKey(a => a.AssessmentId) 
            //    .OnDelete(DeleteBehavior.NoAction); 

            //// Customer -> ApplicationForm
            //modelBuilder.Entity<ApplicationForm>() 
            //.HasOne(a => a.Customers) .WithMany() 
            //.HasForeignKey(a => a.CustomerId) 
            //.OnDelete(DeleteBehavior.NoAction);
            //    modelBuilder.Entity<ApplicationForm>()
            //.HasOne(af => af.Assessments)
            //.WithMany(a => a.ApplicationForms)
            //.HasForeignKey(af => af.AssessmentId)
            //.OnDelete(DeleteBehavior.Restrict); // or .NoAction
            // JobPost -> ApplicationForm (keep cascade)
            modelBuilder.Entity<ApplicationForm>()
                .HasOne(af => af.JobPosts)
                .WithMany(jp => jp.ApplicationForms)
                .HasForeignKey(af => af.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // Assessment -> ApplicationForm (BREAK the cascade here — this is the second path)
            modelBuilder.Entity<ApplicationForm>()
                .HasOne(af => af.Assessments)
                .WithMany(a => a.ApplicationForms)
                .HasForeignKey(af => af.AssessmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // JobPost -> Assessment (keep cascade, or set as you prefer)
            modelBuilder.Entity<Assessment>()
                .HasOne(a => a.JobPosts)
                .WithMany(jp => jp.Assessments)
                .HasForeignKey(a => a.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // Customer -> ApplicationForm
            modelBuilder.Entity<ApplicationForm>()
                .HasOne(af => af.Customers)
                .WithMany() // add a collection nav on Customer if you have one, e.g. .WithMany(c => c.ApplicationForms)
                .HasForeignKey(af => af.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Contract>()
       .HasOne(c => c.ApplicationForm)
       .WithMany()
       .HasForeignKey(c => c.ApplicationId)
       .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Contract>()
                .HasOne(c => c.JobPost)
                .WithMany()
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ShopOwner>()
    .HasOne(o => o.Branch).WithMany()
    .HasForeignKey(o => o.BranchId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<JobRoleKnowledge>()
     .HasOne(j => j.StaffPosition)
     .WithMany()
     .HasForeignKey(j => j.PositionId)
     .OnDelete(DeleteBehavior.NoAction);

            //        modelBuilder.Entity<LoanApplication>()
            //.HasOne(la => la.Customer)
            //.WithMany()
            //.HasForeignKey(la => la.CustomerId)
            //.OnDelete(DeleteBehavior.Restrict);


            //New ones added

            modelBuilder.Entity<LoanAssessment>()
    .HasOne(a => a.LoanApplication)
    .WithMany() // add ICollection<LoanAssessment> on LoanApplication if you want it navigable both ways
    .HasForeignKey(a => a.LoanApplicationId)
    .OnDelete(DeleteBehavior.Restrict);

            //modelBuilder.Entity<LoanAssessment>()
            //    .HasOne(a => a.ShopOwner)
            //    .WithMany()
            //    .HasForeignKey(a => a.ShopOwnerId)
            //    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CollateralVerification>()
                .HasOne(v => v.LoanAssessment)
                .WithMany(a => a.CollateralVerifications)
                .HasForeignKey(v => v.LoanAssessmentId)
                .OnDelete(DeleteBehavior.Cascade); // deleting an assessment removes its verification rows

            modelBuilder.Entity<CollateralVerification>()
                .HasOne(v => v.CollateralItem)
                .WithMany()
                .HasForeignKey(v => v.CollateralItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FraudFlag>()
                .HasOne(f => f.LoanAssessment)
                .WithMany(a => a.FraudFlags)
                .HasForeignKey(f => f.LoanAssessmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LoanAssessment>()
            .HasOne(a => a.ShopOwner)
            .WithMany()
            .HasForeignKey(a => a.OwnerId)   // ← must match the property name above
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LoanDisbursement>()
            .HasOne(d => d.LoanApplication)
            .WithMany()
            .HasForeignKey(d => d.LoanApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LoanDisbursement>()
                .HasOne(d => d.ShopOwner)
                .WithMany()
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        }


    }
}