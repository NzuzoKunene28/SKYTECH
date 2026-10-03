using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkyTech.Models.Repair_Services;

namespace SkyTech.Models.Repair_Item_Services
{
    public class RepairAssignment
    {
        [Key]
        public int AssignmentId { get; set; }

        [ForeignKey("RepairRequestId")]
        public int RepairRequestId { get; set; }
        public virtual Repair_Request RepairRequest { get; set; }

        [ForeignKey("TechnicianId")]
        public int TechnicianId { get; set; }
        public virtual Technician Technician { get; set; }

        public DateTime AssignedDate { get; set; }

        public string AssignmentNotes { get; set; }
    }
}
