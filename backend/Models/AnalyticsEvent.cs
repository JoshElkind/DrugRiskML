using System.ComponentModel.DataAnnotations;

namespace DrugRiskAPI.Models
{
    public class AnalyticsEvent
    {
        public int Id { get; set; }
        public int UserRunId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string EventData { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual UserRun UserRun { get; set; } = null!;
    }
} 