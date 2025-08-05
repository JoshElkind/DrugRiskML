using System.ComponentModel.DataAnnotations;

namespace DrugRiskAPI.DTOs
{
    public class DrugRiskRequest
    {
        public string? UserId { get; set; } 
        [Required]
        public string DrugName { get; set; } = string.Empty;
        [Required]
        public string VcfFileContent { get; set; } = string.Empty;
        public string? VcfFileName { get; set; }
        public Dictionary<string, object>? AdditionalMetadata { get; set; }
    }
} 