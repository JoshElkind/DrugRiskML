namespace DrugRiskAPI.DTOs
{
    public class TableauEmbedUrl
    {
        public string Url { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public Dictionary<string, object>? UserContext { get; set; }
    }
} 