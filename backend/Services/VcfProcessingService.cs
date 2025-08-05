using DrugRiskAPI.Models;

namespace DrugRiskAPI.Services
{
    public class VcfProcessingService : IVcfProcessingService
    {
        private readonly ILogger<VcfProcessingService> _logger;

        public VcfProcessingService(ILogger<VcfProcessingService> logger)
        {
            _logger = logger;
        }

        public async Task<List<VcfData>> ProcessVcfContentAsync(string vcfContent, int userRunId)
        {
            var variants = new List<VcfData>();
            var lines = vcfContent.Split('\n');

            foreach (var line in lines)
            {
                if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split('\t');
                if (parts.Length < 8) continue;

                var variant = new VcfData
                {
                    UserRunId = userRunId,
                    Chromosome = parts[0],
                    Position = int.Parse(parts[1]),
                    ReferenceAllele = parts[3],
                    AlternateAllele = parts[4],
                    Gene = ExtractGene(parts[7]),
                    Impact = ExtractImpact(parts[7]),
                    ClinicalSignificance = ExtractClinicalSignificance(parts[7]),
                    DrugInteractions = ExtractDrugInteractions(parts[7])
                };

                variants.Add(variant);
            }

            _logger.LogInformation($"Processed {variants.Count} variants from VCF content");
            
            foreach (var variant in variants)
            {
                _logger.LogInformation($"Variant: Chr{variant.Chromosome}:{variant.Position} {variant.ReferenceAllele}->{variant.AlternateAllele}, Gene={variant.Gene}, Impact={variant.Impact}, ClinicalSig={variant.ClinicalSignificance}");
            }
            
            return await Task.FromResult(variants);
        }

        public async Task<List<VcfData>> ExtractRelevantVariantsAsync(List<VcfData> variants, string drugName)
        {
            var drugGeneMapping = new Dictionary<string, List<string>>
            {
                ["Warfarin"] = new List<string> { "CYP2C9", "VKORC1" },
                ["Clopidogrel"] = new List<string> { "CYP2C19" },
                ["Simvastatin"] = new List<string> { "SLCO1B1" },
                ["Codeine"] = new List<string> { "CYP2D6" }
            };

            var relevantGenes = drugGeneMapping.GetValueOrDefault(drugName, new List<string>());
            var relevantVariants = variants.Where(v => 
                relevantGenes.Any(gene => 
                    v.Gene?.Contains(gene, StringComparison.OrdinalIgnoreCase) == true ||
                    v.DrugInteractions?.Contains(drugName, StringComparison.OrdinalIgnoreCase) == true
                )).ToList();

            _logger.LogInformation($"Found {relevantVariants.Count} relevant variants for drug {drugName}");
            return await Task.FromResult(relevantVariants);
        }

        public async Task<Dictionary<string, object>> CalculateVariantMetricsAsync(List<VcfData> variants)
        {
            var metrics = new Dictionary<string, object>
            {
                ["TotalVariants"] = variants.Count,
                ["HighImpactVariants"] = variants.Count(v => v.Impact?.Equals("HIGH", StringComparison.OrdinalIgnoreCase) == true),
                ["ModerateImpactVariants"] = variants.Count(v => v.Impact?.Equals("MODERATE", StringComparison.OrdinalIgnoreCase) == true),
                ["LowImpactVariants"] = variants.Count(v => v.Impact?.Equals("LOW", StringComparison.OrdinalIgnoreCase) == true),
                ["HighClinicalSignificance"] = variants.Count(v => v.ClinicalSignificance?.Equals("HIGH", StringComparison.OrdinalIgnoreCase) == true),
                ["ModerateClinicalSignificance"] = variants.Count(v => v.ClinicalSignificance?.Equals("MODERATE", StringComparison.OrdinalIgnoreCase) == true),
                ["LowClinicalSignificance"] = variants.Count(v => v.ClinicalSignificance?.Equals("LOW", StringComparison.OrdinalIgnoreCase) == true),
                ["UniqueGenes"] = variants.Where(v => !string.IsNullOrEmpty(v.Gene)).Select(v => v.Gene).Distinct().Count()
            };

            return await Task.FromResult(metrics);
        }

        private string? ExtractGene(string info)
        {
            if (info.Contains("Gene="))
            {
                var geneStart = info.IndexOf("Gene=") + 5;
                var geneEnd = info.IndexOf(';', geneStart);
                return geneEnd > geneStart ? info.Substring(geneStart, geneEnd - geneStart) : info.Substring(geneStart);
            }
            else if (info.Contains("GENE="))
            {
                var geneStart = info.IndexOf("GENE=") + 5;
                var geneEnd = info.IndexOf(';', geneStart);
                return geneEnd > geneStart ? info.Substring(geneStart, geneEnd - geneStart) : info.Substring(geneStart);
            }
            return null;
        }

        private string? ExtractImpact(string info)
        {
            if (info.Contains("IMPACT="))
            {
                var impactStart = info.IndexOf("IMPACT=") + 7;
                var impactEnd = info.IndexOf(';', impactStart);
                return impactEnd > impactStart ? info.Substring(impactStart, impactEnd - impactStart) : info.Substring(impactStart);
            }
            return null;
        }

        private string? ExtractClinicalSignificance(string info)
        {
            if (info.Contains("CLNSIG="))
            {
                var sigStart = info.IndexOf("CLNSIG=") + 7;
                var sigEnd = info.IndexOf(';', sigStart);
                return sigEnd > sigStart ? info.Substring(sigStart, sigEnd - sigStart) : info.Substring(sigStart);
            }
            else if (info.Contains("CLIN_SIG="))
            {
                var sigStart = info.IndexOf("CLIN_SIG=") + 9;
                var sigEnd = info.IndexOf(';', sigStart);
                return sigEnd > sigStart ? info.Substring(sigStart, sigEnd - sigStart) : info.Substring(sigStart);
            }
            return null;
        }

        private string? ExtractDrugInteractions(string info)
        {
            if (info.Contains("Drug="))
            {
                var drugStart = info.IndexOf("Drug=") + 5;
                var drugEnd = info.IndexOf(';', drugStart);
                return drugEnd > drugStart ? info.Substring(drugStart, drugEnd - drugStart) : info.Substring(drugStart);
            }
            return null;
        }
    }
} 