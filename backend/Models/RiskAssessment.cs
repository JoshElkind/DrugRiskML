using System.ComponentModel.DataAnnotations;

namespace DrugRiskAPI.Models
{
    public class RiskAssessment
    {
        public int Id { get; set; }
        public int UserRunId { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public decimal RiskScore { get; set; }
        public decimal Confidence { get; set; }
        public int VariantCount { get; set; }
        public int HighRiskVariants { get; set; }
        public string ClinicalEvidence { get; set; } = string.Empty;
        public string Recommendations { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual UserRun UserRun { get; set; } = null!;
    }
} 