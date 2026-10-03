using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.Documents;
using SkyTech.Models.JobPostServices;
using SkyTech.Services;
using SkyTech.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography.Xml;

namespace SkyTech.Controllers
{
    [AuthorizeUser]
    public class ShopOwnerController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly EmailServices _services = null;  // ← add this
        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly LeaseReturnService leaseReturnService;

        //paying customer their money
        private readonly PayStackTransferService _transfer;
        //private readonly IAIJobPostDraftService _aiService;

        //public ShopOwnerController()
        //{
        //    //this.db = db;
        //}

        public ShopOwnerController(ApplicationDbContext context, EmailServices services, IWebHostEnvironment webHostEnvironment, PayStackTransferService transfer, LeaseReturnService leaseReturnService/*IAIJobPostDraftService aiService*/)
        {
            //this.db = db;

            db = context;
            _transfer = transfer;
            this.leaseReturnService = leaseReturnService;
            //_aiService = aiService;
            _services = services;
            _webHostEnvironment = webHostEnvironment;
        }
        //public ShopOwnerController(ApplicationDbContext context) { db = context; }

        public IActionResult ShopOwnerDash()
        {
            int currentId = HttpContext.Session.GetInt32("UserId").Value;

            var appStat = db.shopowners
                .Where(a => a.OwnerId == currentId)
                .Select(a => a.ApplicationStatus)
                .FirstOrDefault();

            var drivers = db.DeliveryDrivers
                .Count(d => d.ApplicationStatus == Status.Approved && d.IsAvailable);

            //Now filters directly on Order.OwnerId instead of OrderItems
            var pendingOrdersCount = db.Orders
                .Where(i => i.OwnerId == currentId && i.Status == OrderStatus.Pending)
                .Count();

            var completed = db.Orders
                .Where(i => i.OwnerId == currentId && i.Status == OrderStatus.Delivered)
                .Count();

            var totalStoreSales = db.Orders
                .Where(oi => oi.Status == OrderStatus.Delivered && oi.OwnerId == currentId)
                .Sum(oi => (double?)oi.TotalAmount) ?? 0;

            // EF Core: EF.Functions.DateDiffWeek replaces SqlFunctions.DatePart
            var weeklySales = db.OrderItems
                 .Include(o => o.Order)        // Added the two includes Order and Products
                 .Include(o => o.Products)
                .Where(o => o.Products.OwnerId == currentId && o.Order.Status == OrderStatus.Delivered)
                .AsEnumerable() // pulls to memory first
                .GroupBy(o => System.Globalization.ISOWeek.GetWeekOfYear(o.Order.OrderedOn))
                .Select(g => new { Week = g.Key, Total = g.Sum(x => x.PriceAtTimeOfPurchase * x.Quantity) })
                .OrderBy(x => x.Week)
                .ToList();

            ViewBag.WeeklySalesLabels = weeklySales.Select(x => "Week " + x.Week).ToList();
            ViewBag.WeeklySalesData = weeklySales.Select(x => x.Total).ToList();
            ViewBag.ApplicationStatus = appStat;
            ViewBag.AvailableDrivers = drivers;
            ViewBag.PendingOrders = pendingOrdersCount;
            ViewBag.CompletedOrders = completed;
            ViewBag.TotalSales = totalStoreSales;

            return View(new ShopOwner());
        }

        [HttpGet]
        public IActionResult AddProductPage() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddProductPage(AddProductViewModel model)
        {
            if (!ModelState.IsValid) return View("AddProductPage");
          
            //var sessionId = 
            int currentId = HttpContext.Session.GetInt32("UserId").Value;

            // Verify this OwnerId actually exists in the shopowners table
            var ownerExists = db.shopowners.Any(o => o.OwnerId == currentId);
            if (!ownerExists)
            {
                TempData["Error"] = $"Owner with ID {currentId} not found. Session may be storing the wrong ID.";
                return View("AddProductPage");
            }

            var addProduct = new AddProduct
            {
                OwnerId = currentId,
                ProdName = model.ProdName,
                ProdCategory = model.ProdCategory,
                ProdType = model.ProdType,
                ProdQuantity = model.ProdQuantity,
                ProdDescription = model.ProdDescription,
                ProdPrice = model.ProdPrice,
                ProdSize = model.ProdSize,
                ProductPictures = new List<ProductPicture>(),
                Status = model.ProdQuantity == 0 ? ProdStatus.Out_Of_Stock
                                 : model.ProdQuantity <= 10 ? ProdStatus.Low_stock
                                                            : ProdStatus.In_Stock
            };

            if (model.ProductPicture != null && model.ProductPicture.Length > 0)
            {
                 var pictureFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "ProductPicture");
                 Directory.CreateDirectory(pictureFolder);
                
                 var fileName = $"{currentId}_{Guid.NewGuid()}{Path.GetExtension(model.ProductPicture.FileName)}";
                 var filePath = Path.Combine(pictureFolder, fileName);
                
                 using (var stream = new FileStream(filePath, FileMode.Create))
                 {
                     model.ProductPicture.CopyTo(stream);
                 }

                addProduct.ProductPictures.Add(new ProductPicture
                {
                    OwnerId = currentId,
                    FilePath = $"/Uploads/ProductPicture/{fileName}",
                    FileName = Path.GetFileName(model.ProductPicture.FileName),
                    FileType = model.ProductPicture.ContentType,
                    SavedOn = DateTime.Now
                });
            }

