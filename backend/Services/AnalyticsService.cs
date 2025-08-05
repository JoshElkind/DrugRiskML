using DrugRiskAPI.Data;
using DrugRiskAPI.DTOs;
using DrugRiskAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DrugRiskAPI.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly DrugRiskContext _context;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(DrugRiskContext context, ILogger<AnalyticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task TrackAnalyticsEventAsync(int userRunId, string eventType, string eventData)
        {
            var analyticsEvent = new AnalyticsEvent
            {
                UserRunId = userRunId,
                EventType = eventType,
                EventData = eventData,
                CreatedAt = DateTime.UtcNow
            };

            _context.AnalyticsEvents.Add(analyticsEvent);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Tracked analytics event: {eventType} for user run {userRunId}");
        }

        public async Task<AnalyticsResponse> GetCommunityAnalyticsAsync(string? drugName = null)
        {
            var query = _context.UserRuns.AsQueryable();

            if (!string.IsNullOrEmpty(drugName))
            {
                query = query.Where(u => u.DrugName == drugName);
            }

            var totalAssessments = await query.CountAsync();
            var averageRiskScore = await query.AverageAsync(u => u.RiskScore);
            var riskLevelDistribution = await query
                .GroupBy(u => u.RiskLevel)
                .Select(g => new { RiskLevel = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RiskLevel, x => x.Count);

            var topDrugs = await query
                .GroupBy(u => u.DrugName)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToListAsync();

            var communityInsights = new Dictionary<string, object>
            {
                ["totalAssessments"] = totalAssessments,
                ["averageRiskScore"] = averageRiskScore,
                ["riskLevelDistribution"] = riskLevelDistribution,
                ["topDrugs"] = topDrugs
            };

            return new AnalyticsResponse
            {
                TotalAssessments = totalAssessments,
                AverageRiskScore = averageRiskScore,
                RiskLevelDistribution = riskLevelDistribution,
                TopDrugs = topDrugs,
                CommunityInsights = communityInsights
            };
        }

        public async Task<AnalyticsResponse> GetUserAnalyticsAsync(string userId)
        {
            var userRuns = await _context.UserRuns
                .Where(u => u.UserId == userId)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            if (!userRuns.Any())
            {
                return new AnalyticsResponse
                {
                    TotalAssessments = 0,
                    AverageRiskScore = 0,
                    RiskLevelDistribution = new Dictionary<string, int>(),
                    TopDrugs = new List<string>(),
                    CommunityInsights = new Dictionary<string, object>()
                };
            }

            var totalAssessments = userRuns.Count;
            var averageRiskScore = userRuns.Average(u => u.RiskScore);
            var riskLevelDistribution = userRuns
                .GroupBy(u => u.RiskLevel)
                .ToDictionary(g => g.Key, g => g.Count());

            var topDrugs = userRuns
                .GroupBy(u => u.DrugName)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList();

            var userInsights = new Dictionary<string, object>
            {
                ["totalAssessments"] = totalAssessments,
                ["averageRiskScore"] = averageRiskScore,
                ["riskLevelDistribution"] = riskLevelDistribution,
                ["topDrugs"] = topDrugs,
                ["lastAssessment"] = userRuns.First().CreatedAt
            };

            return new AnalyticsResponse
            {
                TotalAssessments = totalAssessments,
                AverageRiskScore = averageRiskScore,
                RiskLevelDistribution = riskLevelDistribution,
                TopDrugs = topDrugs,
                CommunityInsights = userInsights
            };
        }
    }
} 