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

        /// <summary>
        /// Get AI-powered job matches for a GitHub user
        /// </summary>
        /// <param name="username">GitHub username to analyze and match with jobs</param>
        /// <returns>AI analysis of the profile and job matches with scores</returns>
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

        /// <summary>
        /// Get AI analysis of a GitHub profile only (without job matching)
        /// </summary>
        /// <param name="username">GitHub username to analyze</param>
        /// <returns>AI analysis of the GitHub profile</returns>
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
                
                // First get GitHub data, then analyze with AI
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
