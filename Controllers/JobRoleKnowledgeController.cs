using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.JobPostServices;

namespace SkyTech.Controllers
{
    public class JobRoleKnowledgeController : Controller
    {
        private readonly ApplicationDbContext db;
        public JobRoleKnowledgeController(ApplicationDbContext db) { this.db = db; }

        public IActionResult Index()
        {
            var roles = db.JobRoleKnowledges.OrderBy(r => r.JobTitle).ToList();
            return View(roles);
        }


        private async Task PopulateDepartmentsAsync()
        {
            var departments = await db.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            ViewBag.Departments = new SelectList(departments, "DepartmentId", "DepartmentName");
        }

        //public async Task<IActionResult> Create()
        //{
        //    var positions = await db.StaffPositions
        //    .OrderBy(p => p.PositionName)
        //    .ToListAsync();

        //    ViewBag.JobTitles = new SelectList(positions, "PositionName", "PositionName");

        //    await PopulateDepartmentsAsync();
        //    return View();
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(JobRoleKnowledge model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        var positions = await db.StaffPositions
        //            .OrderBy(p => p.PositionName)
        //            .ToListAsync();

        //        ViewBag.JobTitles = new SelectList(positions, "PositionId", "PositionName");

        //        await PopulateDepartmentsAsync();
        //        return View(model);
        //    }

        //    model.CreatedAt = DateTime.Now;
        //    model.UpdatedAt = DateTime.Now;
        //    db.Add(model);
        //    await db.SaveChangesAsync();
        //    TempData["successful"] = "Role information saved";
        //    return RedirectToAction("Index");
        //}

        public async Task<IActionResult> Create()
        {
            var positions = await db.StaffPositions
                .OrderBy(p => p.PositionName)
                .ToListAsync();

            ViewBag.JobTitles = new SelectList(positions, "PositionId", "PositionName"); // value is now PositionId

            await PopulateDepartmentsAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobRoleKnowledge model)
        {
            int? currentId = HttpContext.Session.GetInt32("UserId");

            if (!ModelState.IsValid)
            {
                var positionsInvalid = await db.StaffPositions
                    .OrderBy(p => p.PositionName)
                    .ToListAsync();

                ViewBag.JobTitles = new SelectList(positionsInvalid, "PositionId", "PositionName");

                await PopulateDepartmentsAsync();
                return View(model);
            }

            var position = await db.StaffPositions.FindAsync(model.PositionId);
            if (position == null)
            {
                ModelState.AddModelError("PositionId", "Please select a valid job title.");
                var positionsRetry = await db.StaffPositions.OrderBy(p => p.PositionName).ToListAsync();
                ViewBag.JobTitles = new SelectList(positionsRetry, "PositionId", "PositionName");
                await PopulateDepartmentsAsync();
                return View(model);
            }
            model.HRId = currentId;
            model.JobTitle = position.PositionName; // keep the text copy in sync
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;
            db.Add(model);
            await db.SaveChangesAsync();
            TempData["successful"] = "Role information saved";
            return RedirectToAction("Index");
        }


        public async Task<IActionResult> Edit(int id)
        {
            var role = await db.JobRoleKnowledges.FindAsync(id);
            if (role == null) return NotFound();
            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(JobRoleKnowledge model)
        {
            if (!ModelState.IsValid)
                return View("JobRoleKnowledge/Edit", model);

            var role = await db.JobRoleKnowledges.FindAsync(model.RoleId);
            if (role == null) return NotFound();

            var position = await db.StaffPositions
                    .FirstOrDefaultAsync(p => p.PositionId == role.PositionId);

            if (position == null)
            {
                ModelState.AddModelError("", "Selected position does not exist.");
                return View(role);
            }

            role.JobTitle = position.PositionName;
            role.CompanyResponsibilities = model.CompanyResponsibilities;
            role.RequiredSkills = model.RequiredSkills;
            role.RequiredExperience = model.RequiredExperience;
            role.RequiredQualifications = model.RequiredQualifications;
            role.OtherCompanyInformation = model.OtherCompanyInformation;
            role.IsActive = model.IsActive;
            role.UpdatedAt = DateTime.Now;

            await db.SaveChangesAsync();
            TempData["successful"] = "Role information updated";
            return RedirectToAction("Index");
        }
    }
}