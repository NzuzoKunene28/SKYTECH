using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SkyTech.Models;
//using SkyTech.Models.Test.Assessment;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
//using RecruitmentSystem.Models;


namespace SkyTech.Models.JobPostServices
{
    public enum JobApplicationStat
    {
        Applied,
        Shortlisted,
        Passed_Test,
        Failed_Test,
        Online_interview, 
        Physical_Interview,
        Rejected

    }
    public class ApplicationForm
    {
        [Key]
        public int ApplicationId { get; set;  }

        [ForeignKey(nameof(JobPosts))]
        public int PostId { get; set; }

        [ValidateNever]
        public virtual JobPost JobPosts { get; set; }
        //  public virtual JobPost Post { get; set; }

        [ForeignKey(nameof(Customers))]
        public int CustomerId { get; set; }
        [ValidateNever]
        public virtual Customer Customers { get; set; }

        [ForeignKey(nameof(Assessments))]
        public int? AssessmentId { get; set; }
        [ValidateNever]
        public virtual Assessment Assessments { get; set; }

        //[ForeignKey(nameof(TestResults))]
        //public int? ResultId { get; set; }
        [ValidateNever]
        public virtual TestResult TestResults { get; set; }



        
        public string FirstName { get; set; }
        //public string SecondName { get; set; }
        public string LastName { get; set; }
        [Required, MaxLength(15)]
        public string PhoneNumber { get; set; }

        public string IDNumber {  get; set; }

        public string Email {  get; set; }
        public string Location { get; set; }
        public string Experience {  get; set; }
        public string Skills { get; set; }
        public string Certificates { get; set; }
        public string Links { get; set;  }

        public bool HasInternertAccess { get; set; } 
        public DateTime AppliedAt {  get; set; } =DateTime.Now;



        public bool TestWritten { get; set; } = false;
        public int? TestScore { get; set; }
 


        public JobApplicationStat JobStat { get; set; }
        [ValidateNever]
        //============THESE PROPS ARE INGERITED FROM THE JOBPOST TABLE=========
        public string JobTitle { get; set; }
        [ValidateNever]

          public SelectDepartment Department { get; set; }
          [ValidateNever]
  
        public SelectEmploymentType EmploymentType { get; set; }
        [ValidateNever]

        public double? SalaryMin { get; set; }
        [ValidateNever]

        public double? SalaryMax { get; set; }
        public int MatchedSkillCount { get; set; }



    }
}
