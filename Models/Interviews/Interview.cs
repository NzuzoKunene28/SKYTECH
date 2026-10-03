using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models.Interviews
{
    public enum InterviewStatus
    {
        Scheduled,
        Completed,
        Pre_Approved,
        Unsuccessful,
        Employed
    }

    public class Interview
    {
        [Key]
        public int InterviewId { get; set; }

        [ForeignKey(nameof(Customer))]
        public int CustomerId { get; set; }
        public virtual Customer Customer { get; set; }

        [ForeignKey(nameof(TestResult))]
        public int ResultId { get; set; }

        [ValidateNever]
        public virtual TestResult TestResult { get; set; }

        public DateTime ScheduledDate { get; set; }

        [MaxLength(300)]
        public string JitsiMeetingLink { get; set; }

        [MaxLength(1000)]
        public string EvaluationNotes { get; set; }

        public int? EvaluationScore { get; set; }
        public int Salary { get; set; }

        [Required]
        public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;

        public DateTime? EvaluatedOn { get; set; }

        [MaxLength(100)]
        public string ReviewedByHR { get; set; }
    }
}