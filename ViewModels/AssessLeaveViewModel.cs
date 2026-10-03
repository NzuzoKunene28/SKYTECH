using System;
using System.Collections.Generic;
using SkyTech.Models.SkytechWorkers;

namespace SkyTech.ViewModels
{
    public class AssessLeaveViewModel
    {
        // ── Existing fields (Pending list + Assess page) ──────────────
        public int LeaveRequestId { get; set; }
        public string StaffName { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public string PositionName { get; set; } = "";
        public string LeaveTypeName { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int WorkingDaysRequested { get; set; }
        public DateTime SubmittedAt { get; set; }
        public LeaveRequestStatus Status { get; set; }

        public int CalendarDays => (EndDate.Date - StartDate.Date).Days + 1;

        // ── Request details ───────────────────────────────────────────
        public string? Reason { get; set; }
        public string StaffEmail { get; set; } = "";
        public string StaffPhone { get; set; } = "";
        public DateTime? EmployedSince { get; set; }

        public string? SupportingDocumentUrl { get; set; }
        public string? SupportingDocumentName { get; set; }
        public bool DocumentRequired { get; set; }

        public string? ReviewNote { get; set; }
        public DateTime? ReviewedAt { get; set; }

        // ── Leave balance ─────────────────────────────────────────────
        public bool RequiresBalance { get; set; }
        public bool HasBalance { get; set; }
        public int BalanceAllocated { get; set; }
        public int BalanceUsed { get; set; }
        public int BalanceOtherPending { get; set; }
        public int BalanceAvailable { get; set; }       // before this request
        public int BalanceAfterApproval { get; set; }   // after this request

        // ── Staffing check ────────────────────────────────────────────
        // "Safe" | "Blocked" | "Unknown"
        public string StaffingVerdict { get; set; } = "Unknown";
        public string StaffingMessage { get; set; } = "";

        public bool MinimumConfigured { get; set; }
        public int MinimumStaffRequired { get; set; }
        public int ActiveStaff { get; set; }
        public int AlreadyApprovedPeak { get; set; }
        public int MaximumAllowedOnLeave { get; set; }
        public int MoreCanBeApproved { get; set; }
        public int UnderstaffedDayCount { get; set; }
        public int OverlappingPendingCount { get; set; }

        public List<OverlappingLeaveViewModel> OverlappingApprovedLeaves { get; set; } = new();
        public List<StaffingDayViewModel> StaffingDays { get; set; } = new();
    }

    public class OverlappingLeaveViewModel
    {
        public string StaffName { get; set; } = "";
        public string LeaveTypeName { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class StaffingDayViewModel
    {
        public DateTime Date { get; set; }
        public int OtherOnLeaveCount { get; set; }
        public List<string> OtherOnLeaveNames { get; set; } = new();
        public int RemainingStaff { get; set; }   // if this request is approved
        public bool IsUnderstaffed { get; set; }
    }
}