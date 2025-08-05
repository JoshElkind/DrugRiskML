using DrugRiskAPI.DTOs;

namespace DrugRiskAPI.Services
{
    public interface IDrugRiskService
    {
        Task<DrugRiskResponse> AssessDrugRiskAsync(DrugRiskRequest request);
        Task<DrugRiskResponse?> GetUserRunAsync(int userRunId);
        Task<List<DrugRiskResponse>> GetUserRunsAsync(string userId);
        Task<AnalyticsResponse> GetCommunityAnalyticsAsync(string? drugName = null);
        Task<AnalyticsResponse> GetUserAnalyticsAsync(string userId);
    }
} 