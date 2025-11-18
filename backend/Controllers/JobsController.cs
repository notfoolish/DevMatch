using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.DTOs;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly IJobService _jobService;
        private readonly ILogger<JobsController> _logger;

        public JobsController(IJobService jobService, ILogger<JobsController> logger)
        {
            _jobService = jobService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<JobPostingDto>>> GetActiveJobs([FromQuery] string? location = null)
        {
            try
            {
                _logger.LogInformation($"Retrieving all active job postings for location: {location ?? "all"}");
                
                var jobs = await _jobService.GetActiveJobsAsync(location);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active job postings");
                return StatusCode(500, new { message = "An error occurred while retrieving job postings" });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<JobPostingDto>> GetJobById(int id)
        {
            try
            {
                _logger.LogInformation($"Retrieving job posting with ID: {id}");
                
                var job = await _jobService.GetJobByIdAsync(id);
                if (job == null)
                {
                    return NotFound(new { message = $"Job posting with ID {id} not found" });
                }

                return Ok(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving job posting with ID: {id}");
                return StatusCode(500, new { message = "An error occurred while retrieving the job posting" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<JobPostingDto>> CreateJob([FromBody] CreateJobPostingDto createJobDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation($"Creating new job posting: {createJobDto.Title}");
                
                var createdJob = await _jobService.CreateJobAsync(createJobDto);
                return CreatedAtAction(nameof(GetJobById), new { id = createdJob.Id }, createdJob);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating job posting: {createJobDto.Title}");
                return StatusCode(500, new { message = "An error occurred while creating the job posting" });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<JobPostingDto>> UpdateJob(int id, [FromBody] CreateJobPostingDto updateJobDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation($"Updating job posting with ID: {id}");
                
                var updatedJob = await _jobService.UpdateJobAsync(id, updateJobDto);
                if (updatedJob == null)
                {
                    return NotFound(new { message = $"Job posting with ID {id} not found" });
                }

                return Ok(updatedJob);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating job posting with ID: {id}");
                return StatusCode(500, new { message = "An error occurred while updating the job posting" });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteJob(int id)
        {
            try
            {
                _logger.LogInformation($"Deleting job posting with ID: {id}");
                
                var success = await _jobService.DeleteJobAsync(id);
                if (!success)
                {
                    return NotFound(new { message = $"Job posting with ID {id} not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting job posting with ID: {id}");
                return StatusCode(500, new { message = "An error occurred while deleting the job posting" });
            }
        }
    }
}
