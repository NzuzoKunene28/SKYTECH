using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.Repair_Item_Services;
using SkyTech.Models.Repair_Services;
using SkyTech.Services;
using System.Linq;

namespace SkyTech.Controllers
{
    [AuthorizeUser]
    public class SystemAdminController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly EmailServices _services;
        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly LeaseReturnService leaseReturnService;

        // ✅ ONE constructor only
        public SystemAdminController(ApplicationDbContext context, EmailServices services, IWebHostEnvironment webHostEnvironment, LeaseReturnService leaseReturnService)
        {
            db = context;
            _services = services;
            _webHostEnvironment = webHostEnvironment;
            this.leaseReturnService = leaseReturnService;
        }

        public IActionResult SystemAdminDash() => View();

        // ── SHOP OWNER MANAGEMENT ─────────────────────────────────────

        public IActionResult ViewApplications()
        {
            var applications = db.shopowners
                .Include(s => s.OwnerIdentityBooks)
                .Include(s => s.BussRegCertificates)
                .ToList();
            return View(applications);
        }

        // public IActionResult DownloadBusRegCert(int id)
        // {
        //     var doc = db.BusRegCertificates.Find(id);
        //     return doc == null ? NotFound() : File(doc.FileData, doc.FileType);
        // }

        // public IActionResult DownloadOwnerID(int id)
        // {
        //     var doc = db.OwnerIdentityBooks.Find(id);
        //     return doc == null ? NotFound() : File(doc.FileData, doc.FileType);
        // }

        public IActionResult DownloadBusRegCert(int id)
        {
            var doc = db.BusRegCertificates.Find(id);
            if (doc == null) return NotFound();

            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, doc.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            var fileBytes = System.IO.File.ReadAllBytes(fullPath);
            return File(fileBytes, doc.FileType, doc.FileName);
        }

        public IActionResult DownloadOwnerID(int id)
        {
            var doc = db.OwnerIdentityBooks.Find(id);
            if (doc == null) return NotFound();

            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, doc.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            var fileBytes = System.IO.File.ReadAllBytes(fullPath);
            return File(fileBytes, doc.FileType, doc.FileName);
        }

        public IActionResult ApproveAppl(int id)
        {
            var app = db.shopowners.Find(id);
            if (app != null)
            {
                app.ApplicationStatus = ApplicationStat.Approved;
                db.SaveChanges();
                _services.SOwnerApplApproved(app.Email, app.Names);
            }
            return RedirectToAction("ViewApplications");
        }

        public IActionResult RejectAppl(int id)
        {
            var app = db.shopowners.Find(id);
            if (app != null)
            {
                app.ApplicationStatus = ApplicationStat.Rejected;
                db.SaveChanges();
                _services.SOwnerApplRejected(app.Email, app.Names);
            }
            return RedirectToAction("ViewApplications");
        }

        // ── DRIVER MANAGEMENT ────────────────────────────────────────

        public IActionResult ViewDriverApplication()
        {
            var applications = db.DeliveryDrivers
                .Include(s => s.DriversLicences)
                .Include(s => s.DriverIdentityBooks)
                .ToList();
            return View(applications);
        }

        // public IActionResult DownloadDriverIdentityBooks(int id)
        // {
        //     var doc = db.DriverIdentityBooks.Find(id);
        //     return doc == null ? NotFound() : File(doc.FileData, doc.FileType);
        // }

        // public IActionResult DownloadLicence(int id)
        // {
        //     var doc = db.DriversLicences.Find(id);
        //     return doc == null ? NotFound() : File(doc.FileData, doc.FileType);
        // }

        public IActionResult DownloadDriverIdentityBooks(int id)
        {
            var doc = db.DriverIdentityBooks.Find(id);
            if (doc == null) return NotFound();

            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, doc.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            var fileBytes = System.IO.File.ReadAllBytes(fullPath);
            return File(fileBytes, doc.FileType, doc.FileName);
        }

        public IActionResult DownloadLicence(int id)
        {
            var doc = db.DriversLicences.Find(id);
            if (doc == null) return NotFound();

            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, doc.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            var fileBytes = System.IO.File.ReadAllBytes(fullPath);
            return File(fileBytes, doc.FileType, doc.FileName);
        }

        // ── REPAIR REQUEST MANAGEMENT ──────────────────────────────

        public IActionResult PendingRepairRequests()
        {
            var pendingRequests = db.RepairRequests
                .Where(r => r.Status == RepairStatus.Pending)
                .Include(r => r.Category)
                .Include(r => r.Customers)
                .OrderByDescending(r => r.DateCreated)
                .ToList();
            return View(pendingRequests);
        }

