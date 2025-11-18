using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.DTOs;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        private readonly IAiService _aiService;
        private readonly ILogger<AiController> _logger;

        public AiController(IAiService aiService, ILogger<AiController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        [HttpGet("match/{username}")]
        public async Task<ActionResult<JobMatchResponseDto>> GetJobMatches(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return BadRequest(new { message = "Username cannot be empty" });
                }

                _logger.LogInformation($"Getting AI job matches for username: {username}");
                
                var jobMatches = await _aiService.GetJobMatchesAsync(username);
                return Ok(jobMatches);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, $"Invalid username provided: {username}");
                return NotFound(new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"External API error for username: {username}");
                return BadRequest(new { message = "External API error. Please try again later." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error getting job matches for username: {username}");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
        }

        [HttpGet("analyze/{username}")]
        public async Task<ActionResult<AiAnalysisResponseDto>> AnalyzeProfile(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return BadRequest(new { message = "Username cannot be empty" });
                }

                _logger.LogInformation($"Getting AI analysis for username: {username}");
                
                var gitHubService = HttpContext.RequestServices.GetService<IGitHubService>();
                if (gitHubService == null)
                {
                    return StatusCode(500, new { message = "GitHub service not available" });
                }

                var gitHubData = await gitHubService.AnalyzeProfileAsync(username);
                var aiAnalysis = await _aiService.AnalyzeProfileAsync(username, gitHubData);
                
                return Ok(aiAnalysis);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, $"Invalid username provided: {username}");
                return NotFound(new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"External API error for username: {username}");
                return BadRequest(new { message = "External API error. Please try again later." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error analyzing profile for username: {username}");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
        }
    }
}
