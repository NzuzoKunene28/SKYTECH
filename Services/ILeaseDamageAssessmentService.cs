using SkyTech.Models;
using System.Threading.Tasks;

namespace SkyTech.Services
{
    public class LeaseDamageAssessmentResult
    {

        public ReturnConditionAssessment Condition { get; set; }
        public int ConfidencePercent { get; set; }
        public double SuggestedDamageFee { get; set; }
        public string Notes { get; set; } = "";
    }

    public interface ILeaseDamageAssessmentService
    {
        // imageBytes/imageContentType: the uploaded condition photo.
        // itemName / securityDeposit: context so the model can reason about
        // what a "reasonable" damage fee looks like relative to the deposit.
        Task<LeaseDamageAssessmentResult> AnalyzeAsync(byte[] imageBytes, string imageContentType, string itemName, double securityDeposit);
    }

}
