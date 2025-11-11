using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobPostingsController : ControllerBase
    {
        private readonly DevMatchDbContext _context;
        private readonly ILogger<JobPostingsController> _logger;

        public JobPostingsController(DevMatchDbContext context, ILogger<JobPostingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<JobPosting>>> GetJobPostings()
        {
            return await _context.JobPostings
                .Where(j => j.IsActive)
                .OrderByDescending(j => j.PostedAt)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<JobPosting>> GetJobPosting(int id)
        {
            var jobPosting = await _context.JobPostings
                .Include(j => j.JobMatches)
                .ThenInclude(m => m.GitHubProfile)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (jobPosting == null)
            {
                return NotFound();
            }

            return jobPosting;
        }

        [HttpPost]
        public async Task<ActionResult<JobPosting>> CreateJobPosting(JobPosting jobPosting)
        {
            jobPosting.PostedAt = DateTime.UtcNow;
            _context.JobPostings.Add(jobPosting);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetJobPosting), new { id = jobPosting.Id }, jobPosting);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateJobPosting(int id, JobPosting jobPosting)
        {
            if (id != jobPosting.Id)
            {
                return BadRequest();
            }

            _context.Entry(jobPosting).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!JobPostingExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        private bool JobPostingExists(int id)
        {
            return _context.JobPostings.Any(e => e.Id == id);
        }
    }
}
