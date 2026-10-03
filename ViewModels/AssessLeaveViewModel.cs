using SkyTech.Models.SkytechWorkers;

namespace SkyTech.ViewModels
{
    public class AssessLeaveViewModel //What information does HR need to assess this employee's request?
    {
        // Used on the Pending Requests page
        public int LeaveRequestId { get; set; }//so controller knows which leave is approved or rejected

        public string StaffName { get; set; } = string.Empty;

        public string DepartmentName { get; set; } = string.Empty;

        public string PositionName { get; set; } = string.Empty;

        public string LeaveTypeName { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int WorkingDaysRequested { get; set; }

        public DateTime SubmittedAt { get; set; }

        public LeaveRequestStatus Status { get; set; }


        // Used on the individual Assess page
        public string Reason { get; set; } = string.Empty;

        public string? SupportingDocumentPath { get; set; }

        public string? ReviewNote { get; set; }


        // Department staffing information
        public int TotalActiveStaff { get; set; }

        public int StaffAlreadyOnApprovedLeave { get; set; }

        public int StaffRemainingIfApproved { get; set; }

        public decimal MaximumLeavePercentage { get; set; } //e.g 30% of staff can be on leave at same time

        public int MaximumStaffAllowedOnLeave { get; set; }

        public int AdditionalStaffThatCanBeApproved { get; set; } //maybe 5 are already on leave and one more can be approved for leave
    }
}