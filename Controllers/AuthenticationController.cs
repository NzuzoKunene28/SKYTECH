using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.Auth;
using SkyTech.Models.Documents;
using SkyTech.Models.Repair_Services;
using SkyTech.Models.SkytechWorkers;
using SkyTech.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;

namespace SkyTech.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly EmailServices _services;// = null;  // ← add this
        
        //Added this namespace, angikazi ngokwanele ukuthi owani
        //But ngoba engu web host environment i think uyena owenza kukhandeke i path ku wwwroot, kungasaveki ku db
        private readonly IWebHostEnvironment _webHostEnvironment; 


        // ✅ Fix it to include email
        public AuthenticationController(ApplicationDbContext context, EmailServices services, IWebHostEnvironment webHostEnvironment)
        {
            db = context;
            _services = services;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Home() => View();

        [HttpGet]
        public IActionResult LogIn() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LogIn(LogInViewModel models)
        {
            if (!ModelState.IsValid) return View(models);

            // ShopOwner
            var shopOwner = db.shopowners
                .FirstOrDefault(d => d.Email == models.Email && d.Password == models.Password);
            if (shopOwner != null)
            {
                HttpContext.Session.SetInt32("UserId", shopOwner.OwnerId);
                HttpContext.Session.SetString("UserType", "ShopOwner");
                return RedirectToAction("ShopOwnerDash", "ShopOwner");
            }

            // Customer
            var cus = db.Customers
                .FirstOrDefault(d => d.Email == models.Email && d.Password == models.Password);
            if (cus != null)
            {
                HttpContext.Session.SetInt32("UserId", cus.CustomerId);
                HttpContext.Session.SetString("UserType", "Customer");

                // Merge guest cart into real cart if there is one
                var guestCart = HttpContext.Session.GetString("GuestCart");
                if (!string.IsNullOrEmpty(guestCart))
                {
                    var guestItems = System.Text.Json.JsonSerializer
                        .Deserialize<List<GuestCartItem>>(guestCart);

                    if (guestItems != null && guestItems.Any())
                    {
                        var cart = db.Carts
                            .Include(c => c.CartItems)
                            .FirstOrDefault(c => c.CustomerId == cus.CustomerId && !c.IsCheckedOut);

                        if (cart == null)
                        {
                            cart = new Cart
                            {
                                CustomerId = cus.CustomerId,
                                DateCreated = DateTime.Now,
                                IsCheckedOut = false,
                                CartItems = new List<CartItem>()
                            };
                            db.Carts.Add(cart);
                            db.SaveChanges();
                        }

                        foreach (var guestItem in guestItems)
                        {
                            var existing = cart.CartItems
                                .FirstOrDefault(ci => ci.ProdId == guestItem.ProdId);

                            if (existing != null)
                                existing.ProdQuantity += guestItem.Quantity;
                            else
                                cart.CartItems.Add(new CartItem
                                {
                                    ProdId = guestItem.ProdId,
                                    ProdName = guestItem.ProdName,
                                    ProdQuantity = guestItem.Quantity,
                                    PriceAtTimeAdded = guestItem.Price
                                });
                        }

                        db.SaveChanges();

                        // Clear guest cart from session after merging
                        HttpContext.Session.Remove("GuestCart");
                    }
                }

                // Redirect to cart if they came from there
                if (TempData["ReturnToCart"] != null)
                    return RedirectToAction("ViewCart", "Customer");

                return RedirectToAction("ShoppingPage", "Customer");
            }

            // SystemAdmin
            var systemAdmin = db.SystemAdmins
                .FirstOrDefault(d => d.Email == models.Email && d.Password == models.Password);
            if (systemAdmin != null)
            {
                HttpContext.Session.SetInt32("UserId", systemAdmin.AdminId);
                HttpContext.Session.SetString("UserType", "SystemAdmin");
                return RedirectToAction("SystemAdminDash", "SystemAdmin");
            }

            // Staff
            //var staff = db.Staffs
            //    .Include(s => s.Positions)
            //    .ThenInclude(p => p.Departments)
            //    .Include(s => s.Branches)
            //    .FirstOrDefault(s => s.Email == models.Email && s.Password == models.Password);
            //if (staff != null)
            //{
            //    HttpContext.Session.SetInt32("UserId", staff.StaffId);
            //    HttpContext.Session.SetString("UserType", "Staff");
            //    return RedirectToAction("StaffDash", "StaffRoster");
            //}
            // Staff
            var staff = db.Staffs
                .Include(s => s.Positions)
                    .ThenInclude(p => p.Departments)
                .Include(s => s.Branches)
                .FirstOrDefault(s => s.Username == models.Email && s.Password == models.Password);
            if (staff != null)
            {
                HttpContext.Session.SetInt32("UserId", staff.StaffId);
                HttpContext.Session.SetString("UserType", "Staff");
                return RedirectToAction("StaffDash", "StaffRoster");
            }


            // Technician
            var technician = db.Technicians
                .FirstOrDefault(n => n.Email == models.Email && n.Password == models.Password);
            if (technician != null)
            {
                HttpContext.Session.SetInt32("UserId", technician.TechnicianId);
                HttpContext.Session.SetString("UserType", "Technician");
                return RedirectToAction("Dashboard", "Technician");
            }

            // Driver
            var driver = db.DeliveryDrivers
                .FirstOrDefault(n => n.Email == models.Email && n.Password == models.Password);
            if (driver != null)
            {
                HttpContext.Session.SetInt32("UserId", driver.DriverId);
                HttpContext.Session.SetString("UserType", "Driver");
                return RedirectToAction("DriverDash", "DeliveryDriver");
            }

            //HR
            var hr = db.HRs
                .FirstOrDefault(n => n.Email == models.Email && n.Password == models.Password);
            if (hr != null)
            {
                HttpContext.Session.SetInt32("UserId", hr.HRId);
                HttpContext.Session.SetString("UserType", "HR");
                return RedirectToAction("HRDash", "HR");
            }

            ModelState.AddModelError("", "Invalid email or password");
            return View(models);

           
           
        }

        [HttpGet]
        public IActionResult CustomerReg() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CustomerReg(CustomerViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (db.Customers.Any(d => d.Email == model.Email) || db.DeliveryDrivers.Any(d => d.Email == model.Email) || db.shopowners.Any(d => d.Email == model.Email) || db.Technicians.Any(d => d.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already exists");
                return View(model);
            }

            var customer = new Customer
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Password = model.Password,
                ConfirmPassword = model.ConfirmPassword,
                Address = model.Address,
                PhoneNumber = model.PhoneNumber,
                UserType = "Customer",
                RegisteredOn = DateTime.Now
            };

            db.Customers.Add(customer);
            db.SaveChanges();

            //new Services().CustomerRegConfirmation(model.Email, model.FirstName);
            _services.CustomerRegConfirmation(model.Email, model.FirstName);
            TempData["Message"] = "Registration successful";
            return RedirectToAction("LogIn");
        }

        [HttpGet]
        public async Task<IActionResult> TechnicianReg()
        {
            var vm = new TechnicianViewModel
            {
                Categories = await db.RepairItemCategories
                .OrderBy(c => c.CategoryName)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.CategoryName
                    })
                    .ToListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TechnicianReg(TechnicianViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                // Repopulate dropdown before returning
                vm.Categories = await db.RepairItemCategories
                    .OrderBy(c => c.CategoryName)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.CategoryName
                    })
                    .ToListAsync();

                return View(vm);
            }

            //if (!ModelState.IsValid)
            //{
            //    // TEMP: see exactly what's failing
            //    var errors = ModelState
            //        .Where(x => x.Value.Errors.Count > 0)
            //        .Select(x => $"{x.Key} (Value: {ModelState[x.Key].RawValue}): {string.Join(", ", x.Value.Errors.Select(e => e.ErrorMessage))}")
            //        .ToList();

            //    TempData["Message"] = $"CategoryId={vm.CategoryId} | {string.Join(" | ", errors)}";

            //    vm.Categories = await db.RepairItemCategories
            //        .OrderBy(c => c.CategoryName)
            //        .Select(c => new SelectListItem
            //        {
            //            Value = c.CategoryId.ToString(),
            //            Text = c.CategoryName
            //        })
            //        .ToListAsync();

            //    return View(vm);
            //}

            // Check email isn't already taken across all user tables
            if (db.Technicians.Any(t => t.Email == vm.Email) ||
                db.Customers.Any(c => c.Email == vm.Email) || db.DeliveryDrivers.Any(d => d.Email == vm.Email) || db.shopowners.Any(d => d.Email == vm.Email))
            {
                ModelState.AddModelError("Email", "Email already exists");

                vm.Categories = await db.RepairItemCategories
                    .OrderBy(c => c.CategoryName)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.CategoryName
                    })
                    .ToListAsync();

                return View(vm);
            }

            var technician = new Technician
            {
                TechnicianFirstName = vm.FirstName,
                TechnicianLastName = vm.LastName,
                Email = vm.Email,
                Password = vm.Password,
                PhoneNumber = vm.PhoneNumber,
                Address = vm.Address,
                CategoryId = vm.CategoryId,
                Status = AvailabilityStatus.Available,
                UserType = "Technician",
                RegisteredOn = DateTime.Now
            };

            db.Technicians.Add(technician);
            await db.SaveChangesAsync();

            _services.CustomerRegConfirmation(vm.Email, vm.FirstName);
            TempData["Message"] = "Registration successful";
            return RedirectToAction("LogIn");
        }


        [HttpGet]
        public IActionResult ShopOwnerReg() => View();



        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult ShopOwnerReg(ShopOwnerViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //        return View(model);

        //    if (db.Customers.Any(d => d.Email == model.Email) || db.DeliveryDrivers.Any(d => d.Email == model.Email) || db.shopowners.Any(d => d.Email == model.Email))
        //    {
        //        ModelState.AddModelError("Email", "Email already exists");
        //        return View(model);
        //    }

        //    // 1. Create the Branch first
        //    var branch = new Branch
        //    {
        //        BranchName = model.ShopName,
        //        BranchLocation = model.ShopAddress
        //        // Latitude, Longitude, RadiusMeters left null for now — can be set later
        //    };
        //    db.Add(branch);
        //    db.SaveChangesAsync(); // must save now so branch.BranchId is generate

        //    var shopOwner = new ShopOwner
        //    {

        //        IdBookNumber = model.IdBookNumber,
        //        Names = model.Names,
        //        Surname = model.Surname,
        //        Email = model.Email,
        //        ResidentialAddress = model.ResidentialAddress,
        //        AccountNumber = model.AccountNumber,
        //        BankType = model.BankType,
        //        //ShopName = model.ShopName,
        //        ShopAddress = model.ShopAddress,
        //        //Shoptype = model.Shoptype,
        //        Password = model.Password,
        //        ApplicationStatus = ApplicationStat.Pending,
        //        UserType = "ShopOwner",
        //        RegisteredOn = DateTime.Now
        //    };

        //    db.shopowners.Add(shopOwner);
        //    db.SaveChanges();

        //    //int currentId = shopOwner.OwnerId;
        //    if (model.OwnerIdentityBook != null && model.OwnerIdentityBook.Length > 0)
        //    {
        //        // using var ms = new MemoryStream();
        //        // model.OwnerIdentityBook.CopyTo(ms);


        //        var idFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "OwnerIdBook"); //Path
        //        Directory.CreateDirectory(idFolder); //this one creates a new folder if it does not exist

        //        // Build a unique filename using the  ID + a random GUID + the original file extension
        //        //this prevents ovewriting the file if users upload the file with the same name
        //        var idFileName = $"{shopOwner.OwnerId}_{Guid.NewGuid()}{Path.GetExtension(model.OwnerIdentityBook.FileName)}";
        //        var idFilePath = Path.Combine(idFolder, idFileName); // Combine the folder path and the filename to get the full path where the file will be saved

        //        using (var stream = new FileStream(idFilePath, FileMode.Create))
        //        {
        //            model.OwnerIdentityBook.CopyTo(stream);
        //        }

        //        db.OwnerIdentityBooks.Add(new OwnerIdentityBook
        //        {
        //            OwnerId = shopOwner.OwnerId,
        //            FileName = model.OwnerIdentityBook.FileName,
        //            FileType = model.OwnerIdentityBook.ContentType,
        //           // FileData = ms.ToArray(), Remove this one
        //             FilePath = $"/Uploads/OwnerIdBook/{idFileName}", //Store relative path
        //            SavedOn = DateTime.Now
        //        });
        //        //db.SaveChanges();
        //    }

        //    if (model.BusRegCertificate != null && model.BusRegCertificate.Length > 0)
        //    {
        //        // using var ms = new MemoryStream();
        //        // model.BusRegCertificate.CopyTo(ms);

        //         var busRegFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "RegCertificate");//path
        //         Directory.CreateDirectory(busRegFolder); //creating new folder if does not exist

        //         var busRegFileName = $"{shopOwner.OwnerId}_{Guid.NewGuid()}{Path.GetExtension(model.BusRegCertificate.FileName)}";
        //         var busRegFilePath = Path.Combine(busRegFolder, busRegFileName);

        //         using (var stream = new FileStream(busRegFilePath, FileMode.Create))
        //         {
        //             model.BusRegCertificate.CopyTo(stream);
        //         }

        //        db.BusRegCertificates.Add(new BusRegCertificate
        //        {
        //            OwnerId = shopOwner.OwnerId,
        //            FileName = model.BusRegCertificate.FileName,
        //            FileType = model.BusRegCertificate.ContentType,
        //           // FileData = ms.ToArray(),
        //           FilePath = $"/Uploads/RegCertificate/{busRegFileName}", //store relative path
        //            SavedOn = DateTime.Now
        //        });
        //        //db.SaveChanges();
        //    }

        //    db.SaveChanges();
        //    _services.CustomerRegConfirmation(model.Email, model.Names);
        //    TempData["Message"] = "Registration successful";
        //    return RedirectToAction("LogIn");
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShopOwnerReg(ShopOwnerViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            if (model.Latitude == null || model.Longitude == null)
            {
                ModelState.AddModelError("ShopAddress", "Please select a valid address from the suggestions before submitting.");
                return View(model);
            }

            if (db.Customers.Any(d => d.Email == model.Email) ||
                db.DeliveryDrivers.Any(d => d.Email == model.Email) ||
                db.shopowners.Any(d => d.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already exists");
                return View(model);
            }

            // ==========================================
            // 1. CREATE BRANCH
            // ==========================================

            var branch = new Branch
            {
                BranchName = model.ShopName,
                BranchLocation = model.ShopAddress,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                RadiusMeters = 150 // default geofence radius — adjustable later via a branch edit screen if you build one
            };

            db.Branches.Add(branch);

            // IMPORTANT: wait for SQL Server to generate BranchId
            await db.SaveChangesAsync();

            // At this point:
            // branch.BranchId contains the real database ID


            // ==========================================
            // 2. CREATE SHOP OWNER
            // ==========================================

            var shopOwner = new ShopOwner
            {
                BranchId = branch.BranchId,   // IMPORTANT
                ShopName = model.ShopName,
                IdBookNumber = model.IdBookNumber,
                Names = model.Names,
                Surname = model.Surname,
                Email = model.Email,
                ResidentialAddress = model.ResidentialAddress,
                AccountNumber = model.AccountNumber,
                BankType = model.BankType,
                ShopAddress = model.ShopAddress,
                Password = model.Password,

                ApplicationStatus = ApplicationStat.Pending,
                UserType = "ShopOwner",
                RegisteredOn = DateTime.Now
            };

            db.shopowners.Add(shopOwner);

            await db.SaveChangesAsync();


            // ==========================================
            // 3. SAVE ID DOCUMENT
            // ==========================================

            if (model.OwnerIdentityBook != null &&
                model.OwnerIdentityBook.Length > 0)
            {
                var idFolder = Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    "Uploads",
                    "OwnerIdBook");

                Directory.CreateDirectory(idFolder);

                var idFileName =
                    $"{shopOwner.OwnerId}_{Guid.NewGuid()}{Path.GetExtension(model.OwnerIdentityBook.FileName)}";

                var idFilePath = Path.Combine(idFolder, idFileName);

                using (var stream = new FileStream(idFilePath, FileMode.Create))
                {
                    await model.OwnerIdentityBook.CopyToAsync(stream);
                }

                db.OwnerIdentityBooks.Add(new OwnerIdentityBook
                {
                    OwnerId = shopOwner.OwnerId,
                    FileName = model.OwnerIdentityBook.FileName,
                    FileType = model.OwnerIdentityBook.ContentType,
                    FilePath = $"/Uploads/OwnerIdBook/{idFileName}",
                    SavedOn = DateTime.Now
                });
            }


            // ==========================================
            // 4. SAVE BUSINESS REGISTRATION
            // ==========================================

            if (model.BusRegCertificate != null &&
                model.BusRegCertificate.Length > 0)
            {
                var busRegFolder = Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    "Uploads",
                    "RegCertificate");

                Directory.CreateDirectory(busRegFolder);

                var busRegFileName =
                    $"{shopOwner.OwnerId}_{Guid.NewGuid()}{Path.GetExtension(model.BusRegCertificate.FileName)}";

                var busRegFilePath = Path.Combine(
                    busRegFolder,
                    busRegFileName);

                using (var stream = new FileStream(busRegFilePath, FileMode.Create))
                {
                    await model.BusRegCertificate.CopyToAsync(stream);
                }

                db.BusRegCertificates.Add(new BusRegCertificate
                {
                    OwnerId = shopOwner.OwnerId,
                    FileName = model.BusRegCertificate.FileName,
                    FileType = model.BusRegCertificate.ContentType,
                    FilePath = $"/Uploads/RegCertificate/{busRegFileName}",
                    SavedOn = DateTime.Now
                });
            }


            // ==========================================
            // 5. SAVE DOCUMENTS
            // ==========================================

            await db.SaveChangesAsync();

            _services.CustomerRegConfirmation(
                model.Email,
                model.Names);

            TempData["Message"] = "Registration successful";

            return RedirectToAction("LogIn");
        }


        [HttpGet]
        public IActionResult DeliveryDriverReg() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeliveryDriverReg(DeliveryDriverViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (db.Customers.Any(d => d.Email == model.Email) || db.DeliveryDrivers.Any(d => d.Email == model.Email) || db.shopowners.Any(d => d.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already exists");
                return View(model);
            }

            var driver = new DeliveryDriver
            {
                Email = model.Email,
                DriverName = model.DriverName,
                DriverSurname = model.DriverSurname,
                PhoneNumber = model.PhoneNumber,
                ResidentialAddress = model.ResidentialAddress,
                LicenceNumber = model.LicenceNumber,
                VehicleType = model.VehicleType,
                VehicleNumber = model.VehicleNumber,
                AccountNumber = model.AccountNumber,
                BankType = model.BankType, //this was missing
                IsAvailable = true,
                ApplicationStatus = Status.Pending,
                Password = model.Password,
                UserType = "Driver",
                RegisteredOn = DateTime.Now
            };

            db.DeliveryDrivers.Add(driver);
            db.SaveChanges();

            //int currentId = driver.DriverId;
                if (model.DriversLicence != null && model.DriversLicence.Length > 0)
            {
                // using var ms = new MemoryStream();
                // DriversLicences.CopyTo(ms);

                 var licenceFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "DrivingLicence"); //Path
                 Directory.CreateDirectory(licenceFolder); // creates folder if it doesn't exist
                
                 var licenceFileName = $"{driver.DriverId}_{Guid.NewGuid()}{Path.GetExtension(model.DriversLicence.FileName)}";
                 var licenceFilePath = Path.Combine(licenceFolder, licenceFileName);
                
                 using (var stream = new FileStream(licenceFilePath, FileMode.Create))
                 {
                     model.DriversLicence.CopyTo(stream);
                 }

                
                db.DriversLicences.Add(new DriversLicence
                {
                    DriverId = driver.DriverId,
                    FileName = model.DriversLicence.FileName,
                    FileType = model.DriversLicence.ContentType,
                  //  FileData = ms.ToArray(),
                   FilePath = $"/Uploads/DrivingLicence/{licenceFileName}", // store relative path
                    SavedOn = DateTime.Now
                });
            }

            if (model.DriverIdentityBook != null && model.DriverIdentityBook.Length > 0)
            {
                // using var ms = new MemoryStream();
                // DriverIdentityBooks.CopyTo(ms);
                
                  var idFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "DriverIdDocument");
                  Directory.CreateDirectory(idFolder);
                
                  var idFileName = $"{driver.DriverId}_{Guid.NewGuid()}{Path.GetExtension(model.DriverIdentityBook.FileName)}";
                  var idFilePath = Path.Combine(idFolder, idFileName);
                
                  using (var stream = new FileStream(idFilePath, FileMode.Create))
                  {
                      model.DriverIdentityBook.CopyTo(stream);
                  }
                
                db.DriverIdentityBooks.Add(new DriverIdentityBook
                {
                    DriverId = driver.DriverId,
                    FileName = model.DriverIdentityBook.FileName,
                    FileType = model.DriverIdentityBook.ContentType,
                   // FileData = ms.ToArray(),
                   FilePath = $"/Uploads/DriverIdDocument/{idFileName}",
                    SavedOn = DateTime.Now
                });
            }

            db.SaveChanges();
            //new Services().DriverRegConfirmation(model.Email, model.DriverName);
            _services.CustomerRegConfirmation(model.Email, model.DriverName);
            TempData["Message"] = "Registration successful";
            return RedirectToAction("LogIn", "Authentication");
        }

        // ── FORGOT PASSWORD ──────────────────────────────────────────────

        [HttpGet]
        public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            bool userExists = db.Customers.Any(d => d.Email == model.UserEmail)
                           || db.Staffs.Any(n => n.Email == model.UserEmail)
                           || db.shopowners.Any(n => n.Email == model.UserEmail)
                           || db.DeliveryDrivers.Any(n => n.Email == model.UserEmail);

            if (!userExists)
            {
                ModelState.AddModelError("UserEmail", "Email not found.");
                return View(model);
            }

            string otp = new Random().Next(100000, 999999).ToString();

            HttpContext.Session.SetString("OTP", otp);
            HttpContext.Session.SetString("ResetEmail", model.UserEmail);
            HttpContext.Session.SetString("OtpExpiry", DateTime.Now.AddMinutes(5).ToString("o"));

            //new Services().SendOtpEmail(model.UserEmail, otp);
            _services.CustomerRegConfirmation(model.UserEmail, otp);
            return RedirectToAction("VerifyOtp");
        }

        [HttpGet]
        public IActionResult VerifyOtp() =>
            View(new VerifyOtpViewModel { UserEmail = HttpContext.Session.GetString("ResetEmail") });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOtp(VerifyOtpViewModel model)
        {
            string sessionOtp = HttpContext.Session.GetString("OTP");
            string expiryStr = HttpContext.Session.GetString("OtpExpiry");
            DateTime expiry = expiryStr != null ? DateTime.Parse(expiryStr) : DateTime.MinValue;

            if (DateTime.Now > expiry)
            {
                ModelState.AddModelError("", "OTP expired. Please request again.");
                return View(model);
            }

            if (model.OtpCode == sessionOtp)
                return RedirectToAction("ResetPassword");

            ModelState.AddModelError("OtpCode", "Invalid OTP.");
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPassword() =>
            View(new ResetPasswordViewModel { UserEmail = HttpContext.Session.GetString("ResetEmail") });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var customer = db.Customers.FirstOrDefault(d => d.Email == model.UserEmail);
            var staff = db.Staffs.FirstOrDefault(n => n.Email == model.UserEmail);
            var driver = db.DeliveryDrivers.FirstOrDefault(n => n.Email == model.UserEmail);
            var shopowner = db.shopowners.FirstOrDefault(n => n.Email == model.UserEmail);

            if (customer != null)
            {
                customer.Password = model.NewPassword;
                customer.ConfirmPassword = model.NewPassword;
            }
            else if (staff != null)
            {
                staff.Password = model.NewPassword;
            }
            else if (driver != null)
            {
                driver.Password = model.NewPassword;
                driver.ConfirmPassword = model.NewPassword;
            }
            else if (shopowner != null)
            {
                shopowner.Password = model.NewPassword;
                shopowner.ConfirmPassword = model.NewPassword;
            }
            else
            {
                ModelState.AddModelError("", "User not found.");
                return View(model);
            }

            db.SaveChanges();

            HttpContext.Session.Remove("OTP");
            HttpContext.Session.Remove("ResetEmail");
            HttpContext.Session.Remove("OtpExpiry");

            TempData["Message"] = "Password reset successful. Please log in.";
            return RedirectToAction("LogIn");
        }

        //Logout action method
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            var userType = HttpContext.Session.GetString("UserType");
            HttpContext.Session.Clear();

            // Customers go to shopping page, everyone else goes to login
            if (userType == "Customer")
                return RedirectToAction("ShoppingPage", "Customer");

            return RedirectToAction("LogIn", "Authentication");
        }
    }
}
