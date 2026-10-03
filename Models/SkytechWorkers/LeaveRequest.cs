using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models.SkytechWorkers
{
    public enum LeaveRequestStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class LeaveRequest
    {
        [Key]
        public int LeaveRequestId { get; set; }

        //Foreign Key connecting a reuest to the employee who requested it

        [ForeignKey(nameof(Staff))]
        public int StaffId { get; set; }

        //Navigation property to navigate from the reuest to the staff so you can see who reuested it

        [ValidateNever]
        public virtual Staff Staff { get; set; }


        [ForeignKey(nameof(LeaveType))]
        public int LeaveTypeId { get; set; }

        [ValidateNever]
        public virtual LeaveType LeaveType { get; set; }


        [Required]
        [MaxLength(500)]
        public string Reason { get; set; }


        [Required]
        public DateTime StartDate { get; set; }


        [Required]
        public DateTime EndDate { get; set; }

        //system calcs working days requested and stored here
        public int WorkingDaysRequested { get; set; }

        //stores where document is uploaded not actual document
        //it can be null cos not every leave type requires a document

        public string? SupportingDocumentPath { get; set; }

        //Eevery new leave request will start at pending
        public LeaveRequestStatus Status { get; set; }
            = LeaveRequestStatus.Pending;


        public DateTime SubmittedAt { get; set; }
            = DateTime.Now;


        // =====================================================
        // HR ASSESSMENT INFORMATION
        // =====================================================

        [MaxLength(500)]
        public string? ReviewNote { get; set; }


        public DateTime? ReviewedAt { get; set; }
    }
}