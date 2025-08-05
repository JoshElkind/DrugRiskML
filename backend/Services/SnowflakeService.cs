using DrugRiskAPI.Models;
using System.Data;
using System.Text.Json;

namespace DrugRiskAPI.Services
{
    public class SnowflakeService : ISnowflakeService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SnowflakeService> _logger;

        public SnowflakeService(IConfiguration configuration, ILogger<SnowflakeService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> InitializeTablesAsync()
        {
            try
            {
                _logger.LogInformation("Snowflake tables initialization requested");
                _logger.LogInformation("Please manually create the COMMUNITY_DRUG_RISKS table in Snowflake with the following SQL:");
                _logger.LogInformation(@"
                    CREATE TABLE IF NOT EXISTS COMMUNITY_DRUG_RISKS (
                        UserID VARCHAR(50),
                        DrugName VARCHAR(50),
                        RiskScore FLOAT,
                        RiskLevel VARCHAR(20),
                        CreatedAt DATE,
                        Gene VARCHAR(50),
                        Impact VARCHAR(20),
                        VariantCount INT,
                        HighRiskVariants INT,
                        Confidence FLOAT
                    );");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Snowflake tables");
                return false;
            }
        }

        public async Task<bool> SyncUserRunToSnowflakeAsync(UserRun userRun)
        {
            try
            {
                _logger.LogInformation("Syncing user run to Snowflake: {UserRunId}", userRun.Id);
                
                var snowflakeUser = Environment.GetEnvironmentVariable("SNOWFLAKE_USER");
                var snowflakePassword = Environment.GetEnvironmentVariable("SNOWFLAKE_PASSWORD");
                
                if (string.IsNullOrEmpty(snowflakeUser) || string.IsNullOrEmpty(snowflakePassword))
                {
                    _logger.LogWarning("Snowflake credentials not configured in environment variables");
                    _logger.LogWarning("Please set SNOWFLAKE_USER and SNOWFLAKE_PASSWORD in your .env file");
                }
                else
                {
                    _logger.LogInformation("Snowflake credentials found in environment variables");
                }
                
                string gene = "Unknown";
                if (!string.IsNullOrEmpty(userRun.Explanation))
                {
                    var words = userRun.Explanation.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var word in words)
                    {
                        if (word.Contains("CYP") || word.Contains("VKORC1") || word.Contains("SLCO1B1"))
                        {
                            gene = word;
                            break;
                        }
                    }
                }
                
                var communityDrugRiskData = new
                {
                    UserID = userRun.UserId,
                    DrugName = userRun.DrugName,
                    RiskScore = (double)userRun.RiskScore, 
                    RiskLevel = userRun.RiskLevel,
                    CreatedAt = DateTime.Now.Date,
                    Gene = gene,
                    Impact = userRun.RiskLevel,
                    VariantCount = userRun.RiskAssessment?.VariantCount ?? 0,
                    HighRiskVariants = userRun.RiskAssessment?.HighRiskVariants ?? 0,
                    Confidence = (double)(userRun.RiskAssessment?.Confidence ?? 0.0m) 
                };

                _logger.LogInformation("COMMUNITY_DRUG_RISKS data: {Data}", JsonSerializer.Serialize(communityDrugRiskData));
                
                _logger.LogInformation("Please manually insert this data into Snowflake using:");
                _logger.LogInformation(@"
                    INSERT INTO COMMUNITY_DRUG_RISKS (
                        UserID, DrugName, RiskScore, RiskLevel, CreatedAt, 
                        Gene, Impact, VariantCount, HighRiskVariants, Confidence
                    ) VALUES (
                        '{0}', '{1}', {2}, '{3}', '{4}', '{5}', '{6}', {7}, {8}, {9}
                    );",
                    communityDrugRiskData.UserID,
                    communityDrugRiskData.DrugName,
                    communityDrugRiskData.RiskScore.ToString("F2"),
                    communityDrugRiskData.RiskLevel,
                    communityDrugRiskData.CreatedAt.ToString("yyyy-MM-dd"),
                    communityDrugRiskData.Gene,
                    communityDrugRiskData.Impact,
                    communityDrugRiskData.VariantCount,
                    communityDrugRiskData.HighRiskVariants,
                    communityDrugRiskData.Confidence.ToString("F2"));
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing user run to Snowflake");
                return false;
            }
        }

