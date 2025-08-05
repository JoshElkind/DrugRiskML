using DrugRiskAPI.DTOs;
using DrugRiskAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DrugRiskAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TableauController : ControllerBase
    {
        private readonly ITableauService _tableauService;
        private readonly ILogger<TableauController> _logger;
        private readonly IConfiguration _configuration;

        public TableauController(ITableauService tableauService, ILogger<TableauController> logger, IConfiguration configuration)
        {
            _tableauService = tableauService;
            _logger = logger;
            _configuration = configuration;
        }

      
        [HttpGet("analytics/drug-risk")]
        public async Task<ActionResult<TableauEmbedUrl>> GetDrugRiskAnalytics([FromQuery] string? drugName = null)
        {
            try
            {
                var filters = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(drugName))
                {
                    filters["vf_drugName"] = drugName; 
                }

                // consistent dashboard name
                var embedUrl = await _tableauService.GetEmbedUrlAsync("DrugRiskAnalytics/Dashboard", filters);
                return Ok(embedUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drug risk analytics embed URL");
                return BadRequest(new { error = ex.Message });
            }
        }

        
        [HttpGet("analytics/user-specific")]
        public async Task<ActionResult<TableauEmbedUrl>> GetUserSpecificAnalytics(
            [FromQuery] string drugName,
            [FromQuery] decimal userRiskScore,
            [FromQuery] string userRiskLevel,
            [FromQuery] string? userId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(drugName) || string.IsNullOrEmpty(userRiskLevel))
                {
                    return BadRequest(new { error = "drugName and userRiskLevel are required parameters" });
                }

                var embedUrl = await _tableauService.GetUserSpecificAnalyticsAsync(
                    drugName, userRiskScore, userRiskLevel, userId);
                return Ok(embedUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user-specific Tableau analytics");
                return BadRequest(new { error = ex.Message });
            }
        }

       
        [HttpGet("analytics/user/{userId}")]
        public async Task<ActionResult<TableauEmbedUrl>> GetUserAnalytics(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { error = "userId is required" });
                }

                var filters = new Dictionary<string, string>
                {
                    ["vf_UserId"] = userId 
                };

                var embedUrl = await _tableauService.GetEmbedUrlAsync("DrugRiskAnalytics/Dashboard", filters);
                return Ok(embedUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Tableau embed URL for user analytics");
                return BadRequest(new { error = ex.Message });
            }
        }

        
        [HttpGet("dashboards")]
        public async Task<ActionResult<List<string>>> GetAvailableDashboards()
        {
            try
            {
                var dashboards = await _tableauService.GetAvailableDashboardsAsync();
                return Ok(dashboards);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available dashboards");
                return BadRequest(new { error = ex.Message });
            }
        }

       
        [HttpPost("validate-token")]
        public async Task<ActionResult<bool>> ValidateToken([FromBody] string token)
        {
            try
            {
                var isValid = await _tableauService.ValidateTokenAsync(token);
                return Ok(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Tableau token");
                return BadRequest(new { error = ex.Message });
            }
        }

        
        [HttpGet("debug/env")]
        public ActionResult<object> DebugEnvironmentVariables()
        {
            var username = Environment.GetEnvironmentVariable("TABLEAU_USERNAME");
            var email = Environment.GetEnvironmentVariable("TABLEAU_EMAIL");
            var password = Environment.GetEnvironmentVariable("TABLEAU_PASSWORD");
            var snowflakeUser = Environment.GetEnvironmentVariable("SNOWFLAKE_USER");
            var snowflakePassword = Environment.GetEnvironmentVariable("SNOWFLAKE_PASSWORD");
            
            return Ok(new
            {
                tableau = new
                {
                    username = username ?? "Not set",
                    email = email ?? "Not set",
                    password = !string.IsNullOrEmpty(password) ? "Set" : "Not set",
                    authUsername = !string.IsNullOrEmpty(username) ? username : email ?? "Not available",
                    serverUrl = _configuration["Tableau:ServerUrl"] ?? "Not configured",
                    siteId = _configuration["Tableau:SiteId"] ?? "Not configured"
                },
                snowflake = new
                {
                    user = snowflakeUser ?? "Not set",
                    password = !string.IsNullOrEmpty(snowflakePassword) ? "Set" : "Not set",
                    account = Environment.GetEnvironmentVariable("SNOWFLAKE_ACCOUNT") ?? "Not set",
                    warehouse = Environment.GetEnvironmentVariable("SNOWFLAKE_WAREHOUSE") ?? "Not set",
                    database = Environment.GetEnvironmentVariable("SNOWFLAKE_DATABASE") ?? "Not set",
                    schema = Environment.GetEnvironmentVariable("SNOWFLAKE_SCHEMA") ?? "Not set"
                },
                configuration = new
                {
                    envFileExists = System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env")),
                    dotEnvPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"),
                    currentDirectory = Directory.GetCurrentDirectory()
                }
            });
        }

       
        [HttpGet("debug/auth")]
        public async Task<ActionResult<object>> DebugTableauAuth()
        {
            try
            {
                var token = await _tableauService.GetTableauTokenAsync();
                
                return Ok(new
                {
                    token = token == "embed" ? "Public embed mode" : "Authenticated token received",
                    tokenLength = token?.Length ?? 0,
                    isEmbedToken = token == "embed",
                    isAuthenticated = !string.IsNullOrEmpty(token) && token != "embed",
                    timestamp = DateTime.UtcNow,
                    recommendations = GetAuthRecommendations(token)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { 
                    error = ex.Message, 
                    stackTrace = ex.StackTrace,
                    recommendations = new[] { "Check Tableau credentials and server configuration" }
                });
            }
        }

        
        [HttpGet("debug/embed-url")]
        public async Task<ActionResult<object>> DebugEmbedUrl(
            [FromQuery] string? drugName = "Clopidogrel",
            [FromQuery] decimal userRiskScore = 0.53m,
            [FromQuery] string userRiskLevel = "MODERATE")
        {
            try
            {
                var embedUrl = await _tableauService.GetUserSpecificAnalyticsAsync(
                    drugName ?? "Clopidogrel", userRiskScore, userRiskLevel, "debug-user");

                return Ok(new
                {
                    url = embedUrl.Url,
                    token = embedUrl.Token,
                    expiresAt = embedUrl.ExpiresAt,
                    userContext = embedUrl.UserContext,
                    urlLength = embedUrl.Url?.Length ?? 0,
                    containsAuth = embedUrl.Url?.Contains("token=") ?? false,
                    containsEmbedParams = embedUrl.Url?.Contains(":embed=yes") ?? false
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        private string[] GetAuthRecommendations(string? token)
        {
            var recommendations = new List<string>();

            if (string.IsNullOrEmpty(token) || token == "embed")
            {
                recommendations.Add("Using public embed mode - ensure dashboard is published and accessible publicly");
                recommendations.Add("Set TABLEAU_USERNAME/EMAIL and TABLEAU_PASSWORD environment variables for authenticated access");
                recommendations.Add("Verify Tableau:ServerUrl and Tableau:SiteId in appsettings.json");
            }
            else
            {
                recommendations.Add("Authentication successful - using authenticated embed");
                recommendations.Add("Token will be cached for 50 minutes to improve performance");
            }

            var username = Environment.GetEnvironmentVariable("TABLEAU_USERNAME");
            var email = Environment.GetEnvironmentVariable("TABLEAU_EMAIL");
            var password = Environment.GetEnvironmentVariable("TABLEAU_PASSWORD");

            if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(email))
            {
                recommendations.Add("Set TABLEAU_USERNAME or TABLEAU_EMAIL environment variable");
            }

            if (string.IsNullOrEmpty(password))
            {
                recommendations.Add("Set TABLEAU_PASSWORD environment variable");
            }

            return recommendations.ToArray();
        }
    }
} 