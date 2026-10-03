using SkyTech.Models.Documents;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models
{
    public enum LeaseUnitStatus
    {
        Available,    // 0
        Reserved,     // 1
        OnLease,      // 2
        Maintenance,  // 3
        Retired       // 4
    }



    public class LeaseProduct
    {
        [Key]
        public int LeaseProdId { get; set; }

        public int OwnerId { get; set; }
        [ForeignKey("OwnerId")]
        public virtual ShopOwner Owner { get; set; }

        [Required] public string LeaseProdName { get; set; }
        [Required] public SelectCategory LeaseProdCategory { get; set; }
        [Required] public string LeaseProdDescription { get; set; }
        [Required] public string LeaseProdSize { get; set; }

        [Required] public double MonthlyRate { get; set; }
        [Required] public double SecurityDeposit { get; set; }

        [Required] public int MinLeaseMonths { get; set; }
        [Required] public int MaxLeaseMonths { get; set; }


        [Required] 
        public double EarlyReturnFeePercentage { get; set; } = 5;


        // Configurable per product, per your call on this. Added to a
        // Recurring LeasePayment's AmountDue once it's 5+ days overdue.
        [Required] public double LateFeeAmount { get; set; }

        // Quantity is implicit: LeaseUnits.Count. Each unit is a distinct
        // physical, serialized item — no separate quantity field needed.
        public virtual ICollection<LeaseUnit> LeaseUnits { get; set; } = new List<LeaseUnit>();

        public virtual ICollection<LeaseProductPicture> LeaseProductPictures { get; set; } = new List<LeaseProductPicture>();
    }



    public class LeaseUnit
    {
        [Key]
        public int LeaseUnitId { get; set; }

        public int LeaseProdId { get; set; }
        [ForeignKey("LeaseProdId")]
        public virtual LeaseProduct LeaseProduct { get; set; }

        [Required] 
        public string SerialNumber { get; set; }
        
        public string ConditionNotes { get; set; }

        public LeaseUnitStatus Status { get; set; } = LeaseUnitStatus.Available;
    }



    // Mirrors ProductPicture's actual fields, confirmed from ShopOwnerController's
    // AddProductPage upload logic.
    public class LeaseProductPicture
    {
        [Key]
        public int LeasePictureId { get; set; }

        public int LeaseProdId { get; set; }
        [ForeignKey("LeaseProdId")]
        public virtual LeaseProduct LeaseProduct { get; set; }

        public int OwnerId { get; set; }
        [Required] public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public DateTime SavedOn { get; set; }
    }
}