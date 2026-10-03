using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.Repair_Item_Services;

namespace SkyTech.Controllers
{
    public class Request_Repair_Service : Controller
    {
        private readonly ApplicationDbContext _context;

        public Request_Repair_Service(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Request_Repair_Service
        public IActionResult Index()
        {
            var applicationDbContext = _context.RepairRequests.Include(r => r.Category).Include(r => r.Customers);
            return View(applicationDbContext.ToList());
        }

        // GET: Request_Repair_Service/Details/5
        public IActionResult Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var repair_Request = _context.RepairRequests
                .Include(r => r.Category)
                .Include(r => r.Customers)
                .FirstOrDefault(m => m.RequestId == id);
            if (repair_Request == null)
            {
                return NotFound();
            }

            return View(repair_Request);
        }

        // GET: Request_Repair_Service/Create
        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userType = HttpContext.Session.GetString("UserType");

            if (userId == null || userType != "Customer")
                return RedirectToAction("LogIn", "Authentication");

            ViewData["CategoryId"] = new SelectList(_context.RepairItemCategories, "CategoryId", "CategoryName");
            ViewData["CustomerId"] = new SelectList(_context.Customers, "CustomerId", "FirstName");
            return View();
        }

        // POST: Request_Repair_Service/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create (Repair_Request repair_Request)
        {

            // Read customer ID directly from session
            var userId = HttpContext.Session.GetInt32("UserId");
            var userType = HttpContext.Session.GetString("UserType");

            if (userId == null || userType != "Customer")
                return RedirectToAction("LogIn", "Authentication");


            // Assign the customer ID from session — no DB lookup needed
            repair_Request.CustomerId = userId.Value;

            // Remove CustomerId from ModelState so it doesn't cause validation errors
            // since we're setting it manually and it wasn't in the form
            ModelState.Remove("CustomerId");

            // Remove fields we set manually or that are navigation properties
            ModelState.Remove("CustomerId");
            ModelState.Remove("Category");      // navigation property — always null on POST
            ModelState.Remove("Customers");     // navigation property — always null on POST
            ModelState.Remove("Status");        // set manually below
            ModelState.Remove("DateCreated");   // set manually below

            if (!ModelState.IsValid)
            {
                // optional: log what's still failing
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    foreach (var error in state.Errors)
                        System.Diagnostics.Debug.WriteLine($"ModelState error — {key}: {error.ErrorMessage}");
                }

                ViewData["CategoryId"] = new SelectList(
                    _context.RepairItemCategories, "CategoryId", "CategoryName",
                    repair_Request.CategoryId);
                return View(repair_Request);
            }

                repair_Request.DateCreated = DateTime.Now;
                repair_Request.Status = RepairStatus.Pending;
                _context.Add(repair_Request);
                _context.SaveChanges();
               
            ViewData["CategoryId"] = new SelectList(_context.RepairItemCategories, "CategoryId", "CategoryName", repair_Request.CategoryId);
            ViewData["CustomerId"] = new SelectList(_context.Customers, "CustomerId", "FirstName", repair_Request.CustomerId);
            return RedirectToAction("Index", "Request_Repair_Service");
          
        }


        // GET: Request_Repair_Service/Delete/5
        public IActionResult Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var repair_Request = _context.RepairRequests
                .Include(r => r.Category)
                .Include(r => r.Customers)
                .FirstOrDefault(m => m.RequestId == id);
            if (repair_Request == null)
            {
                return NotFound();
            }

            return View(repair_Request);
        }

        // POST: Request_Repair_Service/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var repair_Request = _context.RepairRequests.Find  (id);
            if (repair_Request != null)
            {
                _context.RepairRequests.Remove(repair_Request);
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        private bool Repair_RequestExists(int id)
        {
            return _context.RepairRequests.Any(e => e.RequestId == id);
        }
    }
}
