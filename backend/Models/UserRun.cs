using System.ComponentModel.DataAnnotations;

namespace DrugRiskAPI.Models
{
    public class UserRun
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty; // Optional - anonymous if not provided
        [Required]
        public string DrugName { get; set; } = string.Empty;
        public decimal RiskScore { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        public virtual ICollection<VcfData> VcfData { get; set; } = new List<VcfData>();
        public virtual RiskAssessment? RiskAssessment { get; set; }
        public virtual ICollection<DrugAlternative> DrugAlternatives { get; set; } = new List<DrugAlternative>();
        public virtual ICollection<AnalyticsEvent> AnalyticsEvents { get; set; } = new List<AnalyticsEvent>();
    }
} 