            db.AddProducts.Add(addProduct);
            db.SaveChanges();

            TempData["Message"] = "Product added successfully!";
            return RedirectToAction("AddProductPage", "ShopOwner");
        }

        [HttpGet]
        public IActionResult EditProductPage()
        {
            int currentId = HttpContext.Session.GetInt32("UserId").Value;
            var product = db.AddProducts.FirstOrDefault(o => o.OwnerId == currentId);

            if (product == null) return NotFound();

            return View(new AddProductViewModel
            {
                ProdName = product.ProdName,
                ProdCategory = product.ProdCategory,
                ProdQuantity = product.ProdQuantity,
                ProdDescription = product.ProdDescription,
                ProdPrice = product.ProdPrice,
                ProdSize = product.ProdSize
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProductPage(AddProductViewModel model, IFormFile ProductPicture)
        {
            int currentId = HttpContext.Session.GetInt32("UserId").Value;
            var product = db.AddProducts
                .Include(p => p.ProductPictures)
                .FirstOrDefault(o => o.OwnerId == currentId);

            if (product == null) return NotFound();

            product.ProdName = model.ProdName;
            product.ProdCategory = model.ProdCategory;
            product.ProdType = model.ProdType;
            product.ProdQuantity = model.ProdQuantity;
            product.ProdDescription = model.ProdDescription;
            product.ProdPrice = model.ProdPrice;
            product.ProdSize = model.ProdSize;
            product.Status = model.ProdQuantity == 0 ? ProdStatus.Out_Of_Stock     // the product status was not updating so i added this.
                                 : model.ProdQuantity <= 10 ? ProdStatus.Low_stock
                                                            : ProdStatus.In_Stock;


            if (ProductPicture != null && ProductPicture.Length > 0)
            {
                // Save new file to disk
                var pictureFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "ProductPicture");
                Directory.CreateDirectory(pictureFolder);
            
                var fileName = $"{currentId}_{Guid.NewGuid()}{Path.GetExtension(ProductPicture.FileName)}";
                var filePath = Path.Combine(pictureFolder, fileName);
            
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    ProductPicture.CopyTo(stream);
                }
            
                var existingPic = product.ProductPictures.FirstOrDefault();
                if (existingPic != null)
                {
                    // Delete old file from disk before replacing
                    var oldFullPath = Path.Combine(_webHostEnvironment.WebRootPath, existingPic.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFullPath))
                        System.IO.File.Delete(oldFullPath);
            
                    // Update existing record with new path
                    existingPic.FilePath = $"/Uploads/ProductPicture/{fileName}";
                    existingPic.FileName = Path.GetFileName(ProductPicture.FileName);
                    existingPic.FileType = ProductPicture.ContentType;
                    existingPic.SavedOn = DateTime.Now;
                }
                else
                {
                    // No picture existed before, add a new one
                    product.ProductPictures.Add(new ProductPicture
                    {
                        OwnerId = currentId,
                        FilePath = $"/Uploads/ProductPicture/{fileName}",
                        FileName = Path.GetFileName(ProductPicture.FileName),
                        FileType = ProductPicture.ContentType,
                        SavedOn = DateTime.Now
                    });
                }

            }

            db.SaveChanges();
            TempData["SuccessMessage"] = "Product updated successfully!";
            return RedirectToAction("TrackStockLevelPage");
        }

        public IActionResult TrackStockLevelPage()
        {
            int currentId = HttpContext.Session.GetInt32("UserId").Value;
            var allProds = db.AddProducts
                .Include(p => p.ProductPictures)
                .Where(p => p.OwnerId == currentId)
                .ToList();
            return View(allProds);
        }

        [HttpGet]
        public IActionResult StaffReg() => View();

        [HttpPost]
        public IActionResult StaffReg(StaffViewModel model)
        {
            int currentId = HttpContext.Session.GetInt32("UserId").Value;

            if (!ModelState.IsValid) return View("StaffReg");

            if (db.Customers.Any(d => d.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already exists");
                return View("StaffReg");
            }

            db.Staffs.Add(new Staff
            {
                OwnerId = currentId,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Password = model.Password,
                UserType = "Staff",
                DateEmployed = DateTime.Now
            });
            db.SaveChanges();

            _services.CustomerRegConfirmation(model.Email, model.FirstName);
            TempData["Message"] = "Registration successful";
            return RedirectToAction("ShopOwnerDash");
        }

        //public IActionResult StaffMembers()
        //{
        //    int currentId = HttpContext.Session.GetInt32("UserId").Value;
        //    return View(db.  Staffs.Where(p => p.OwnerId == currentId).ToList());
        //}

        public IActionResult ManageDeliveriesPage() => View();

        public IActionResult OwnerProfile()
        {
            int currentId = HttpContext.Session.GetInt32("UserId").Value;
            return View(db.shopowners.Where(p => p.OwnerId == currentId).ToList());
        }

        //Separating orders
        // DELIVERY ORDERS - Shows orders delivered to this branch AND items that need to be transported OUT
        public IActionResult DeliveryOrders()
        {
            int currentId = HttpContext.Session.GetInt32("UserId")!.Value;

            // 1. Home Delivery orders belonging to this branch
            var deliveryOrders = db.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Products)
                .Include(o => o.Driver)
                .Include(o => o.Customers)
                .Where(o => o.OwnerId == currentId
                            && o.OrderType == DeliveryType.HomeDelivery)
                .OrderByDescending(o => o.OrderedOn)
                .ToList();

            // 2. Items from this branch that need to be transported to another branch (for pickup orders)
            var transfersOut = db.InterBranchTransfers
                .Include(t => t.OrderItem)
                    .ThenInclude(oi => oi.Order)
                        .ThenInclude(o => o.Customers)
                .Include(t => t.OrderItem)
                    .ThenInclude(oi => oi.Products)
                .Include(t => t.TargetBranch)
                .Include(t => t.Driver)
                .Where(t => t.SourceBranchId == currentId
                            && t.TransferStatus != ItemTransferStatus.DeliveredToPickupBranch)
                .OrderBy(t => t.ScheduledTransferDate)
                .ToList();

            ViewBag.TransfersOut = transfersOut;
            ViewBag.AvailableDrivers = db.DeliveryDrivers
                .Where(d => d.IsAvailable && d.ApplicationStatus == Status.Approved)
                .ToList();

            return View(deliveryOrders);
        }

        // PICKUP ORDERS - Shows orders where customer will collect from this branch
        public IActionResult PickupOrders()
        {
            int currentId = HttpContext.Session.GetInt32("UserId")!.Value;

            // Get all pickup orders where this branch is the collection point
            var pickupOrders = db.PickupOrders
                .Include(p => p.Order)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Products)
                .Include(p => p.Order)
                    .ThenInclude(o => o.Customers)
                .Where(p => p.PickupBranchId == currentId)
                .OrderByDescending(p => p.PickupOrderId)
                .ToList();

            // Also get items that have been delivered to this branch from other branches
            var incomingTransfers = db.InterBranchTransfers
                .Include(t => t.OrderItem)
                    .ThenInclude(oi => oi.Order)
                        .ThenInclude(o => o.Customers)
                .Include(t => t.OrderItem)
                    .ThenInclude(oi => oi.Products)
                .Include(t => t.SourceBranch)
                .Where(t => t.TargetBranchId == currentId)
                .OrderBy(t => t.DeliveredToTargetOn)
                .ToList();

            ViewBag.IncomingTransfers = incomingTransfers;

            return View(pickupOrders);
        }

        // Optional: Keep old ReceivedOrders if needed for backward compatibility, or remove it

        public IActionResult AssignDriver(int id)
        {
            var order = db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("DeliveryOrders");
            }

            var availableDrivers = db.DeliveryDrivers
                .Where(d => d.IsAvailable && d.ApplicationStatus == Status.Approved)
                .ToList();

            ViewBag.DeliveryDrivers = availableDrivers;
            ViewBag.IsTransfer = false;

            return View("AssignDriver", order);
        }



        [HttpPost]
        public IActionResult AssignDriver(int orderId, int driverId)
        {
            var order = db.Orders.Find(orderId);
            if (order == null)
                return Json(new { success = false, reason = "Order not found" });

            var driver = db.DeliveryDrivers.Find(driverId);
            if (driver == null)
                return Json(new { success = false, reason = "Driver not found" });

            // Count how many orders this driver is already assigned for
            // on the same scheduled delivery date as this order
            int ordersOnSameDay = db.Orders.Count(o =>
                o.DriverId == driverId &&
                o.ScheduledDeliveryDate.Date == order.ScheduledDeliveryDate.Date &&
                o.Status != OrderStatus.Delivered);

            // Block assignment if driver already has 5 orders for that day
            if (ordersOnSameDay >= 5)
                return Json(new { success = false, reason = "This driver already has 5 orders assigned for that day. Please choose another driver." });

            // Assign driver
            order.DriverId = driverId;
            order.Status = OrderStatus.Assigned;

            // Check total orders assigned for that day after this assignment
            int totalAfterAssignment = ordersOnSameDay + 1;

            // Mark driver unavailable if they now have 5 orders for that day
            if (totalAfterAssignment >= 5)
                driver.IsAvailable = false;

            db.SaveChanges();
            return RedirectToAction("DeliveryOrders");
        }

        public IActionResult AssignTransferDriver(int transferId)
        {
            var transfer = db.InterBranchTransfers
                .Include(t => t.OrderItem)
                    .ThenInclude(oi => oi.Order)
                        .ThenInclude(o => o.Customers)
                .Include(t => t.SourceBranch)
                .Include(t => t.TargetBranch)
                .FirstOrDefault(t => t.TransferId == transferId);

            if (transfer == null)
            {
                TempData["Error"] = "Transfer not found.";
                return RedirectToAction("DeliveryOrders");
            }

            var availableDrivers = db.DeliveryDrivers
                .Where(d => d.IsAvailable && d.ApplicationStatus == Status.Approved)
                .ToList();

            ViewBag.DeliveryDrivers = availableDrivers;
            ViewBag.IsTransfer = true;
            ViewBag.TransferId = transferId;
            ViewBag.TransferData = transfer;

            // Create empty order for the view
            var emptyOrder = new Order();

            return View("AssignDriver", emptyOrder);
        }

        //Assigning the driver for order transfer 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AssignTransferDriver(int transferId, int driverId)
        {
            var transfer = db.InterBranchTransfers.Find(transferId);
            var driver = db.DeliveryDrivers.Find(driverId);

            if (transfer == null || driver == null)
            {
                TempData["Error"] = "Transfer or driver not found.";
                return RedirectToAction("DeliveryOrders");
            }

            transfer.DriverId = driverId;
            transfer.TransferStatus = ItemTransferStatus.AssignedToDriver;

            db.SaveChanges();

            TempData["Success"] = $"Driver {driver.DriverName} assigned to transfer item {transfer.OrderItem?.ProdName}.";

            return RedirectToAction("DeliveryOrders");
        }

        //Pickup branch orders
        public IActionResult PickupBranchOrders()
        {
            int currentId = HttpContext.Session.GetInt32("UserId")!.Value;

            // All pickup orders where THIS branch is the collection point
            var pickupOrders = db.PickupOrders
                .Include(p => p.Order)
                    .ThenInclude(o => o.Customers)
                .Include(p => p.Order)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Products)
                .Include(p => p.Order)
                    .ThenInclude(o => o.ShopOwner)
                .Where(p => p.PickupBranchId == currentId)
                .OrderByDescending(p => p.Order.OrderedOn)
                .ToList();

            // All inbound transfers coming TO this branch
            var inboundTransfers = db.InterBranchTransfers
                .Include(t => t.OrderItem)
                    .ThenInclude(oi => oi.Order)
                .Include(t => t.OrderItem)
                    .ThenInclude(oi => oi.Products)
                .Include(t => t.SourceBranch)
                .Include(t => t.Driver)
                .Where(t => t.TargetBranchId == currentId)
                .ToList();

            ViewBag.InboundTransfers = inboundTransfers;

            return View(pickupOrders);
        }

        [HttpPost]
        public IActionResult ConfirmTransferArrival(int transferId, string transferCode)
        {
            int currentId = HttpContext.Session.GetInt32("UserId")!.Value;

            var transfer = db.InterBranchTransfers
                .Include(t => t.OrderItem)
                .FirstOrDefault(t => t.TransferId == transferId
                                  && t.TargetBranchId == currentId);

            if (transfer == null)
            {
                TempData["Error"] = "Transfer not found.";
                return RedirectToAction("PickupBranchOrders");
            }

            if (transfer.TransferCode != transferCode.Trim().ToUpper())
            {
                TempData["Error"] = "Incorrect transfer code. Please try again.";
                return RedirectToAction("PickupBranchOrders");
            }

            transfer.IsDelivered = true;
            transfer.TransferStatus = ItemTransferStatus.DeliveredToPickupBranch;
            transfer.DeliveredToTargetOn = DateTime.Now;

            // Check if ALL transfers for this pickup order are now delivered
            // so we can mark the pickup order as ReadyForPickup
            var orderItemOrderId = transfer.OrderItem.OrderId;
            var allTransfersForOrder = db.InterBranchTransfers
                .Include(t => t.OrderItem)
                .Where(t => t.OrderItem.OrderId == orderItemOrderId
                         && t.TargetBranchId == currentId)
                .ToList();

            bool allDelivered = allTransfersForOrder.All(t => t.IsDelivered);
            if (allDelivered)
            {
                var pickupOrder = db.PickupOrders
                    .FirstOrDefault(p => p.OrderId == orderItemOrderId
                                      && p.PickupBranchId == currentId);
                if (pickupOrder != null)
                {
                    pickupOrder.Status = PickupStatus.ReadyForPickup;
                    db.SaveChanges();
                }
            }

            db.SaveChanges();
            TempData["Success"] = "Item arrival confirmed successfully.";
            return RedirectToAction("PickupBranchOrders");
        }

        [HttpPost]
        public IActionResult MarkOrderCollected(int pickupOrderId)
        {
            int currentId = HttpContext.Session.GetInt32("UserId")!.Value;

            var pickupOrder = db.PickupOrders
                .Include(p => p.Order)
                .FirstOrDefault(p => p.PickupOrderId == pickupOrderId
                                  && p.PickupBranchId == currentId);

            if (pickupOrder == null)
            {
                TempData["Error"] = "Pickup order not found.";
                return RedirectToAction("PickupBranchOrders");
            }

            pickupOrder.Status = PickupStatus.Collected;
            pickupOrder.CollectedOn = DateTime.Now;
            pickupOrder.Order.Status = OrderStatus.Delivered;
            pickupOrder.Order.DeliveredOn = DateTime.Now;

            db.SaveChanges();
            TempData["Success"] = "Order marked as collected.";
            return RedirectToAction("PickupBranchOrders");
        }

       
        // Shows ALL active returns (not just pending)
        public IActionResult ReturnedItems()
        {
            int currentId = HttpContext.Session.GetInt32("UserId")!.Value;

            var activeReturns = db.OrderItems
                .Include(i => i.Order)
                    .ThenInclude(o => o.Customers)
                .Include(i => i.Products)
                    .ThenInclude(p => p.ProductPictures)
                .Include(i => i.DeliveryDriver)
                .Where(i => i.Order.OwnerId == currentId
                         && i.ReturnPStatus != ReturnProduct.No_Request
                         && i.ReturnPStatus != ReturnProduct.Completed)
                .OrderByDescending(i => i.Order.DeliveredOn)
                .ToList();

            return View(activeReturns);
        }

        // Step 1 — shop owner approves & assigns collection driver
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveReturn(int orderItemId)
        {
            int currentId = HttpContext.Session.GetInt32("UserId")!.Value;

            var item = db.OrderItems
                .Include(i => i.Order)
                .FirstOrDefault(i => i.OrderItemId == orderItemId
                                  && i.Order.OwnerId == currentId);

            if (item == null) return NotFound();

            var availableDriver = db.DeliveryDrivers
                .FirstOrDefault(d => d.IsAvailable == true
                                  && d.ApplicationStatus == Status.Approved);

            if (availableDriver == null)
            {
                TempData["Error"] = "No drivers are currently available. Please try again later.";
                return RedirectToAction("ReturnedItems");
            }

            item.ReturnPStatus = ReturnProduct.Collection_Assigned;
            item.DriverId = availableDriver.DriverId;
            item.IsAssingedForCollection = true;
            item.CollectionDate = DateTime.Now.AddDays(1);
            item.NewDeliveryDate = DateTime.Now.AddDays(4);

            availableDriver.IsAvailable = false;
            db.SaveChanges();

            TempData["Message"] = $"Return for '{item.ProdName}' approved. Driver {availableDriver.DriverName} {availableDriver.DriverSurname} assigned for collection.";
            return RedirectToAction("ReturnedItems");
        }

        // Step 2 — mark item as collected (driver picked it up, now being inspected)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkItemCollected(int orderItemId)
        {
            int currentId = HttpContext.Session.GetInt32("UserId")!.Value;

            var item = db.OrderItems
                .Include(i => i.Order)
                .Include(i => i.DeliveryDriver)
                .FirstOrDefault(i => i.OrderItemId == orderItemId
                                  && i.Order.OwnerId == currentId
                                  && i.ReturnPStatus == ReturnProduct.Collection_Assigned);

            if (item == null) return NotFound();

            // Free up the collection driver
            if (item.DeliveryDriver != null)
                item.DeliveryDriver.IsAvailable = true;

            item.ReturnPStatus = ReturnProduct.Item_Collected;
            item.DriverId = null;   // clear driver — new one assigned in next step

            db.SaveChanges();

            TempData["Message"] = $"'{item.ProdName}' marked as collected and is now being inspected/replaced.";
            return RedirectToAction("ReturnedItems");
        }

        // Step 3 — assign a driver to deliver the replacement/exchange item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AssignExchangeDriver(int orderItemId)
        {
            int currentId = HttpContext.Session.GetInt32("UserId")!.Value;

            var item = db.OrderItems
                .Include(i => i.Order)
                .FirstOrDefault(i => i.OrderItemId == orderItemId
                                  && i.Order.OwnerId == currentId
                                  && i.ReturnPStatus == ReturnProduct.Item_Collected);

            if (item == null) return NotFound();

            var availableDriver = db.DeliveryDrivers
                .FirstOrDefault(d => d.IsAvailable == true
                                  && d.ApplicationStatus == Status.Approved);

            if (availableDriver == null)
            {
                TempData["Error"] = "No drivers available for re-delivery. Please try again later.";
                return RedirectToAction("ReturnedItems");
            }

            item.ReturnPStatus = ReturnProduct.Exchange_Dispatched;
            item.DriverId = availableDriver.DriverId;
            item.IsAssingedForSecDelivery = true;
            item.NewDeliveryDate = DateTime.Now.AddDays(1);

            availableDriver.IsAvailable = false;
            db.SaveChanges();

            TempData["Message"] = $"Replacement for '{item.ProdName}' dispatched. Driver {availableDriver.DriverName} {availableDriver.DriverSurname} assigned for delivery.";
            return RedirectToAction("ReturnedItems");
        }

        public IActionResult AssignCollection() => View();

        //=================================Increament 2 ==========================

        //public IActionResult EvaluateProposal() => View();
        public IActionResult EvaluateProposal()
        {
            var proposals = db.ProposeItems
                .Include(p => p.ReceiptPics)
                .Include(p => p.ProposedProdPics)
                .Where(p => p.ProdStatus == Status.Pending) // ← only show pending
                .ToList();

            return View(proposals);
        }

        // action to open the partial view so that the shop owner can enter the proposed amount 
        [HttpGet]
        public IActionResult AcceptProposal(int id)
        {
            var proposal = db.ProposeItems.Find(id);
            if (proposal == null) return NotFound();

            return PartialView("_AcceptProposalModal", proposal);
        }

        [HttpPost]
        public IActionResult AcceptProposal(int id, double amount)
        {
            var proposal = db.ProposeItems.Find(id);
            if (proposal == null) return NotFound();

            int currentId = HttpContext.Session.GetInt32("UserId").Value;

            // Fetch the logged-in shop owner's address
            var shopOwner = db.shopowners.FirstOrDefault(s => s.OwnerId == currentId);

            proposal.AmountProposed = amount;
            proposal.StoreAddress = shopOwner?.ShopAddress ?? "Address not found";
            proposal.ProdStatus = Status.Approved;

            db.SaveChanges();

            return RedirectToAction("EvaluateProposal");
        }

        // this one declines the proposal
        [HttpPost]
        public IActionResult DeclineProposal(int id)
        {
            var proposal = db.ProposeItems.Find(id);
            if (proposal == null) return NotFound();

            proposal.ProdStatus = Status.Rejected;

            db.SaveChanges();

            return RedirectToAction("EvaluateProposal");
        }

        public IActionResult AcceptedProposals()
        {
            var proposals = db.ProposeItems
                .Include(p => p.ReceiptPics)
                .Include(p => p.ProposedProdPics)
                .Include(p => p.Checkout)
                .Where(p => p.ProdStatus == Status.Approved ||
                            p.ProdStatus == Status.CustomerAccepted ||
                            p.ProdStatus == Status.Item_Collected ||
                            p.ProdStatus == Status.CollectionScheduled ||
                            p.ProdStatus == Status.CustomerAccepted)
                .ToList();

            return View(proposals);
        }
        // GET — shows confirmation page
        [HttpGet]
        public IActionResult ScheduleCollection(int id)
        {
            var proposal = db.ProposeItems
                .Include(p => p.Checkout)
                .FirstOrDefault(p => p.PropItemId == id);

            if (proposal == null) return NotFound();

            return View(proposal);
        }

        // POST — does the actual work
        [HttpPost]
        [ActionName("ScheduleCollection")]  // ← tells ASP.NET this is still "ScheduleCollection"
        public IActionResult ScheduleCollectionConfirmed(int id)  // ← different method name
        {
            var proposal = db.ProposeItems.Find(id);

            if (proposal == null) return NotFound();

            bool anyDriversExist = db.DeliveryDrivers
                .Any(d => d.ApplicationStatus == Status.Approved);

            if (!anyDriversExist)
            {
                TempData["Error"] = "No drivers are registered in the system yet.";
                return RedirectToAction("AcceptedProposals");
            }

            var availableDriver = db.DeliveryDrivers
                .FirstOrDefault(d => d.IsAvailable == true &&
                                     d.ApplicationStatus == Status.Approved);

            if (availableDriver == null)
            {
                TempData["Error"] = "All drivers are currently busy. Please try again later.";
                return RedirectToAction("AcceptedProposals");
            }

            DateTime collectionDate = DateTime.Now.AddDays(4);

            proposal.CollectionDate = collectionDate;
            proposal.ProdStatus = Status.CollectionScheduled;
            proposal.AssignedDriverId = availableDriver.DriverId;

            availableDriver.IsAvailable = true;

            db.SaveChanges();

            TempData["Success"] = $"Collection scheduled for {collectionDate:dd MMM yyyy}. " +
                                  $"Driver {availableDriver.DriverName} " +
                                  $"{availableDriver.DriverSurname} has been assigned.";

            return RedirectToAction("AcceptedProposals");
        }

        // ── GET: show payment confirmation page ───────────────────────────────────────
        [HttpGet]
        public IActionResult MakePayment(int id)
        {
            int currentId = HttpContext.Session.GetInt32("UserId")!.Value;

            var proposal = db.ProposeItems
                .Include(p => p.Customer)
                .Include(p => p.Checkout)
                .FirstOrDefault(p => p.PropItemId == id);

            if (proposal == null) return NotFound();

            if (proposal.Checkout == null)
            {
                TempData["Error"] = "Customer has not submitted their details yet.";
                return RedirectToAction("AcceptedProposals");
            }

            if (proposal.ProdStatus == Status.PaymentMade)
            {
                TempData["Error"] = "Payment has already been made for this proposal.";
                return RedirectToAction("AcceptedProposals");
            }

            return View(proposal);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("MakePayment")]
        public IActionResult MakeTranferPayment(int propItemId)
        {
            var proposal = db.ProposeItems
                .Include(p => p.Customer)
                .Include(p => p.Checkout)
                .FirstOrDefault(p => p.PropItemId == propItemId);

            if (proposal == null) return NotFound();

            if (proposal.ProdStatus == Status.PaymentMade)
            {
                TempData["Error"] = "Payment has already been made for this proposal.";
                return RedirectToAction("AcceptedProposals");
            }

            // Mark as paid
            proposal.ProdStatus = Status.PaymentMade;
            db.SaveChanges();

            // Pass details to the view via TempData so the success popup can display them
            TempData["PaymentSuccess"] = "true";
            TempData["PaidAmount"] = proposal.AmountProposed.ToString("N2");
            TempData["PaidToName"] = proposal.Checkout?.AccountHolderName ?? "Customer";
            TempData["PaidProductName"] = proposal.ProdName;

            return RedirectToAction("AcceptedProposals");
        }




        [HttpGet]
        public IActionResult AddLeasableProductPage() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddLeasableProductPage(AddLeasableProductViewModel model)
        {
            if (!ModelState.IsValid) return View("AddLeasableProductPage");

            int currentId = HttpContext.Session.GetInt32("UserId").Value;

            // Verify this OwnerId actually exists in the shopowners table
            var ownerExists = db.shopowners.Any(o => o.OwnerId == currentId);
            if (!ownerExists)
            {
                TempData["Error"] = $"Owner with ID {currentId} not found. Session may be storing the wrong ID.";
                return View("AddLeasableProductPage");
            }

            // Quantity is implicit: one serial number = one physical unit.
            // Blank rows (unfilled "+ Add another unit" rows) are dropped.
            //
            // FIX: SerialNumbers and ConditionNotes are paired by index BEFORE
            // filtering blank serial rows out, so a blank row in the middle can
            // never shift the alignment between a serial number and its notes.
            // ConditionNotes is also coalesced to "" instead of null, since the
            // LeaseUnits.ConditionNotes column does not allow NULL (this was the
            // cause of the SqlException / DbUpdateException on save).
            var rawSerials = model.SerialNumbers ?? new List<string>();
            var rawNotes = model.ConditionNotes ?? new List<string>();

            var units = new List<(string SerialNumber, string ConditionNotes)>();

            for (int i = 0; i < rawSerials.Count; i++)
            {
                var serial = rawSerials[i]?.Trim();
                if (string.IsNullOrWhiteSpace(serial))
                    continue; // drop unfilled "+ Add another unit" rows

                var notes = (i < rawNotes.Count ? rawNotes[i] : null) ?? "";
                units.Add((serial, notes.Trim()));
            }

            if (!units.Any())
            {
                TempData["Error"] = "Add at least one serial number for this product.";
                return View("AddLeasableProductPage");
            }

            var leaseProduct = new LeaseProduct
            {
                OwnerId = currentId,
                LeaseProdName = model.LeaseProdName,
                LeaseProdCategory = model.LeaseProdCategory,
                LeaseProdDescription = model.LeaseProdDescription,
                LeaseProdSize = model.LeaseProdSize,
                MonthlyRate = model.MonthlyRate,
                SecurityDeposit = model.SecurityDeposit,

                LateFeeAmount = model.LateFeeAmount,
                EarlyReturnFeePercentage = model.EarlyReturnFeePercentage,
                MinLeaseMonths = model.MinLeaseMonths,
                MaxLeaseMonths = model.MaxLeaseMonths,
                LeaseUnits = new List<LeaseUnit>(),
                LeaseProductPictures = new List<LeaseProductPicture>()
            };

            foreach (var unit in units)
            {
                leaseProduct.LeaseUnits.Add(new LeaseUnit
                {
                    SerialNumber = unit.SerialNumber,
                    ConditionNotes = "Test Value", // guaranteed non-null right here
                    Status = LeaseUnitStatus.Available
                });
            }

            if (model.ProductPicture != null && model.ProductPicture.Length > 0)
            {
                var pictureFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "LeaseProductPicture");
                Directory.CreateDirectory(pictureFolder);

                var fileName = $"{currentId}_{Guid.NewGuid()}{Path.GetExtension(model.ProductPicture.FileName)}";
                var filePath = Path.Combine(pictureFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    model.ProductPicture.CopyTo(stream);
                }

                leaseProduct.LeaseProductPictures.Add(new LeaseProductPicture
                {
                    OwnerId = currentId,
                    FilePath = $"/Uploads/LeaseProductPicture/{fileName}",
                    FileName = Path.GetFileName(model.ProductPicture.FileName),
                    FileType = model.ProductPicture.ContentType,
                    SavedOn = DateTime.Now
                });
            }

            foreach (var u in leaseProduct.LeaseUnits)
                System.Diagnostics.Debug.WriteLine($"Serial={u.SerialNumber} Notes='{u.ConditionNotes}' IsNull={u.ConditionNotes == null}");

            db.LeaseProducts.Add(leaseProduct);
            db.SaveChanges();

            TempData["Message"] = "Leasable product added successfully!";
            return RedirectToAction("AddLeasableProductPage", "ShopOwner");
        }

        [HttpGet]
        public IActionResult ViewLeasableProducts()
        {
            int currentId = HttpContext.Session.GetInt32("UserId")!.Value;

            var products = db.LeaseProducts
                .Include(p => p.LeaseUnits)
                .Include(p => p.LeaseProductPictures)
                .Where(p => p.OwnerId == currentId)
                .ToList();

            return View(products);
        }


        [HttpGet]
        public IActionResult ShopOwnerLeaseAgreements()
        {
            int currentId = HttpContext.Session.GetInt32("UserId")!.Value;

            var agreements = db.LeaseAgreements
                .Include(a => a.Customer)
                .Include(a => a.Items)
                    .ThenInclude(i => i.LeaseUnit)
                        .ThenInclude(u => u.LeaseProduct)
                            .ThenInclude(p => p.LeaseProductPictures)
                .Where(a => a.Items.Any(i => i.LeaseUnit.LeaseProduct.OwnerId == currentId))
                .OrderByDescending(a => a.StartDate)
                .ToList();

            return View(agreements);
        }


        // ── Replace the five return-related actions in ShopOwnerController.cs with
        // these. Only change: every query/service call now scopes to this shop
        // owner's own agreements via currentId, matching the OwnerId == currentId
        // pattern already used everywhere else in this controller (DeliveryOrders,
        // TrackStockLevelPage, etc.). ──

        [HttpGet]
        public async Task<IActionResult> PendingReturns()
        {
            int currentId = HttpContext.Session.GetInt32("UserId")!.Value;

            var items = await db.LeaseAgreementItems
                .Include(i => i.LeaseAgreement)
                    .ThenInclude(a => a.Customer)
                .Include(i => i.LeaseUnit)
                    .ThenInclude(u => u.LeaseProduct)
                .Where(i => i.LeaseAgreement.OwnerId == currentId
                         && (i.ReturnStatus == LeaseReturnStatus.Return_Pending
                             || i.ReturnStatus == LeaseReturnStatus.Item_Collected))
                .OrderBy(i => i.ScheduledCollectionDate)
                .ToListAsync();

            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogReturnedEquipment(int leaseAgreementItemId)
        {
            int currentId = HttpContext.Session.GetInt32("UserId")!.Value;

            var (success, error) = await leaseReturnService.LogReturnedAsync(leaseAgreementItemId, currentId);
            if (!success) TempData["Error"] = error;
            return RedirectToAction("PendingReturns");
        }

        [HttpGet]
        public async Task<IActionResult> InspectReturn(int leaseAgreementItemId)
        {
            int currentId = HttpContext.Session.GetInt32("UserId")!.Value;

            var item = await db.LeaseAgreementItems
                .Include(i => i.LeaseAgreement)
                .Include(i => i.LeaseUnit)
                    .ThenInclude(u => u.LeaseProduct)
                        .ThenInclude(p => p.LeaseProductPictures)
                .FirstOrDefaultAsync(i => i.LeaseAgreementItemId == leaseAgreementItemId
                                       && i.LeaseAgreement.OwnerId == currentId);

            if (item == null) return NotFound();
            return View(item);
        }
        // RunInspection removed — the customer now uploads the condition photo and
        // the AI assessment runs when they schedule the return (ScheduleReturnAsync).
        // InspectReturn is now purely a review-and-confirm screen.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmInspection(int leaseAgreementItemId, double finalDamageFee, string overrideReason)
        {
            int currentId = HttpContext.Session.GetInt32("UserId")!.Value;

            var (success, error) = await leaseReturnService.ConfirmInspectionAsync(leaseAgreementItemId, currentId, finalDamageFee, overrideReason);
            if (!success) TempData["Error"] = error;
            else TempData["Message"] = "Inspection confirmed.";

            return RedirectToAction("PendingReturns");
        }
    }
}
