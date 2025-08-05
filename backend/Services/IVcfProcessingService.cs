using DrugRiskAPI.Models;

namespace DrugRiskAPI.Services
{
    public interface IVcfProcessingService
    {
        Task<List<VcfData>> ProcessVcfContentAsync(string vcfContent, int userRunId);
        Task<List<VcfData>> ExtractRelevantVariantsAsync(List<VcfData> variants, string drugName);
    }
} 