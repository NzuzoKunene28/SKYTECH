using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace SkyTech.Models.SkytechWorkers
{
    public class Branch
    {
        [Key]
        public int BranchId {  get; set; }

        public string BranchName { get; set; }
        public string BranchLocation { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? RadiusMeters { get; set; }

        [ValidateNever]

        public virtual ICollection<Staff> Staffs { get; set; }
    }
}
