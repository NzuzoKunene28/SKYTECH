using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace SkyTech.Models.SkytechWorkers
{
    public class Role
    {
        [Key]
        public int RoleId {  get; set; }

        public string RoleName { get; set; }
        [ValidateNever]

        public virtual ICollection<Staff> staffs { get; set; }
    }

}
