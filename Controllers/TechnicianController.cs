using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.Repair_Item_Services;
using SkyTech.Models.Repair_Services;
using SkyTech.Services;

namespace SkyTech.Controllers
{
    public class TechnicianController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailServices _emailServices;

        public TechnicianController(ApplicationDbContext context, EmailServices emailServices)
        {
            _context = context;
            _emailServices = emailServices;
        }

        // Get current technician ID from session
        private int GetTechnicianId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        // Check if user is technician
        private bool IsTechnician()
        {
            var userType = HttpContext.Session.GetString("UserType");
            return userType == "Technician";
        }

        // ── DASHBOARD ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (!IsTechnician())
                return RedirectToAction("LogIn", "Authentication");

            var technicianId = GetTechnicianId();
            var technician = await _context.Technicians
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.TechnicianId == technicianId);

            if (technician == null)
                return RedirectToAction("LogIn", "Authentication");

            // Get all assignments for this technician with related data
            var allAssignments = await _context.RepairAssignments
                .Where(ra => ra.TechnicianId == technicianId)
                .Include(ra => ra.RepairRequest)
                .ThenInclude(r => r.Customers)
                .Include(ra => ra.RepairRequest.Category)
                .ToListAsync();

            // Count by status
            var assignedCount = allAssignments.Count(ra => ra.RepairRequest.Status == RepairStatus.Assigned);
            var inProgressCount = allAssignments.Count(ra => ra.RepairRequest.Status == RepairStatus.InProgress);
            var completedCount = allAssignments.Count(ra => ra.RepairRequest.Status == RepairStatus.Completed);

            var quotationPendingCount = await _context.Quotations
                .Where(q => q.TechnicianId == technicianId && q.Status == QuotationStatus.Pending)
                .CountAsync();

            // Get recent assignments
            var recentAssignments = allAssignments
                .OrderByDescending(ra => ra.AssignedDate)
                .Take(5)
                .ToList();

            ViewBag.AssignedCount = assignedCount;
            ViewBag.InProgressCount = inProgressCount;
            ViewBag.QuotationPendingCount = quotationPendingCount;
            ViewBag.CompletedCount = completedCount;
            ViewBag.RecentAssignments = recentAssignments;
            ViewBag.TechnicianName = $"{technician.TechnicianFirstName} {technician.TechnicianLastName}";
            ViewBag.Specialization = technician.Category?.CategoryName ?? "General";

            return View();
        }

        // ── MY ASSIGNMENTS ──────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> MyAssignments()
        {
            if (!IsTechnician())
                return RedirectToAction("Dashboard");

            var technicianId = GetTechnicianId();

            var repairs = await _context.RepairAssignments
                .Where(ra => ra.TechnicianId == technicianId)
                .Include(ra => ra.RepairRequest)
                .ThenInclude(r => r.Customers)
                .Include(ra => ra.RepairRequest.Category)
                .OrderByDescending(ra => ra.AssignedDate)
                .ToListAsync();

            return View(repairs);
        }

        // ── ACTIVE REPAIRS ──────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> ActiveRepairs()
        {
            if (!IsTechnician())
                return RedirectToAction("Dashboard");

            var technicianId = GetTechnicianId();

            var repairs = await _context.RepairAssignments
                .Where(ra => ra.TechnicianId == technicianId && ra.RepairRequest.Status == RepairStatus.InProgress)
                .Include(ra => ra.RepairRequest)
                .ThenInclude(r => r.Customers)
                .Include(ra => ra.RepairRequest.Category)
                .OrderByDescending(ra => ra.AssignedDate)
                .ToListAsync();

            return View(repairs);
        }

        // ── REPAIR DETAIL ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> RepairDetail(int? id)
        {
            if (!IsTechnician() || id == null)
                return RedirectToAction("Dashboard");

            var technicianId = GetTechnicianId();

            var assignment = await _context.RepairAssignments
                .Where(ra => ra.TechnicianId == technicianId && ra.RepairRequestId == id)
                .Include(ra => ra.RepairRequest)
                .ThenInclude(r => r.Customers)
                .Include(ra => ra.RepairRequest.Category)
                .Include(ra => ra.Technician)
                .FirstOrDefaultAsync();

            if (assignment == null)
                return NotFound();

            // Get quotations for this repair
            var quotations = await _context.Quotations
                .Where(q => q.RepairRequestId == id)
                .Include(q => q.QuotationItems)
                .ToListAsync();

            ViewBag.Quotations = quotations;

            return View(assignment);
        }

        // ── CREATE QUOTATION ──────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> CreateQuotation(int? id)
        {
            if (!IsTechnician() || id == null)
                return RedirectToAction("Dashboard");

            var technicianId = GetTechnicianId();

            var assignment = await _context.RepairAssignments
                .Where(ra => ra.TechnicianId == technicianId && ra.RepairRequestId == id)
                .Include(ra => ra.RepairRequest)
                .ThenInclude(r => r.Customers)
                .FirstOrDefaultAsync();

            if (assignment == null)
                return NotFound();

            // Check if quotation already exists
            var existingQuotation = await _context.Quotations
                .Where(q => q.RepairRequestId == id && q.TechnicianId == technicianId)
                .FirstOrDefaultAsync();

            if (existingQuotation != null)
                return RedirectToAction("EditQuotation", new { id = existingQuotation.QuotationId });

            // Set ViewBag properties to display repair details in view
            ViewBag.RepairRequestId = assignment.RepairRequest.RequestId;
            ViewBag.ItemName = assignment.RepairRequest.ItemName;
            ViewBag.IssueDescription = assignment.RepairRequest.IssueDescription;
            ViewBag.CustomerName = $"{assignment.RepairRequest.Customers?.FirstName} {assignment.RepairRequest.Customers?.LastName}";

            var quotationVM = new CreateQuotationViewModel
            {
                RepairRequestId = assignment.RepairRequest.RequestId,
                ItemName = assignment.RepairRequest.ItemName,
                IssueDescription = assignment.RepairRequest.IssueDescription,
                CustomerName = $"{assignment.RepairRequest.Customers?.FirstName} {assignment.RepairRequest.Customers?.LastName}"
            };

            return View(quotationVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuotation(CreateQuotationViewModel model)
        {
            if (!IsTechnician())
                return RedirectToAction("Dashboard");

            // Debug: Log ModelState errors
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(x => x.Errors);
                foreach (var error in errors)
                {
                    System.Diagnostics.Debug.WriteLine($"ModelState Error: {error.ErrorMessage}");
                }

                // Set ViewBag properties for displaying repair details on validation error
                ViewBag.RepairRequestId = model.RepairRequestId;
                ViewBag.ItemName = model.ItemName;
                ViewBag.IssueDescription = model.IssueDescription;
                ViewBag.CustomerName = model.CustomerName;
                return View(model);
            }

            var technicianId = GetTechnicianId();

            var assignment = await _context.RepairAssignments
                .FirstOrDefaultAsync(ra => ra.TechnicianId == technicianId && 
                    ra.RepairRequestId == model.RepairRequestId);

            if (assignment == null)
                return NotFound();

            try
            {
                // Validate TotalCost
                if (model.TotalCost <= 0)
                {
                    ModelState.AddModelError("TotalCost", "Total cost must be greater than 0");
                    ViewBag.RepairRequestId = model.RepairRequestId;
                    ViewBag.ItemName = model.ItemName;
                    ViewBag.IssueDescription = model.IssueDescription;
                    ViewBag.CustomerName = model.CustomerName;
                    TempData["Error"] = "Total cost must be greater than 0";
                    return View(model);
                }

                var quotation = new Quotation
                {
                    RepairRequestId = model.RepairRequestId,
                    TechnicianId = technicianId,
                    CreatedDate = DateTime.Now,
                    Status = QuotationStatus.Pending,
                    TotalCost = model.TotalCost,
                    TechnicianNotes = model.TechnicianNotes
                };

                _context.Quotations.Add(quotation);
                await _context.SaveChangesAsync();

                // Add quotation items
                if (model.QuotationItems != null && model.QuotationItems.Any())
                {
                    foreach (var item in model.QuotationItems)
                    {
                        if (item != null && !string.IsNullOrWhiteSpace(item.ItemDescription) && item.UnitCost > 0)
                        {
                            var quotationItem = new QuotationItem
                            {
                                QuotationId = quotation.QuotationId,
                                ItemDescription = item.ItemDescription,
                                UnitCost = item.UnitCost,
                                Quantity = item.Quantity
                            };
                            _context.QuotationItems.Add(quotationItem);
                        }
                    }
                    if (_context.QuotationItems.Local.Any())
                    {
                        await _context.SaveChangesAsync();
                    }
                }

                // Update repair status to Quoted
                var repair = await _context.RepairRequests.FindAsync(model.RepairRequestId);
                if (repair != null)
                {
                    repair.Status = RepairStatus.Qouted;
                    await _context.SaveChangesAsync();
                }

                // Send notification to customer
                var repairRequest = await _context.RepairRequests
                    .Include(r => r.Customers)
                    .FirstOrDefaultAsync(r => r.RequestId == model.RepairRequestId);

                if (repairRequest?.Customers != null)
                {
                    _emailServices.CustomerRegConfirmation(
                        repairRequest.Customers.Email,
                        $"Quotation for your repair has been created. Total cost: R{quotation.TotalCost:N2}");
                }

                TempData["Success"] = "Quotation created successfully!";
                return RedirectToAction("RepairDetail", new { id = model.RepairRequestId });
            }
            catch (Exception ex)
            {
                // Set ViewBag for displaying repair details on error
                ViewBag.RepairRequestId = model.RepairRequestId;
                ViewBag.ItemName = model.ItemName;
                ViewBag.IssueDescription = model.IssueDescription;
                ViewBag.CustomerName = model.CustomerName;
                System.Diagnostics.Debug.WriteLine($"Exception in CreateQuotation: {ex.Message}\n{ex.StackTrace}");
                TempData["Error"] = "Error creating quotation: " + ex.Message;
                return View(model);
            }
        }

        // ── VIEW QUOTATIONS ───────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> ViewQuotations(int? repairId)
        {
            if (!IsTechnician())
                return RedirectToAction("Dashboard");

            var technicianId = GetTechnicianId();

            IQueryable<Quotation> query = _context.Quotations
                .Where(q => q.TechnicianId == technicianId)
                .Include(q => q.RepairRequest)
                .ThenInclude(r => r.Customers)
                .Include(q => q.QuotationItems);

            if (repairId.HasValue)
                query = query.Where(q => q.RepairRequestId == repairId);

            var quotations = await query
                .OrderByDescending(q => q.CreatedDate)
                .ToListAsync();

            return View(quotations);
        }

        // ── EDIT QUOTATION ────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> EditQuotation(int? id)
        {
            if (!IsTechnician() || id == null)
                return RedirectToAction("Dashboard");

            var technicianId = GetTechnicianId();

            var quotation = await _context.Quotations
                .Where(q => q.QuotationId == id && q.TechnicianId == technicianId)
                .Include(q => q.RepairRequest)
                .Include(q => q.QuotationItems)
                .FirstOrDefaultAsync();

            if (quotation == null || quotation.Status != QuotationStatus.Pending)
                return NotFound();

            var model = new EditQuotationViewModel
            {
                QuotationId = quotation.QuotationId,
                RepairRequestId = quotation.RepairRequestId,
                TotalCost = quotation.TotalCost,
                TechnicianNotes = quotation.TechnicianNotes,
                QuotationItems = quotation.QuotationItems.ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuotation(EditQuotationViewModel model)
        {
            if (!IsTechnician())
                return RedirectToAction("Dashboard");

            if (!ModelState.IsValid)
                return View(model);

            var technicianId = GetTechnicianId();

            var quotation = await _context.Quotations
                .Where(q => q.QuotationId == model.QuotationId && q.TechnicianId == technicianId)
                .Include(q => q.QuotationItems)
                .FirstOrDefaultAsync();

            if (quotation == null || quotation.Status != QuotationStatus.Pending)
                return NotFound();

            try
            {
                quotation.TotalCost = model.TotalCost;
                quotation.TechnicianNotes = model.TechnicianNotes;

                // Update items
                _context.QuotationItems.RemoveRange(quotation.QuotationItems);

                foreach (var item in model.QuotationItems ?? new List<QuotationItem>())
                {
                    var quotationItem = new QuotationItem
                    {
                        QuotationId = quotation.QuotationId,
                        ItemDescription = item.ItemDescription,
                        UnitCost = item.UnitCost,
                        Quantity = item.Quantity
                    };
                    _context.QuotationItems.Add(quotationItem);
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Quotation updated successfully!";
                return RedirectToAction("ViewQuotations");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error updating quotation: " + ex.Message;
                return View(model);
            }
        }

        // ── START REPAIR (After quotation approved) ───────────────
        [HttpPost]
        public async Task<IActionResult> StartRepair(int? id)
        {
            if (!IsTechnician() || id == null)
                return RedirectToAction("Dashboard");

            var technicianId = GetTechnicianId();

            var quotation = await _context.Quotations
                .Where(q => q.QuotationId == id && q.TechnicianId == technicianId)
                .FirstOrDefaultAsync();

            if (quotation == null || quotation.Status != QuotationStatus.Approved)
                return NotFound();

            try
            {
                var repair = await _context.RepairRequests.FindAsync(quotation.RepairRequestId);
                if (repair != null)
                {
                    repair.Status = RepairStatus.InProgress;
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Repair started!";
                return RedirectToAction("RepairDetail", new { id = quotation.RepairRequestId });
            }
            catch (Exception)
            {
                TempData["Error"] = "Error starting repair.";
                return RedirectToAction("Dashboard");
            }
        }

        // ── COMPLETE REPAIR ───────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> CompleteRepair(int? id)
        {
            if (!IsTechnician() || id == null)
                return RedirectToAction("Dashboard");

            var technicianId = GetTechnicianId();

            var assignment = await _context.RepairAssignments
                .Where(ra => ra.TechnicianId == technicianId && ra.RepairRequestId == id)
                .Include(ra => ra.RepairRequest)
                .FirstOrDefaultAsync();

            if (assignment == null)
                return NotFound();

            var repair = assignment.RepairRequest;
            if (repair.Status != RepairStatus.InProgress)
                return BadRequest("Repair is not in progress");

            var quotation = await _context.Quotations
                .FirstOrDefaultAsync(q => q.RepairRequestId == id && q.Status == QuotationStatus.Approved);

            var model = new CompleteRepairViewModel
            {
                RepairRequestId = id.Value,
                TotalCost = quotation?.TotalCost ?? 0,
                CustomerName = $"{repair.Customers?.FirstName} {repair.Customers?.LastName}",
                ItemName = repair.ItemName
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRepair(CompleteRepairViewModel model)
        {
            if (!IsTechnician())
                return RedirectToAction("Dashboard");

            if (!ModelState.IsValid)
                return View(model);

            var technicianId = GetTechnicianId();

            var assignment = await _context.RepairAssignments
                .FirstOrDefaultAsync(ra => ra.TechnicianId == technicianId && 
                    ra.RepairRequestId == model.RepairRequestId);

            if (assignment == null)
                return NotFound();

            try
            {
                var repair = await _context.RepairRequests
                    .Include(r => r.Customers)
                    .FirstOrDefaultAsync(r => r.RequestId == model.RepairRequestId);

                if (repair == null)
                    return NotFound();

                repair.Status = RepairStatus.Completed;

                var quotation = await _context.Quotations
                    .FirstOrDefaultAsync(q => q.RepairRequestId == model.RepairRequestId && 
                        q.Status == QuotationStatus.Approved);

                // Create receipt
                var receipt = new Receipt
                {
                    RepairRequestId = model.RepairRequestId,
                    QuotationId = quotation?.QuotationId,
                    TechnicianId = technicianId,
                    IssuedDate = DateTime.Now,
                    FinalAmount = model.TotalCost,
                    ReceiptNumber = $"RCP-{DateTime.Now:yyyyMMddHHmmss}",
                    Notes = model.Notes
                };

                _context.Receipts.Add(receipt);

                // Update technician status back to Available
                var technician = await _context.Technicians.FindAsync(technicianId);
                if (technician != null)
                {
                    technician.Status = AvailabilityStatus.Available;
                }

                await _context.SaveChangesAsync();

                // Send receipt to customer
                if (repair.Customers != null)
                {
                    _emailServices.CustomerRegConfirmation(
                        repair.Customers.Email,
                        $"Your repair for {repair.ItemName} is complete! Receipt: {receipt.ReceiptNumber}");
                }

                TempData["Success"] = "Repair completed and receipt generated!";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error completing repair: " + ex.Message;
                return View(model);
            }
        }

        // ── COMPLETED REPAIRS ─────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> CompletedRepairs()
        {
            if (!IsTechnician())
                return RedirectToAction("Dashboard");

            var technicianId = GetTechnicianId();

            var repairs = await _context.RepairAssignments
                .Where(ra => ra.TechnicianId == technicianId && ra.RepairRequest.Status == RepairStatus.Completed)
                .Include(ra => ra.RepairRequest)
                .ThenInclude(r => r.Customers)
                .Include(ra => ra.RepairRequest.Category)
                .OrderByDescending(ra => ra.AssignedDate)
                .ToListAsync();

            return View(repairs);
        }

        // ── PROFILE ───────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!IsTechnician())
                return RedirectToAction("Dashboard");

            var technicianId = GetTechnicianId();
            var technician = await _context.Technicians
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.TechnicianId == technicianId);

            if (technician == null)
                return RedirectToAction("LogIn", "Authentication");

            return View(technician);
        }
    }

    // ── VIEW MODELS ─────────────────────────────────────────────

    public class CreateQuotationViewModel
    {
        [Required(ErrorMessage = "Repair Request ID is required")]
        public int RepairRequestId { get; set; }

        [Required(ErrorMessage = "Item name is required")]
        public string ItemName { get; set; }

        [Required(ErrorMessage = "Issue description is required")]
        public string IssueDescription { get; set; }

        [Required(ErrorMessage = "Customer name is required")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Total cost is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Total cost must be between 0.01 and 999,999.99")]
        public decimal TotalCost { get; set; }

        public string TechnicianNotes { get; set; }
        public List<QuotationItemViewModel> QuotationItems { get; set; } = new();
    }

    public class EditQuotationViewModel
    {
        public int QuotationId { get; set; }
        public int RepairRequestId { get; set; }
        public decimal TotalCost { get; set; }
        public string TechnicianNotes { get; set; }
        public List<QuotationItem> QuotationItems { get; set; } = new();
    }

    public class QuotationItemViewModel
    {
        public string ItemDescription { get; set; }
        public decimal UnitCost { get; set; }
        public int Quantity { get; set; }
    }

    public class CompleteRepairViewModel
    {
        public int RepairRequestId { get; set; }
        public string CustomerName { get; set; }
        public string ItemName { get; set; }
        public decimal TotalCost { get; set; }
        public string Notes { get; set; }
    }
}
