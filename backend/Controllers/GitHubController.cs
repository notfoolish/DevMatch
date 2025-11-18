using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.DTOs;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GitHubController : ControllerBase
    {
        private readonly IGitHubService _gitHubService;
        private readonly ILogger<GitHubController> _logger;

        public GitHubController(IGitHubService gitHubService, ILogger<GitHubController> logger)
        {
            _gitHubService = gitHubService;
            _logger = logger;
        }

        [HttpGet("{username}")]
        public async Task<ActionResult<GitHubAnalysisResponseDto>> AnalyzeProfile(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return BadRequest(new { message = "Username cannot be empty" });
                }

                _logger.LogInformation($"Analyzing GitHub profile for username: {username}");
                
                var analysis = await _gitHubService.AnalyzeProfileAsync(username);
                return Ok(analysis);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, $"Invalid username provided: {username}");
                return NotFound(new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"GitHub API error for username: {username}");
                return BadRequest(new { message = "GitHub API error. Please try again later." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error analyzing profile for username: {username}");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
        }

        [HttpHead("{username}")]
        public async Task<ActionResult> CheckUserExists(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return BadRequest();
                }

                await _gitHubService.AnalyzeProfileAsync(username);
                return Ok();
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
            catch
            {
                return StatusCode(500);
            }
        }
    }
}
