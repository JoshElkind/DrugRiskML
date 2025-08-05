using DrugRiskAPI.DTOs;

namespace DrugRiskAPI.Services
{
    public interface ITableauService
    {
        Task<TableauEmbedUrl> GetEmbedUrlAsync(string dashboardName, Dictionary<string, string>? filters = null);
        Task<TableauEmbedUrl> GetUserSpecificAnalyticsAsync(string drugName, decimal userRiskScore, string userRiskLevel, string? userId = null);
        Task<string> GetTableauTokenAsync();
        Task<bool> ValidateTokenAsync(string token);
        Task<List<string>> GetAvailableDashboardsAsync();
    }
} 