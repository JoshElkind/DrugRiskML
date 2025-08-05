using DrugRiskAPI.Models;

namespace DrugRiskAPI.Services
{
    public interface ISnowflakeService
    {
        Task<bool> InitializeTablesAsync();
        Task<bool> SyncUserRunToSnowflakeAsync(UserRun userRun);
        Task<bool> SyncCommunityAnalyticsAsync();
        Task<List<Dictionary<string, object>>> GetCommunityAnalyticsAsync(string? drugName = null);
        Task<Dictionary<string, object>> GetAnalyticsSummaryAsync();
    }
} 