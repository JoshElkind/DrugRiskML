using DrugRiskAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DrugRiskAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SnowflakeController : ControllerBase
    {
        private readonly ISnowflakeService _snowflakeService;
        private readonly ILogger<SnowflakeController> _logger;

        public SnowflakeController(ISnowflakeService snowflakeService, ILogger<SnowflakeController> logger)
        {
            _snowflakeService = snowflakeService;
            _logger = logger;
        }

        [HttpPost("initialize")]
        public async Task<ActionResult<bool>> InitializeTables()
        {
            try
            {
                _logger.LogInformation("Initializing Snowflake tables for community analytics");

                var result = await _snowflakeService.InitializeTablesAsync();

                if (result)
                {
                    _logger.LogInformation("Snowflake tables initialized successfully");
                    return Ok(new { success = true, message = "Snowflake tables initialized successfully" });
                }
                else
                {
                    _logger.LogError("Failed to initialize Snowflake tables");
                    return StatusCode(500, new { success = false, message = "Failed to initialize Snowflake tables" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Snowflake tables");
                return StatusCode(500, new { success = false, message = "Error initializing Snowflake tables", details = ex.Message });
            }
        }

     
        [HttpGet("analytics/community")]
        public async Task<ActionResult<List<Dictionary<string, object>>>> GetCommunityAnalytics([FromQuery] string? drugName = null)
        {
            try
            {
                _logger.LogInformation("Getting community analytics from Snowflake for drug: {DrugName}", drugName ?? "ALL");

                var analytics = await _snowflakeService.GetCommunityAnalyticsAsync(drugName);

                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting community analytics from Snowflake");
                return StatusCode(500, new { error = "Error getting community analytics", details = ex.Message });
            }
        }

       
        [HttpGet("analytics/summary")]
        public async Task<ActionResult<Dictionary<string, object>>> GetAnalyticsSummary()
        {
            try
            {
                _logger.LogInformation("Getting analytics summary from Snowflake");

                var summary = await _snowflakeService.GetAnalyticsSummaryAsync();

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analytics summary from Snowflake");
                return StatusCode(500, new { error = "Error getting analytics summary", details = ex.Message });
            }
        }

       
        [HttpPost("analytics/sync")]
        public async Task<ActionResult<bool>> SyncCommunityAnalytics()
        {
            try
            {
                _logger.LogInformation("Syncing community analytics view in Snowflake");

                var result = await _snowflakeService.SyncCommunityAnalyticsAsync();

                if (result)
                {
                    _logger.LogInformation("Community analytics synced successfully");
                    return Ok(new { success = true, message = "Community analytics synced successfully" });
                }
                else
                {
                    _logger.LogError("Failed to sync community analytics");
                    return StatusCode(500, new { success = false, message = "Failed to sync community analytics" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing community analytics");
                return StatusCode(500, new { success = false, message = "Error syncing community analytics", details = ex.Message });
            }
        }
    }
} 