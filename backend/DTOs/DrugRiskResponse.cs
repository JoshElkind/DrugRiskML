namespace DrugRiskAPI.DTOs
{
    public class DrugRiskResponse
    {
        public int UserRunId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string DrugName { get; set; } = string.Empty;
        public decimal RiskScore { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public List<DrugAlternativeDto> DrugAlternatives { get; set; } = new List<DrugAlternativeDto>();
        public DateTime CreatedAt { get; set; }
        
        // Tableau Dashboard URLs
        public string? TableauDashboardUrl { get; set; }
        public string? TableauUserSpecificUrl { get; set; }
        public Dictionary<string, object>? TableauUserContext { get; set; }
    }

    public class DrugAlternativeDto
    {
        public string AlternativeDrug { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string ClinicalEvidence { get; set; } = string.Empty;
        public string DosageRecommendation { get; set; } = string.Empty;
        public string MonitoringRequirements { get; set; } = string.Empty;
    }
} 