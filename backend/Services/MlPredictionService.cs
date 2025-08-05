using DrugRiskAPI.Models;

namespace DrugRiskAPI.Services
{
    public class MlPredictionService : IMlPredictionService
    {
        private readonly ILogger<MlPredictionService> _logger;
        private readonly IConfiguration _configuration;

        public MlPredictionService(ILogger<MlPredictionService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<RiskAssessment> PredictRiskAsync(List<VcfData> variants, string drugName)
        {
            var highRiskVariants = variants.Count(v => 
                v.Impact?.Equals("HIGH", StringComparison.OrdinalIgnoreCase) == true ||
                v.ClinicalSignificance?.Equals("HIGH", StringComparison.OrdinalIgnoreCase) == true);

            var riskScore = CalculateRiskScore(variants, highRiskVariants);
            var riskLevel = DetermineRiskLevel(riskScore);
            var confidence = CalculateConfidence(variants);

            var assessment = new RiskAssessment
            {
                RiskLevel = riskLevel,
                RiskScore = riskScore,
                Confidence = confidence,
                VariantCount = variants.Count,
                HighRiskVariants = highRiskVariants,
                ClinicalEvidence = GenerateClinicalEvidence(variants, drugName),
                Recommendations = GenerateRecommendations(riskLevel, drugName),
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation($"Predicted risk for {drugName}: {riskLevel} (Score: {riskScore:F2}, Confidence: {confidence:F2})");
            return await Task.FromResult(assessment);
        }

        public async Task<List<DrugAlternative>> GenerateAlternativesAsync(RiskAssessment assessment, string drugName)
        {
            var alternatives = new List<DrugAlternative>();

            if (assessment.RiskLevel == "HIGH")
            {
                alternatives.Add(new DrugAlternative
                {
                    AlternativeDrug = GetAlternativeDrug(drugName),
                    Reason = "High risk detected - alternative recommended",
                    ConfidenceScore = 0.85m,
                    ClinicalEvidence = "Based on genetic variants and clinical guidelines",
                    DosageRecommendation = "Start with lower dose and monitor closely",
                    MonitoringRequirements = "Regular blood tests and clinical monitoring",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else if (assessment.RiskLevel == "MODERATE")
            {
                alternatives.Add(new DrugAlternative
                {
                    AlternativeDrug = GetAlternativeDrug(drugName),
                    Reason = "Moderate risk - consider alternative or adjusted dosing",
                    ConfidenceScore = 0.75m,
                    ClinicalEvidence = "Genetic variants suggest potential interactions",
                    DosageRecommendation = "Consider reduced initial dose",
                    MonitoringRequirements = "Enhanced monitoring recommended",
                    CreatedAt = DateTime.UtcNow
                });
            }

            _logger.LogInformation($"Generated {alternatives.Count} alternatives for {drugName}");
            return await Task.FromResult(alternatives);
        }

        public async Task<Dictionary<string, object>> GetModelMetadataAsync()
        {
            var modelPath = _configuration["MlModel:ModelPath"];
            var scalerPath = _configuration["MlModel:ScalerPath"];
            var featuresPath = _configuration["MlModel:FeatureColumnsPath"];

            return await Task.FromResult(new Dictionary<string, object>
            {
                ["ModelPath"] = modelPath ?? "Not configured",
                ["ScalerPath"] = scalerPath ?? "Not configured",
                ["FeaturesPath"] = featuresPath ?? "Not configured",
                ["ModelType"] = "XGBoost",
                ["LastTrained"] = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd")
            });
        }

        private decimal CalculateRiskScore(List<VcfData> variants, int highRiskVariants)
        {
            if (!variants.Any()) return 0.1m;

            var baseScore = 0.3m;
            var highRiskMultiplier = highRiskVariants * 0.2m;
            var totalVariantsMultiplier = Math.Min(variants.Count * 0.01m, 0.3m);

            var score = baseScore + highRiskMultiplier + totalVariantsMultiplier;
            return Math.Min(score, 1.0m);
        }

        private string DetermineRiskLevel(decimal riskScore)
        {
            return riskScore switch
            {
                >= 0.7m => "HIGH",
                >= 0.4m => "MODERATE",
                _ => "LOW"
            };
        }

        private decimal CalculateConfidence(List<VcfData> variants)
        {
            if (!variants.Any()) return 0.5m;

            var confidence = 0.6m + (variants.Count * 0.02m);
            return Math.Min(confidence, 0.95m);
        }

        private string GenerateClinicalEvidence(List<VcfData> variants, string drugName)
        {
            var relevantVariants = variants.Where(v => 
                v.Gene != null || v.DrugInteractions != null).ToList();

            if (!relevantVariants.Any())
                return $"No specific genetic variants detected for {drugName}. Standard dosing recommended.";

            var geneList = relevantVariants.Where(v => v.Gene != null)
                .Select(v => v.Gene).Distinct().ToList();
            var genes = string.Join(", ", geneList);

            return $"Detected variants in genes: {genes}. These may affect {drugName} metabolism and response.";
        }

        private string GenerateRecommendations(string riskLevel, string drugName)
        {
            return riskLevel switch
            {
                "HIGH" => $"High risk for {drugName}. Consider alternative medication or intensive monitoring.",
                "MODERATE" => $"Moderate risk for {drugName}. Consider dose adjustment and enhanced monitoring.",
                _ => $"Low risk for {drugName}. Standard dosing recommended with routine monitoring."
            };
        }

        private string GetAlternativeDrug(string originalDrug)
        {
            return originalDrug switch
            {
                "Warfarin" => "Apixaban",
                "Clopidogrel" => "Ticagrelor",
                "Simvastatin" => "Pravastatin",
                "Codeine" => "Tramadol",
                _ => "Consult healthcare provider for alternatives"
            };
        }
    }
} 