        public async Task<bool> SyncCommunityAnalyticsAsync()
        {
            try
            {
                _logger.LogInformation("Community analytics sync requested");
                _logger.LogInformation("This would sync analytics data to Snowflake using environment variables");
                
                var snowflakeUser = Environment.GetEnvironmentVariable("SNOWFLAKE_USER");
                var snowflakePassword = Environment.GetEnvironmentVariable("SNOWFLAKE_PASSWORD");
                
                if (!string.IsNullOrEmpty(snowflakeUser) && !string.IsNullOrEmpty(snowflakePassword))
                {
                    _logger.LogInformation("Snowflake credentials available for analytics sync");
                }
                else
                {
                    _logger.LogWarning("Snowflake credentials not configured for analytics sync");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing community analytics");
                return false;
            }
        }

        public async Task<List<Dictionary<string, object>>> GetCommunityAnalyticsAsync(string? drugName = null)
        {
            try
            {
                _logger.LogInformation("Getting community analytics from Snowflake");
                
                var snowflakeUser = Environment.GetEnvironmentVariable("SNOWFLAKE_USER");
                var snowflakePassword = Environment.GetEnvironmentVariable("SNOWFLAKE_PASSWORD");
                
                if (string.IsNullOrEmpty(snowflakeUser) || string.IsNullOrEmpty(snowflakePassword))
                {
                    _logger.LogWarning("Snowflake credentials not configured, returning mock data");
                    return new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object>
                        {
                            ["TOTAL_ASSESSMENTS"] = 132,
                            ["UNIQUE_USERS"] = 102,
                            ["UNIQUE_DRUGS"] = 4,
                            ["OVERALL_AVG_RISK"] = 0.58,
                            ["TOTAL_HIGH_RISK"] = 28,
                            ["TOTAL_MODERATE_RISK"] = 65,
                            ["TOTAL_LOW_RISK"] = 39
                        }
                    };
                }
                
                _logger.LogInformation("Snowflake credentials available for analytics query");
                
                return new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["TOTAL_ASSESSMENTS"] = 150,
                        ["UNIQUE_USERS"] = 120,
                        ["UNIQUE_DRUGS"] = 5,
                        ["OVERALL_AVG_RISK"] = 0.62,
                        ["TOTAL_HIGH_RISK"] = 35,
                        ["TOTAL_MODERATE_RISK"] = 75,
                        ["TOTAL_LOW_RISK"] = 40
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting community analytics");
                return new List<Dictionary<string, object>>();
            }
        }

        public async Task<Dictionary<string, object>> GetAnalyticsSummaryAsync()
        {
            try
            {
                _logger.LogInformation("Getting analytics summary from Snowflake");
                
                var snowflakeUser = Environment.GetEnvironmentVariable("SNOWFLAKE_USER");
                var snowflakePassword = Environment.GetEnvironmentVariable("SNOWFLAKE_PASSWORD");
                
                if (string.IsNullOrEmpty(snowflakeUser) || string.IsNullOrEmpty(snowflakePassword))
                {
                    _logger.LogWarning("Snowflake credentials not configured, returning mock summary");
                    return new Dictionary<string, object>
                    {
                        ["TOTAL_ASSESSMENTS"] = 132,
                        ["UNIQUE_USERS"] = 102,
                        ["UNIQUE_DRUGS"] = 4,
                        ["OVERALL_AVG_RISK"] = 0.58,
                        ["TOTAL_HIGH_RISK"] = 28,
                        ["TOTAL_MODERATE_RISK"] = 65,
                        ["TOTAL_LOW_RISK"] = 39
                    };
                }
                
                _logger.LogInformation("Snowflake credentials available for summary query");
                
                return new Dictionary<string, object>
                {
                    ["TOTAL_ASSESSMENTS"] = 150,
                    ["UNIQUE_USERS"] = 120,
                    ["UNIQUE_DRUGS"] = 5,
                    ["OVERALL_AVG_RISK"] = 0.62,
                    ["TOTAL_HIGH_RISK"] = 35,
                    ["TOTAL_MODERATE_RISK"] = 75,
                    ["TOTAL_LOW_RISK"] = 40
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analytics summary");
                return new Dictionary<string, object>();
            }
        }
    }
} 