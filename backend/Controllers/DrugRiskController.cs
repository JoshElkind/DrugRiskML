using DrugRiskAPI.DTOs;
using DrugRiskAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DrugRiskAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DrugRiskController : ControllerBase
    {
        private readonly IDrugRiskService _drugRiskService;
        private readonly ILogger<DrugRiskController> _logger;

        public DrugRiskController(IDrugRiskService drugRiskService, ILogger<DrugRiskController> logger)
        {
            _drugRiskService = drugRiskService;
            _logger = logger;
        }

      
        [HttpPost("assess")]
        public async Task<ActionResult<DrugRiskResponse>> AssessDrugRisk([FromBody] DrugRiskRequest request)
        {
            try
            {
                _logger.LogInformation($"Assessing drug risk for {request.DrugName}");
                var response = await _drugRiskService.AssessDrugRiskAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assessing drug risk");
                return BadRequest(new { error = ex.Message });
            }
        }

       
        [HttpGet("run/{userRunId}")]
        public async Task<ActionResult<DrugRiskResponse>> GetUserRun(int userRunId)
        {
            try
            {
                var response = await _drugRiskService.GetUserRunAsync(userRunId);
                if (response == null)
                    return NotFound();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user run");
                return BadRequest(new { error = ex.Message });
            }
        }

       
        [HttpGet("runs/{userId}")]
        public async Task<ActionResult<List<DrugRiskResponse>>> GetUserRuns(string userId)
        {
            try
            {
                var responses = await _drugRiskService.GetUserRunsAsync(userId);
                return Ok(responses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user runs");
                return BadRequest(new { error = ex.Message });
            }
        }

       
        [HttpGet("analytics/community")]
        public async Task<ActionResult<AnalyticsResponse>> GetCommunityAnalytics([FromQuery] string? drugName = null)
        {
            try
            {
                var response = await _drugRiskService.GetCommunityAnalyticsAsync(drugName);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting community analytics");
                return BadRequest(new { error = ex.Message });
            }
        }

      
        [HttpGet("analytics/user/{userId}")]
        public async Task<ActionResult<AnalyticsResponse>> GetUserAnalytics(string userId)
        {
            try
            {
                var response = await _drugRiskService.GetUserAnalyticsAsync(userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user analytics");
                return BadRequest(new { error = ex.Message });
            }
        }
    }
} 