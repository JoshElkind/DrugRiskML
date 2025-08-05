using DrugRiskAPI.Models;

namespace DrugRiskAPI.Services
{
    public interface IMlPredictionService
    {
        Task<RiskAssessment> PredictRiskAsync(List<VcfData> variants, string drugName);
        Task<List<DrugAlternative>> GenerateAlternativesAsync(RiskAssessment assessment, string drugName);
    }
} 