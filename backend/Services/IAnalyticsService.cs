using DrugRiskAPI.DTOs;

namespace DrugRiskAPI.Services
{
    public interface IAnalyticsService
    {
        Task TrackAnalyticsEventAsync(int userRunId, string eventType, string eventData);
        Task<AnalyticsResponse> GetCommunityAnalyticsAsync(string? drugName = null);
        Task<AnalyticsResponse> GetUserAnalyticsAsync(string userId);
    }
} 