        public IActionResult AllRepairRequests()
        {
            var allRequests = db.RepairRequests
                .Include(r => r.Category)
                .Include(r => r.Customers)
                .OrderByDescending(r => r.DateCreated)
                .ToList();
            return View(allRequests);
        }

        public async Task<IActionResult> AssignTechnician(int? id)
        {
            if (id == null)
                return NotFound();

            var repairRequest = await db.RepairRequests
                .Include(r => r.Category)
                .Include(r => r.Customers)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (repairRequest == null)
                return NotFound();

            // Get available technicians that match the category
            var technicians = await db.Technicians
                .Where(t => t.CategoryId == repairRequest.CategoryId &&
                       t.Status == AvailabilityStatus.Available)
                .ToListAsync();

            ViewData["Technicians"] = technicians;
            return View(repairRequest);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTechnician(int id, [Bind("TechnicianId,AssignmentNotes")] RepairAssignment assignment)
        {
            if (id <= 0)
                return NotFound();

            var repairRequest = await db.RepairRequests.FindAsync(id);
            if (repairRequest == null)
                return NotFound();


            ModelState.Remove("Technician");
            ModelState.Remove("RepairRequest");
            ModelState.Remove("RepairRequestId");
            ModelState.Remove("AssignedDate");


            var errors = ModelState
        .Where(x => x.Value.Errors.Count > 0)
        .Select(x => $"{x.Key}: {string.Join(", ", x.Value.Errors.Select(e => e.ErrorMessage))}")
        .ToList();

            TempData["Debug"] = string.Join(" | ", errors);

            if (ModelState.IsValid)
            {
                try
                {
                    assignment.RepairRequestId = id;
                    assignment.AssignedDate = DateTime.Now;

                    db.RepairAssignments.Add(assignment);

                    repairRequest.Status = RepairStatus.Assigned;

                    var technician = await db.Technicians.FindAsync(assignment.TechnicianId);
                    if (technician != null)
                    {
                        technician.Status = AvailabilityStatus.Busy;
                    }

                    var tracking = new RepairTracking
                    {
                        RepairRequestId = id,
                        Status = RepairStatus.Assigned,
                        UpdatedDate = DateTime.Now,
                        StatusMessage = $"Request assigned to technician {technician?.TechnicianFirstName} {technician?.TechnicianLastName}"
                    };
                    db.RepairTrackings.Add(tracking);

                    await db.SaveChangesAsync();

                    return RedirectToAction(nameof(PendingRepairRequests));
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", $"Database error: {ex.InnerException?.Message ?? ex.Message}");
                    return View();
                }
            }

            var technicians = await db.Technicians
                .Where(t => t.CategoryId == repairRequest.CategoryId)
                .ToListAsync();

            ViewData["Technicians"] = technicians;
            return View(repairRequest);
        }

        public async Task<IActionResult> RepairRequestDetail(int? id)
        {
            if (id == null)
                return NotFound();

            var repairRequest = await db.RepairRequests
                .Include(r => r.Category)
                .Include(r => r.Customers)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (repairRequest == null)
                return NotFound();

            var assignment = await db.RepairAssignments
                .Include(a => a.Technician)
                .FirstOrDefaultAsync(a => a.RepairRequestId == id);

            ViewBag.Assignment = assignment;

            var trackingHistory = await db.RepairTrackings
                .Where(t => t.RepairRequestId == id)
                .OrderByDescending(t => t.UpdatedDate)
                .ToListAsync();

            ViewBag.TrackingHistory = trackingHistory;

            var quotation = await db.Quotations
                .Include(q => q.QuotationItems)
                .FirstOrDefaultAsync(q => q.RepairRequestId == id);

            ViewBag.Quotation = quotation;

            return View(repairRequest);
        }

        public IActionResult ApproveDriverAppl(int id)
        {
            var app = db.DeliveryDrivers.Find(id);
            if (app != null)
            {
                app.ApplicationStatus = Status.Approved;
                db.Entry(app).Property(d => d.ApplicationStatus).IsModified = true;
                db.SaveChanges();
                _services.DriveAppliApproved(app.Email, app.DriverName);
            }
            return RedirectToAction("ViewDriverApplication");
        }

        public IActionResult RejectDriverAppl(int id)
        {
            var app = db.DeliveryDrivers.Find(id);
            if (app != null)
            {
                app.ApplicationStatus = Status.Rejected;
                db.SaveChanges();
                _services.DriveAppliReject(app.Email, app.DriverName);
            }
            return RedirectToAction("ViewDriverApplication");
        }

        //LEASING MANAGEMENT-------------------------------

        // ── EQUIPMENT LEASE REVIEW/ MANAGEMENT ────────────────────────────────────────

        public IActionResult PendingLeaseAgreements()
        {
            var pending = db.LeaseAgreements
                .Where(a => a.Status == LeaseAgreementStatus.PendingApproval)
                .Include(a => a.Customer)
                .Include(a => a.ShopOwner)
                .Include(a => a.Items)
                .ThenInclude(i => i.LeaseUnit)
                .ThenInclude(u => u.LeaseProduct)
                .OrderBy(a => a.StartDate)
                .ToList();
            return View(pending);
        }

        public IActionResult LeaseAgreementDetails(int id)
        {
            var agreement = db.LeaseAgreements
                .Include(a => a.Customer)
                .Include(a => a.ShopOwner)
                .Include(a => a.Items)
                .ThenInclude(i => i.LeaseUnit)
                .ThenInclude(u => u.LeaseProduct)
                .FirstOrDefault(a => a.LeaseAgreementId == id);

            if (agreement == null) return NotFound();

            return View(agreement);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveLeaseAgreement(int id)
        {
            var agreement = db.LeaseAgreements
                .Include(a => a.Customer)
                .FirstOrDefault(a => a.LeaseAgreementId == id);

            if (agreement == null) return NotFound();

            if (agreement.Status != LeaseAgreementStatus.PendingApproval)
            {
                TempData["Error"] = "This request has already been reviewed.";
                return RedirectToAction("PendingLeaseAgreements");
            }

            agreement.Status = LeaseAgreementStatus.PendingPayment;
            db.SaveChanges();

            TempData["Message"] = "Lease request approved. The customer can now sign and pay.";
            return RedirectToAction("PendingLeaseAgreements");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectLeaseAgreement(int id)
        {
            var agreement = db.LeaseAgreements
                .Include(a => a.Items)
                .ThenInclude(i => i.LeaseUnit)
                .Include(a => a.Customer)
                .FirstOrDefault(a => a.LeaseAgreementId == id);

            if (agreement == null) return NotFound();

            if (agreement.Status != LeaseAgreementStatus.PendingApproval)
            {
                TempData["Error"] = "This request has already been reviewed.";
                return RedirectToAction("PendingLeaseAgreements");
            }

            agreement.Status = LeaseAgreementStatus.Rejected;

            foreach (var item in agreement.Items)
            {
                if (item.LeaseUnit != null)
                {
                    item.LeaseUnit.Status = LeaseUnitStatus.Available;
                }
            }

            db.SaveChanges();

            TempData["Message"] = "Lease request rejected. The reserved units are now available again";
            return RedirectToAction("PendingLeaseAgreements");
        }

        public IActionResult AllLeaseAgreements(LeaseAgreementStatus? status)
        {
            var query = db.LeaseAgreements
                .Include(a => a.Customer)
                .Include(a => a.ShopOwner)
                .Include(a => a.Items)
                    .ThenInclude(i => i.LeaseUnit)
                        .ThenInclude(u => u.LeaseProduct)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(a => a.Status == status.Value);

            var agreements = query
                .OrderByDescending(a => a.StartDate)
                .ToList();

            ViewBag.SelectedStatus = status;
            return View(agreements);
        }


        [HttpGet]
        public async Task<IActionResult> ReadyForRefund()
        {
            // Every open agreement with a return underway (at least one item past Not_Due).
            var inReturn = await db.LeaseAgreements
                .Include(a => a.Customer)
                .Include(a => a.Items)
                    .ThenInclude(i => i.LeaseUnit)
                        .ThenInclude(u => u.LeaseProduct)
                .Where(a => a.Status == LeaseAgreementStatus.Active
                         && a.Items.Any(i => i.ReturnStatus != LeaseReturnStatus.Not_Due))
                .ToListAsync();

            // Ready = every single item finished inspection. Anything else is still
            // in progress, and the view lists those separately so an agreement never
            // just silently "doesn't show up".
            var ready = inReturn
                .Where(a => a.Items.All(i => i.ReturnStatus == LeaseReturnStatus.Inspection_Complete))
                .ToList();

            ViewBag.InProgress = inReturn.Except(ready).ToList();

            return View(ready);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessDepositRefund(int leaseAgreementId)
        {
            var (success, error) = await leaseReturnService.ProcessDepositRefundAsync(leaseAgreementId);
            if (!success) TempData["Error"] = error;
            else TempData["Message"] = "Refund processed and lease closed.";

            return RedirectToAction("ReadyForRefund");
        }
    }
}
