using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GitHubProfilesController : ControllerBase
    {
        private readonly DevMatchDbContext _context;
        private readonly ILogger<GitHubProfilesController> _logger;

        public GitHubProfilesController(DevMatchDbContext context, ILogger<GitHubProfilesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<GitHubProfile>>> GetGitHubProfiles()
        {
            return await _context.GitHubProfiles.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GitHubProfile>> GetGitHubProfile(int id)
        {
            var profile = await _context.GitHubProfiles
                .Include(p => p.JobMatches)
                .ThenInclude(m => m.JobPosting)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile == null)
            {
                return NotFound();
            }

            return profile;
        }

        [HttpPost]
        public async Task<ActionResult<GitHubProfile>> CreateGitHubProfile(GitHubProfile profile)
        {
            profile.AnalyzedAt = DateTime.UtcNow;
            _context.GitHubProfiles.Add(profile);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGitHubProfile), new { id = profile.Id }, profile);
        }

        [HttpPost("{username}/analyze")]
        public async Task<ActionResult<GitHubProfile>> AnalyzeGitHubProfile(string username)
        {
            try
            {
                // Check if profile already exists
                var existingProfile = await _context.GitHubProfiles
                    .FirstOrDefaultAsync(p => p.GitHubUsername == username);

                if (existingProfile != null)
                {
                    return Ok(existingProfile);
                }

                // TODO: Implement GitHub API call and AI analysis
                // For now, return a placeholder
                var profile = new GitHubProfile
                {
                    GitHubUsername = username,
                    FullName = $"User {username}",
                    Bio = "Placeholder profile - GitHub API integration needed",
                    AnalyzedAt = DateTime.UtcNow
                };

                _context.GitHubProfiles.Add(profile);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetGitHubProfile), new { id = profile.Id }, profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing GitHub profile for username: {Username}", username);
                return StatusCode(500, "An error occurred while analyzing the GitHub profile");
            }
        }
    }
}
