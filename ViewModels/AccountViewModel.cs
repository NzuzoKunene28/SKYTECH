using SkyTech.Models;
using SkyTech.Models.Documents;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SkyTech.ViewModels
{
    public class DriverTasksViewModel
    {
        public List<Order> Deliveries { get; set; }
        public List<OrderItem> Collections { get; set; }
        public List<OrderItem> Redeliveries { get; set; }
    }

    public class LogInViewModel
    {
        [Required(ErrorMessage = "Please enter your username")]
        [EmailAddress(ErrorMessage ="Please enter a valid email address!")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Please enter your password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
    public class CustomerViewModel
    {
        [Required, MaxLength(100)] public string FirstName { get; set; }

        [Required, MaxLength(100)] public string LastName { get; set; }

        [Required, EmailAddress] [StringLength(255)] public string Email { get; set; }
        [Required, StringLength(10)] public string PhoneNumber { get; set; }

        [MaxLength(200)] public string Address { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 8, ErrorMessage ="Password must be atleast 8 characters long")]
        [RegularExpression(@".{8,}")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [NotMapped]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

    }


        public class TechnicianViewModel
        {
            [Required, MaxLength(100)] public string FirstName { get; set; }
            [Required, MaxLength(100)] public string LastName { get; set; }

            [Required, EmailAddress][StringLength(255)] public string Email { get; set; }


            //[Required(ErrorMessage = "Please select a specialization")]
            [Range(1, int.MaxValue, ErrorMessage = "Please select a specialization")]
            [Display(Name = "Specialization")]
            public int CategoryId { get; set; }

        // Populated by the controller — not submitted, just for rendering the <select>
        [BindNever]
        [ValidateNever]
        public IEnumerable<SelectListItem> Categories { get; set; }  // ← new

            

            [Required, StringLength(10)] public string PhoneNumber { get; set; }
            [MaxLength(200)] public string Address { get; set; }

            [Required]
            [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Required]
            [Compare("Password", ErrorMessage = "Passwords do not match")]
            [DataType(DataType.Password)]
            public string ConfirmPassword { get; set; }
        }

    


    public class ShopOwnerViewModel
    {
        [Required, MaxLength(50)] public string Names { get; set; }
        [Required, MaxLength(50)] public string Surname { get; set; }
        [Required, EmailAddress] public string Email { get; set; }
        [Required, MaxLength(200)] public string ResidentialAddress { get; set; }
        [Required, StringLength(13, MinimumLength = 13)] public string IdBookNumber { get; set; }
        [Required, MaxLength(100)] public string AccountNumber { get; set; }
        [Required, MaxLength(50)] public string BankType { get; set; }
        [MaxLength(200)] public string ShopAddress { get; set; }
        [MaxLength(200)] public string ShopName { get; set; }
       
        // File uploads
        public IFormFile OwnerIdentityBook { get; set; }
        public IFormFile BusRegCertificate { get; set; }

        [Required, DataType(DataType.Password)] public string Password { get; set; }
        [Required, Compare("Password"), DataType(DataType.Password)] public string ConfirmPassword { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class DeliveryDriverViewModel
    {

        [EmailAddress]  [Required(ErrorMessage ="Please enter email")] public string Email { get; set; }

        [Required(ErrorMessage ="Name is required")] public string DriverName { get; set; }

        [Required(ErrorMessage ="Surname is required")]public string DriverSurname { get; set; }

        [Required(ErrorMessage = "Phone number is Required")] public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Residential adress is required")] public string ResidentialAddress { get; set; }
        [Required(ErrorMessage = "Licence number is required")] public string LicenceNumber { get; set; } 
        
        
        
  [Required(ErrorMessage = "Enter your vehicle type")]  public string VehicleType { get; set; } 
        [Required(ErrorMessage = "Vehicle number is required")] public string VehicleNumber { get; set; }

        [Required(ErrorMessage = "Account number is required")] public string AccountNumber { get; set; }


        [Required(ErrorMessage = "Enter bank type")] public string BankType { get; set; }
       
        [Required(ErrorMessage ="Passwword is required")]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; }


        [Required(ErrorMessage ="Confirm password is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

          //driver file uploads
      public IFormFile DriverIdentityBook { get; set; }
      public IFormFile DriversLicence { get; set; }
     
    }
    
    public class StaffViewModel
    {
        
        [Required(ErrorMessage ="First name is required")] public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]public string LastName { get; set; }

        [Required(ErrorMessage = "Email address is required")]public string Email { get; set; }

        [Required(ErrorMessage = "Phone number address is required")]public string PhoneNumber { get; set; }

        //DIGITAL ACCOUNT SET-UP
        [Required(ErrorMessage = "Password is required")] public string Password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required")]
        [DisplayName("Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
        public string UserType { get; set; }

        public DateTime RegisteredOn { get; set; }
    }

    public class AddProductViewModel
    {
       
        [Required(ErrorMessage ="Product name is required")]
        public string ProdName { get; set; }


        [Required(ErrorMessage = "Product category is required")]

        public SelectCategory ProdCategory { get; set; }
        public SelectType ProdType { get; set; }

        [Required(ErrorMessage = "Product description is required")]
        public string ProdDescription { get; set; }

        [Required(ErrorMessage ="Product price is required")]
        public double ProdPrice { get; set; }

        [Required(ErrorMessage ="Product size is required")]
        public string ProdSize { get; set; } 

        [Required(ErrorMessage ="Product quantity is required")]
        public int ProdQuantity { get; set; }

        [Required(ErrorMessage = "Picture of the product is required")]
      public IFormFile ProductPicture { get; set; }

  
    }


    public class ProposeItemViewModel()
    {
        [Required]
        public string ProdName { get; set; }

        [Required]
        public SelectCategory ProdCategory { get; set; }

        [Required]
        public SelectCondition ProdCondition { get; set; }

        [Required]
        [StringLength(500)]
        public string ProdDescription { get; set; }
        public virtual ICollection<ReceiptPic> ReceiptPics { get; set; } = new List<ReceiptPic>();
        public virtual ICollection<ProposedProdPic> ProposedProdPics { get; set; } = new List<ProposedProdPic>();

        // File uploads
        public List<IFormFile> ReceiptPic { get; set; }
        public List<IFormFile> ProposedProdPic { get; set; }
    }

    public class ProposalCheckoutViewModel
    {
        public int PropItemId { get; set; }
        public string ProdName { get; set; }        // read-only display
        public double AmountProposed { get; set; }  // read-only display

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Enter a valid phone number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Street address is required")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Please select a delivery option")]
        public Delivery SelectDelivery { get; set; }  // ← add this

        [Required(ErrorMessage = "Bank name is required")]
        public string BankName { get; set; }

        [Required]
        public string BankCode { get; set; }   // ← ADD THIS

        [Required(ErrorMessage = "Account holder name is required")]
        public string AccountHolderName { get; set; }

        [Required(ErrorMessage = "Account number is required")]
        public string AccountNumber { get; set; }

        [Required(ErrorMessage = "Branch code is required")]
        public string BranchCode { get; set; }

        public AccountType AccountType { get; set; }
    }
}
