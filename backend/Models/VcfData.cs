using System.ComponentModel.DataAnnotations;

namespace DrugRiskAPI.Models
{
    public class VcfData
    {
        public int Id { get; set; }
        public int UserRunId { get; set; }
        public string Chromosome { get; set; } = string.Empty;
        public int Position { get; set; }
        public string ReferenceAllele { get; set; } = string.Empty;
        public string AlternateAllele { get; set; } = string.Empty;
        public string? Gene { get; set; }
        public string? Impact { get; set; }
        public string? ClinicalSignificance { get; set; }
        public string? DrugInteractions { get; set; }
        
        public virtual UserRun UserRun { get; set; } = null!;
    }
} 