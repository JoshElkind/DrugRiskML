using DrugRiskAPI.Data;
using DrugRiskAPI.DTOs;
using DrugRiskAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DrugRiskAPI.Services
{
    public class DrugRiskService : IDrugRiskService
    {
        private readonly DrugRiskContext _context;
        private readonly IVcfProcessingService _vcfService;
        private readonly IMlPredictionService _mlService;
        private readonly IAnalyticsService _analyticsService;
        private readonly ISnowflakeService? _snowflakeService;
        private readonly ITableauService? _tableauService;

        public DrugRiskService(
            DrugRiskContext context,
            IVcfProcessingService vcfService,
            IMlPredictionService mlService,
            IAnalyticsService analyticsService,
            ISnowflakeService? snowflakeService = null,
            ITableauService? tableauService = null)
        {
            _context = context;
            _vcfService = vcfService;
            _mlService = mlService;
            _analyticsService = analyticsService;
            _snowflakeService = snowflakeService;
            _tableauService = tableauService;
        }

        public async Task<DrugRiskResponse> AssessDrugRiskAsync(DrugRiskRequest request)
        {
            //  user run (anonymous - no login required)
            var userRun = new UserRun
            {
                UserId = request.UserId ?? $"anonymous-{Guid.NewGuid().ToString("N")[..8]}",
                DrugName = request.DrugName,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserRuns.Add(userRun);
            await _context.SaveChangesAsync();

            try
            {
                // Process VCF data
                var vcfData = await _vcfService.ProcessVcfContentAsync(request.VcfFileContent, userRun.Id);
                var relevantVariants = await _vcfService.ExtractRelevantVariantsAsync(vcfData, request.DrugName);
                
                // Add VCF data to context
                _context.VcfData.AddRange(vcfData);
                await _context.SaveChangesAsync();

                // Get ML prediction
                var riskAssessment = await _mlService.PredictRiskAsync(relevantVariants, request.DrugName);
                riskAssessment.UserRunId = userRun.Id;
                
                // Generate alternatives
                var alternatives = await _mlService.GenerateAlternativesAsync(riskAssessment, request.DrugName);
                foreach (var alt in alternatives)
                {
                    alt.UserRunId = userRun.Id;
                }

                // Save to database
                _context.RiskAssessments.Add(riskAssessment);
                _context.DrugAlternatives.AddRange(alternatives);
                await _context.SaveChangesAsync();

                // Update user run with results
                userRun.RiskScore = riskAssessment.RiskScore;
                userRun.RiskLevel = riskAssessment.RiskLevel;
                userRun.Explanation = riskAssessment.ClinicalEvidence;
                userRun.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Track analytics event
                await _analyticsService.TrackAnalyticsEventAsync(
                    userRun.Id, 
                    "DRUG_RISK_ASSESSMENT", 
                    $"Drug: {request.DrugName}, Risk: {riskAssessment.RiskLevel}, Score: {riskAssessment.RiskScore}");

                // Sync to Snowflake for community analytics 
                if (_snowflakeService != null)
                {
                    await _snowflakeService.SyncUserRunToSnowflakeAsync(userRun);
                }

                // Generate Tableau dashboard URLs 
                string? tableauDashboardUrl = null;
                string? tableauUserSpecificUrl = null;
                Dictionary<string, object>? tableauUserContext = null;

                if (_tableauService != null)
                {
                    try
                    {
                        // Get general dashboard URL
                        var generalDashboard = await _tableauService.GetEmbedUrlAsync("DrugRiskAnalytics/RiskDashboard");
                        tableauDashboardUrl = generalDashboard.Url;

                        // Get user-specific dashboard URL
                        var userSpecificDashboard = await _tableauService.GetUserSpecificAnalyticsAsync(
                            userRun.DrugName, 
                            userRun.RiskScore, 
                            userRun.RiskLevel, 
                            userRun.UserId);
                        tableauUserSpecificUrl = userSpecificDashboard.Url;
                        tableauUserContext = userSpecificDashboard.UserContext;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error generating Tableau URLs: {ex.Message}");
                    }
                }

                //  response
                return new DrugRiskResponse
                {
                    UserRunId = userRun.Id,
                    UserId = userRun.UserId,
                    DrugName = userRun.DrugName,
                    RiskScore = userRun.RiskScore,
                    RiskLevel = userRun.RiskLevel,
                    Explanation = userRun.Explanation,
                    DrugAlternatives = alternatives.Select(a => new DrugAlternativeDto
                    {
                        AlternativeDrug = a.AlternativeDrug,
                        Reason = a.Reason,
                        ClinicalEvidence = a.ClinicalEvidence,
                        DosageRecommendation = a.DosageRecommendation,
                        MonitoringRequirements = a.MonitoringRequirements
                    }).ToList(),
                    CreatedAt = userRun.CreatedAt,
                    TableauDashboardUrl = tableauDashboardUrl,
                    TableauUserSpecificUrl = tableauUserSpecificUrl,
                    TableauUserContext = tableauUserContext
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error assessing drug risk: {ex.Message}", ex);
            }
        }

        public async Task<DrugRiskResponse?> GetUserRunAsync(int userRunId)
        {
            var userRun = await _context.UserRuns
                .Include(u => u.RiskAssessment)
                .Include(u => u.DrugAlternatives)
                .FirstOrDefaultAsync(u => u.Id == userRunId);

            if (userRun == null) return null;

            string? tableauDashboardUrl = null;
            string? tableauUserSpecificUrl = null;
            Dictionary<string, object>? tableauUserContext = null;

            if (_tableauService != null)
            {
                try
                {
                    var generalDashboard = await _tableauService.GetEmbedUrlAsync("DrugRiskAnalytics/RiskDashboard");
                    tableauDashboardUrl = generalDashboard.Url;

                    var userSpecificDashboard = await _tableauService.GetUserSpecificAnalyticsAsync(
                        userRun.DrugName, 
                        userRun.RiskScore, 
                        userRun.RiskLevel, 
                        userRun.UserId);
                    tableauUserSpecificUrl = userSpecificDashboard.Url;
                    tableauUserContext = userSpecificDashboard.UserContext;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error generating Tableau URLs: {ex.Message}");
                }
            }

            return new DrugRiskResponse
            {
                UserRunId = userRun.Id,
                UserId = userRun.UserId,
                DrugName = userRun.DrugName,
                RiskScore = userRun.RiskScore,
                RiskLevel = userRun.RiskLevel,
                Explanation = userRun.Explanation,
                DrugAlternatives = userRun.DrugAlternatives.Select(a => new DrugAlternativeDto
                {
                    AlternativeDrug = a.AlternativeDrug,
                    Reason = a.Reason,
                    ClinicalEvidence = a.ClinicalEvidence,
                    DosageRecommendation = a.DosageRecommendation,
                    MonitoringRequirements = a.MonitoringRequirements
                }).ToList(),
                CreatedAt = userRun.CreatedAt,
                TableauDashboardUrl = tableauDashboardUrl,
                TableauUserSpecificUrl = tableauUserSpecificUrl,
                TableauUserContext = tableauUserContext
            };
        }

        public async Task<List<DrugRiskResponse>> GetUserRunsAsync(string userId)
        {
            var userRuns = await _context.UserRuns
                .Include(u => u.RiskAssessment)
                .Include(u => u.DrugAlternatives)
                .Where(u => u.UserId == userId)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return userRuns.Select(ur => new DrugRiskResponse
            {
                UserRunId = ur.Id,
                UserId = ur.UserId,
                DrugName = ur.DrugName,
                RiskScore = ur.RiskScore,
                RiskLevel = ur.RiskLevel,
                Explanation = ur.Explanation,
                DrugAlternatives = ur.DrugAlternatives.Select(a => new DrugAlternativeDto
                {
                    AlternativeDrug = a.AlternativeDrug,
                    Reason = a.Reason,
                    ClinicalEvidence = a.ClinicalEvidence,
                    DosageRecommendation = a.DosageRecommendation,
                    MonitoringRequirements = a.MonitoringRequirements
                }).ToList(),
                CreatedAt = ur.CreatedAt
            }).ToList();
        }

        public async Task<AnalyticsResponse> GetCommunityAnalyticsAsync(string? drugName = null)
        {
            return await _analyticsService.GetCommunityAnalyticsAsync(drugName);
        }

        public async Task<AnalyticsResponse> GetUserAnalyticsAsync(string userId)
        {
            return await _analyticsService.GetUserAnalyticsAsync(userId);
        }
    }
} 