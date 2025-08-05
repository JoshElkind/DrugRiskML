using System.ComponentModel.DataAnnotations;

namespace DrugRiskAPI.Models
{
    public class DrugAlternative
    {
        public int Id { get; set; }
        public int UserRunId { get; set; }
        public string AlternativeDrug { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public decimal ConfidenceScore { get; set; }
        public string ClinicalEvidence { get; set; } = string.Empty;
        public string DosageRecommendation { get; set; } = string.Empty;
        public string MonitoringRequirements { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual UserRun UserRun { get; set; } = null!;
    }
} 