using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.DTOs;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JoobleController : ControllerBase
    {
        private readonly IJoobleService _joobleService;
        private readonly ILogger<JoobleController> _logger;

        public JoobleController(IJoobleService joobleService, ILogger<JoobleController> logger)
        {
            _joobleService = joobleService;
            _logger = logger;
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<JobPostingDto>>> SearchJobs(
            [FromQuery] string keywords = "", 
            [FromQuery] string location = "", 
            [FromQuery] int page = 1)
        {
            try
            {
                _logger.LogInformation($"Searching Jooble jobs with keywords: '{keywords}', location: '{location}', page: {page}");
                
                var jobs = await _joobleService.SearchJobsAsync(keywords, location, page);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching jobs from Jooble");
                return StatusCode(500, new { message = "An error occurred while searching jobs" });
            }
        }

        [HttpGet("tech")]
        public async Task<ActionResult<List<JobPostingDto>>> GetTechJobs(
            [FromQuery] string location = "", 
            [FromQuery] int page = 1)
        {
            try
            {
                _logger.LogInformation($"Fetching tech jobs from Jooble for location: '{location}', page: {page}");
                
                var jobs = await _joobleService.GetTechJobsAsync(location, page);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tech jobs from Jooble");
                return StatusCode(500, new { message = "An error occurred while fetching tech jobs" });
            }
        }
    }
}
