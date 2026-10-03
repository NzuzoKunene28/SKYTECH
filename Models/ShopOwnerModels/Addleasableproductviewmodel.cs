using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SkyTech.ViewModels
{
    public class AddLeasableProductViewModel
    {
        [Required] public string LeaseProdName { get; set; }
        [Required] public SkyTech.Models.SelectCategory LeaseProdCategory { get; set; }
        [Required] public string LeaseProdDescription { get; set; }
        public string LeaseProdSize { get; set; }

        [Required] public double MonthlyRate { get; set; }
        [Required] public double SecurityDeposit { get; set; }

        
        [Required, Range(0, double.MaxValue)]
        public double LateFeeAmount { get; set; }
        [Required, Range(0, 100)]
        public double EarlyReturnFeePercentage { get; set; } = 5;


        [Required] public int MinLeaseMonths { get; set; }
        [Required] public int MaxLeaseMonths { get; set; }




        // One row per serial number added on the form (name="SerialNumbers[0]",
        // "SerialNumbers[1]", ... — model binder collects these into the list).
        // Quantity is just this list's count, no separate field needed.
        public List<string> SerialNumbers { get; set; } = new List<string>();

        // Optional, index-aligned with SerialNumbers (row i's condition note).
        public List<string> ConditionNotes { get; set; } = new List<string>();

        public IFormFile ProductPicture { get; set; }
    }
}