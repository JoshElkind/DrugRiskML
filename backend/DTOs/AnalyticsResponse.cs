namespace DrugRiskAPI.DTOs
{
    public class AnalyticsResponse
    {
        public int TotalAssessments { get; set; }
        public decimal AverageRiskScore { get; set; }
        public Dictionary<string, int> RiskLevelDistribution { get; set; } = new Dictionary<string, int>();
        public List<string> TopDrugs { get; set; } = new List<string>();
        public Dictionary<string, object> CommunityInsights { get; set; } = new Dictionary<string, object>();
        public TableauEmbedUrl? TableauDashboard { get; set; }
    }
} 