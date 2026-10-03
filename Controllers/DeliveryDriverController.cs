// ============================================================
// DeliveryDriverController.cs — FULLY CORRECTED
// ============================================================
using SkyTech.Models;
using SkyTech.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace SkyTech.Controllers
{
    [AuthorizeUser]
    public class DeliveryDriverController : Controller
    {
        private readonly ApplicationDbContext db;

        public DeliveryDriverController(ApplicationDbContext context) { db = context; }

        [HttpGet]
        public IActionResult DriverDash()
        {
            int driverId = HttpContext.Session.GetInt32("UserId")!.Value;

            var driver = db.DeliveryDrivers.Find(driverId);
            ViewBag.DriverName = driver?.DriverName + " " + driver?.DriverSurname;
            ViewBag.ApplicationStatus = driver?.ApplicationStatus;
            ViewBag.ActivePage = "Dashboard";

            ViewBag.CompletedDeliveries = db.Orders.Count(o => o.DriverId == driverId && o.Status == OrderStatus.Delivered);
            ViewBag.InProgressDeliveries = db.Orders.Count(o => o.DriverId == driverId && (o.Status == OrderStatus.Assigned || o.Status == OrderStatus.OutForDelivery));

            return View(driver);
        }

        public IActionResult DriverDeliveryView(int id)
        {
            var order = db.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Products)
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null) return NotFound();
            return View(order);
        }

        [HttpGet]
        public IActionResult MyDeliveries()
        {
            int driverId = HttpContext.Session.GetInt32("UserId").Value;

            var deliveries = db.Orders
                .Include(o => o.Customers)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Products)
                .Where(o => o.DriverId == driverId)
                .OrderByDescending(o => o.OrderedOn)
                .ToList();

            ViewBag.DriverId = driverId;
            return View(deliveries);
        }

        [HttpPost]
        public IActionResult StartDelivery(int id)
        {
            var order = db.Orders.Find(id);
            if (order == null)
                return Json(new { success = false, reason = "Order not found" });

            if (DateTime.Today < order.ScheduledDeliveryDate.Date)
                return Json(new
                {
                    success = false,
                    reason = "This delivery is not scheduled for today. Scheduled for "
                    + order.ScheduledDeliveryDate.ToString("dddd, dd MMM yyyy")
                });

            order.Status = OrderStatus.OutForDelivery;
            db.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteDelivery(int id, int driverId)
        {
            var order = db.Orders.Find(id);
            if (order == null)
                return Json(new { success = false, reason = "Order not found" });

            order.Status = OrderStatus.Delivered;
            order.DeliveredOn = DateTime.Now;
            db.SaveChanges();

            var driver = db.DeliveryDrivers.Find(order.DriverId);
            if (driver != null)
            {
                int remainingOrdersToday = db.Orders.Count(o =>
                    o.DriverId == driver.DriverId &&
                    o.ScheduledDeliveryDate.Date == order.ScheduledDeliveryDate.Date &&
                    o.Status != OrderStatus.Delivered);

                if (remainingOrdersToday == 0)
                    driver.IsAvailable = true;

                db.SaveChanges();
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult VerifyDeliveryPin(int orderId, string pin)
        {
            var order = db.Orders.Find(orderId);
            if (order == null)
                return Json(new { success = false, reason = "Order not found" });

            if (order.DeliveryPin != pin.Trim())
                return Json(new { success = false, reason = "Incorrect pin" });

            return Json(new { success = true });
        }

        // =========================== BRANCH TRANSFERS (FIXED) ===========================
        // This is the main view that lists transfers. It exists at /Views/DeliveryDriver/BranchTransfers.cshtml
        public IActionResult BranchTransfers()
        {
            int driverId = HttpContext.Session.GetInt32("UserId")!.Value;

            var transfers = db.InterBranchTransfers
                    .Include(t => t.OrderItem)
                        .ThenInclude(oi => oi.Products)
                    .Include(t => t.OrderItem)
                        .ThenInclude(oi => oi.Order)
                            .ThenInclude(o => o.Customers)
                    .Include(t => t.SourceBranch)
                    .Include(t => t.TargetBranch)
                    .Where(t => t.DriverId == driverId)
                    .OrderBy(t => t.ScheduledTransferDate)
                    .ToList();

            ViewBag.DriverName = HttpContext.Session.GetString("DriverName");
            ViewBag.ActivePage = "BranchTransfers";

            return View(transfers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkTransferInTransit(int transferId)
        {
            int driverId = HttpContext.Session.GetInt32("UserId")!.Value;

            var transfer = db.InterBranchTransfers
                .FirstOrDefault(t => t.TransferId == transferId && t.DriverId == driverId);

            if (transfer == null)
            {
                TempData["Error"] = "Transfer not found or not assigned to you.";
                return RedirectToAction("BranchTransfers");  // ← CORRECTED: was "MyTransfers"
            }

            transfer.TransferStatus = ItemTransferStatus.InTransit;
            db.SaveChanges();

            TempData["Success"] = "Transfer marked as in transit. Please deliver to the destination branch.";
            return RedirectToAction("BranchTransfers");      // ← CORRECTED
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkTransferDelivered(int transferId)
        {
            int driverId = HttpContext.Session.GetInt32("UserId")!.Value;

            var transfer = db.InterBranchTransfers
                .Include(t => t.OrderItem)
                .FirstOrDefault(t => t.TransferId == transferId && t.DriverId == driverId);

            if (transfer == null)
            {
                TempData["Error"] = "Transfer not found or not assigned to you.";
                return RedirectToAction("BranchTransfers");  // ← CORRECTED
            }

            transfer.TransferStatus = ItemTransferStatus.DeliveredToPickupBranch;
            transfer.DeliveredToTargetOn = DateTime.Now;
            transfer.IsDelivered = true;
            db.SaveChanges();

            TempData["Success"] = $"Item {transfer.OrderItem?.ProdName} has been delivered to the pickup branch.";
            return RedirectToAction("BranchTransfers");      // ← CORRECTED
        }

        // =========================== OTHER ACTIONS (UNCHANGED) ===========================
        public IActionResult HomeDeliveries()
        {
            int driverId = HttpContext.Session.GetInt32("UserId")!.Value;

            var deliveries = db.Orders
                .Include(o => o.Customers)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Products)
                .Where(o => o.DriverId == driverId && o.OrderType == DeliveryType.HomeDelivery)
                .OrderByDescending(o => o.OrderedOn)
                .ToList();

            ViewBag.DriverId = driverId;
            ViewBag.DriverName = HttpContext.Session.GetString("DriverName");
            ViewBag.ActivePage = "HomeDeliveries";

            return View(deliveries);
        }

        public IActionResult ReturnedOrderItems()
        {
            int currentId = HttpContext.Session.GetInt32("UserId")!.Value;

            var AsssignedReturns = db.OrderItems
                .Include(i => i.Order)
                    .ThenInclude(o => o.Customers)
                .Include(i => i.Products)
                    .ThenInclude(p => p.ProductPictures)
                .Where(i => i.ReturnPStatus == ReturnProduct.Return_Approved
                         && i.Order.OwnerId == currentId)
                .OrderByDescending(i => i.Order.DeliveredOn)
                .ToList();

            return View(AsssignedReturns);
        }

        public IActionResult MyTasks()
        {
            int driverId = HttpContext.Session.GetInt32("UserId").Value;

            var deliveries = db.Orders
                .Include(o => o.Customers)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Products)
                .Where(o => o.DriverId == driverId && o.Status != OrderStatus.Delivered)
                .OrderBy(o => o.ScheduledDeliveryDate)
                .ToList();

            var collections = db.OrderItems
                .Include(i => i.Order)
                    .ThenInclude(o => o.Customers)
                .Include(i => i.Products)
                .Where(i => i.DriverId == driverId
                         && i.ReturnPStatus == ReturnProduct.Collection_Assigned)
                .OrderBy(i => i.CollectionDate)
                .ToList();

            var redeliveries = db.OrderItems
                .Include(i => i.Order)
                    .ThenInclude(o => o.Customers)
                .Include(i => i.Products)
                .Where(i => i.DriverId == driverId
                         && i.ReturnPStatus == ReturnProduct.Exchange_Dispatched)
                .OrderBy(i => i.NewDeliveryDate)
                .ToList();

            var model = new DriverTasksViewModel
            {
                Deliveries = deliveries,
                Collections = collections,
                Redeliveries = redeliveries
            };

            ViewBag.DriverId = driverId;
            return View(model);
        }

        public IActionResult ReturnTaskDetail(int orderItemId)
        {
            int driverId = HttpContext.Session.GetInt32("UserId").Value;

            var item = db.OrderItems
                .Include(i => i.Order)
                    .ThenInclude(o => o.Customers)
                .Include(i => i.Products)
                .Include(i => i.DeliveryDriver)
                .FirstOrDefault(i => i.OrderItemId == orderItemId && i.DriverId == driverId);

            if (item == null) return NotFound();

            ViewBag.TaskType = item.ReturnPStatus == ReturnProduct.Collection_Assigned ? "Collection" : "Re-delivery";
            ViewBag.TaskIcon = item.ReturnPStatus == ReturnProduct.Collection_Assigned ? "fa-box-open" : "fa-truck";
            ViewBag.TaskColor = item.ReturnPStatus == ReturnProduct.Collection_Assigned ? "#d97706" : "#0891b2";

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkCollected(int orderItemId)
        {
            int driverId = HttpContext.Session.GetInt32("UserId").Value;

            var item = db.OrderItems
                .Include(i => i.Order)
                .Include(i => i.DeliveryDriver)
                .FirstOrDefault(i => i.OrderItemId == orderItemId
                                  && i.DriverId == driverId
                                  && i.ReturnPStatus == ReturnProduct.Collection_Assigned);

            if (item == null) return Json(new { success = false, reason = "Task not found" });

            if (item.DeliveryDriver != null)
                item.DeliveryDriver.IsAvailable = true;

            item.ReturnPStatus = ReturnProduct.Item_Collected;
            item.DriverId = null;
            item.CollectionDate = DateTime.Now;

            db.SaveChanges();
            return Json(new { success = true, message = "Item marked as collected." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkReDelivered(int orderItemId, string pin)
        {
            int driverId = HttpContext.Session.GetInt32("UserId").Value;

            var item = db.OrderItems
                .Include(i => i.Order)
                .Include(i => i.DeliveryDriver)
                .FirstOrDefault(i => i.OrderItemId == orderItemId
                                  && i.DriverId == driverId
                                  && i.ReturnPStatus == ReturnProduct.Exchange_Dispatched);

            if (item == null) return Json(new { success = false, reason = "Task not found" });

            item.ReturnPStatus = ReturnProduct.Completed;
            item.NewDeliveryDate = DateTime.Now;

            if (item.DeliveryDriver != null)
                item.DeliveryDriver.IsAvailable = true;

            db.SaveChanges();
            return Json(new { success = true, message = "Re-delivery completed." });
        }

        //===================================Collection Of proposed items==============================
        public IActionResult ProposalCollections()
        {
            int driverId = HttpContext.Session.GetInt32("UserId").Value;

            var proposals = db.ProposeItems
                .Include(p => p.ProposedProdPics)
                .Include(p => p.Checkout)
                .Include(p => p.Customer)
                .Where(p => p.AssignedDriverId == driverId &&
                            p.ProdStatus == Status.CollectionScheduled)
                .ToList();

            return View(proposals);
        }
    }
}
