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

        /// <summary>
        /// Search jobs from Jooble API
        /// </summary>
        /// <param name="keywords">Search keywords</param>
        /// <param name="location">Job location</param>
        /// <param name="page">Page number</param>
        /// <returns>List of job postings from Jooble</returns>
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

        /// <summary>
        /// Get tech jobs from Jooble API
        /// </summary>
        /// <param name="location">Job location</param>
        /// <param name="page">Page number</param>
        /// <returns>List of tech job postings from Jooble</returns>
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
