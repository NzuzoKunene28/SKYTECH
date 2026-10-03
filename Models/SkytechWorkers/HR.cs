using SkyTech.Models.JobPostServices;
using System.ComponentModel.DataAnnotations;

namespace SkyTech.Models.SkytechWorkers
{
    public class HR
    {
        [Key]
        public int HRId {  get; set; }
        public string Firstname { get; set; }
        public string LastName {  get; set; }
        public string Email {  get; set; }
        public string UserType { get; set; }

        public string Password { get; set; }

        public ICollection<JobRoleKnowledge> JobRoleKnowledges { get; set; }
    }
}
