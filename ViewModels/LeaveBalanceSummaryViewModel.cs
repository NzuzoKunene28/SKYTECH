//Give the Request Leave page a simple summary of each balance.
public class LeaveBalanceSummaryViewModel
{
    public int LeaveTypeId { get; set; } //which leave type this balance represents sick, family etc

    public string LeaveTypeName { get; set; } = string.Empty;

    public int TotalAllocatedDays { get; set; } //original allocation given to that leave type 

    public int UsedDays { get; set; }

    public int PendingDays { get; set; } //you have 15 allocated days, 10 used days which means 5 available. you submit request for 4 days which means available that you can request for is 1 since the 4 is pending

    public int AvailableDays { get; set; }
}