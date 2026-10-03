
using SkyTech.Models;

namespace SkyTech.ViewModels
{
    public class AssessmentManagementViewModel
    {
        public Assessment Assessment { get; set; } = new Assessment();

        public int QuestionCount { get; set; }
    }
}