using Microsoft.AspNetCore.Http; //need it for IFile which represents doc uploaded
using Microsoft.AspNetCore.Mvc.Rendering; //need it for selectlistitem
using System.ComponentModel.DataAnnotations;

namespace SkyTech.ViewModels
{
    public class RequestLeaveViewModel //what the employee sees when requesting leave
    {
        // ===== INFORMATION DISPLAYED TO THE EMPLOYEE =====

        public string StaffName { get; set; } = string.Empty;

        public List<SelectListItem> LeaveTypes { get; set; } = new();

        public List<LeaveBalanceSummaryViewModel> LeaveBalances { get; set; } = new();

        // ===== INFORMATION ENTERED BY THE EMPLOYEE =====

        [Required(ErrorMessage = "Please select a leave type.")]
        public int LeaveTypeId { get; set; }

        [Required(ErrorMessage = "Please provide a reason for your leave.")]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        //the actual doc uploaded
        public IFormFile? SupportingDocument { get; set; }


        // ===== CALCULATED BY THE SYSTEM =====

        public int? WorkingDaysRequested { get; set; }

        public int? AvailableDays { get; set; }

        public int? RemainingDaysAfterRequest { get; set; }
    }
}