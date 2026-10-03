using SkyTech.Models.Documents;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SkyTech.Models.Repair_Services;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models.Repair_Item_Services
{
    public class Repair_Item_Category
    {
        [Key]
        public int CategoryId { get; set; }

     
        public string CategoryName { get; set; }
        public ICollection<Repair_Request> RepairRequests { get; set; }
        public ICollection<Technician> Technicians { get; set; }
    }
}
