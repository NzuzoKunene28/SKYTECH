using SkyTech.Models;
using SkyTech.Models.Documents;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace SkyTech.Models.Repair_Item_Services
{
    public enum RepairStatus { Pending, Assigned, Qouted, InProgress, Completed, Cancelled }

    public class Repair_Request
    {
        [Key]
        public int RequestId { get; set; }


        [ForeignKey("CustomerId")]
        public int? CustomerId { get; set; }
        public virtual Customer Customers { get; set; }

        public string ItemName { get; set; }
        public string ItemDescription { get; set; }
        public string IssueDescription { get; set; }
        public DateTime DateCreated { get; set; }

        public RepairStatus Status { get; set; }


        [ForeignKey("CategoryId")]
        
        public int? CategoryId { get; set; }
        public virtual Repair_Item_Category Category { get; set; }

    }
}
