using backend.DTOs;
using backend.Models;
using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public interface IJobService
    {
        Task<List<JobPostingDto>> GetActiveJobsAsync();
        Task<JobPostingDto?> GetJobByIdAsync(int id);
        Task<JobPostingDto> CreateJobAsync(CreateJobPostingDto createJobDto);
        Task<JobPostingDto?> UpdateJobAsync(int id, CreateJobPostingDto updateJobDto);
        Task<bool> DeleteJobAsync(int id);
    }

    public class JobService : IJobService
    {
        private readonly DevMatchDbContext _context;
        private readonly ILogger<JobService> _logger;

        public JobService(DevMatchDbContext context, ILogger<JobService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<JobPostingDto>> GetActiveJobsAsync()
        {
            try
            {
                var jobs = await _context.JobPostings
                    .Where(j => j.IsActive && (j.ExpiresAt == null || j.ExpiresAt > DateTime.UtcNow))
                    .OrderByDescending(j => j.PostedAt)
                    .ToListAsync();

                return jobs.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active jobs");
                // Return some sample jobs if database is not available
                return GetSampleJobs();
            }
        }

        public async Task<JobPostingDto?> GetJobByIdAsync(int id)
        {
            try
            {
                var job = await _context.JobPostings
                    .FirstOrDefaultAsync(j => j.Id == id);

                return job != null ? MapToDto(job) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving job with ID {id}");
                return null;
            }
        }

        public async Task<JobPostingDto> CreateJobAsync(CreateJobPostingDto createJobDto)
        {
            try
            {
                var jobPosting = new JobPosting
                {
                    Title = createJobDto.Title,
                    Company = createJobDto.Company,
                    Location = createJobDto.Location,
                    Description = createJobDto.Description,
                    Requirements = string.Join(",", createJobDto.RequiredSkills),
                    TechStack = string.Join(",", createJobDto.PreferredSkills),
                    ExperienceLevel = createJobDto.ExperienceLevel,
                    SalaryMin = createJobDto.SalaryMin,
                    SalaryMax = createJobDto.SalaryMax,
                    IsRemote = createJobDto.RemoteOptions == "Remote",
                    PostedAt = DateTime.UtcNow,
                    ExpiresAt = createJobDto.ExpiresAt,
                    IsActive = true
                };

                _context.JobPostings.Add(jobPosting);
                await _context.SaveChangesAsync();

                return MapToDto(jobPosting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new job posting");
                throw;
            }
        }

        public async Task<JobPostingDto?> UpdateJobAsync(int id, CreateJobPostingDto updateJobDto)
        {
            try
            {
                var existingJob = await _context.JobPostings.FindAsync(id);
                if (existingJob == null)
                    return null;

                existingJob.Title = updateJobDto.Title;
                existingJob.Company = updateJobDto.Company;
                existingJob.Location = updateJobDto.Location;
                existingJob.Description = updateJobDto.Description;
                existingJob.Requirements = string.Join(",", updateJobDto.RequiredSkills);
                existingJob.TechStack = string.Join(",", updateJobDto.PreferredSkills);
                existingJob.ExperienceLevel = updateJobDto.ExperienceLevel;
                existingJob.SalaryMin = updateJobDto.SalaryMin;
                existingJob.SalaryMax = updateJobDto.SalaryMax;
                existingJob.IsRemote = updateJobDto.RemoteOptions == "Remote";
                existingJob.ExpiresAt = updateJobDto.ExpiresAt;

                await _context.SaveChangesAsync();
                return MapToDto(existingJob);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating job with ID {id}");
                throw;
            }
        }

        public async Task<bool> DeleteJobAsync(int id)
        {
            try
            {
                var job = await _context.JobPostings.FindAsync(id);
                if (job == null)
                    return false;

                job.IsActive = false;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting job with ID {id}");
                return false;
            }
        }

        private JobPostingDto MapToDto(JobPosting job)
        {
            return new JobPostingDto
            {
                Id = job.Id,
                Title = job.Title,
                Company = job.Company,
                Location = job.Location,
                Description = job.Description,
                RequiredSkills = !string.IsNullOrEmpty(job.Requirements) 
                    ? job.Requirements.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() 
                    : new List<string>(),
                PreferredSkills = !string.IsNullOrEmpty(job.TechStack)
                    ? job.TechStack.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                    : new List<string>(),
                ExperienceLevel = job.ExperienceLevel,
                SalaryMin = job.SalaryMin,
                SalaryMax = job.SalaryMax,
                RemoteOptions = job.IsRemote ? "Remote" : "On-site",
                PostedAt = job.PostedAt,
                ExpiresAt = job.ExpiresAt,
                IsActive = job.IsActive
            };
        }

        private List<JobPostingDto> GetSampleJobs()
        {
            return new List<JobPostingDto>
            {
                new JobPostingDto
                {
                    Id = 1,
                    Title = "Full Stack Developer",
                    Company = "TechCorp Inc.",
                    Location = "San Francisco, CA",
                    Description = "Join our team as a Full Stack Developer working with React, Node.js, and cloud technologies.",
                    RequiredSkills = new List<string> { "JavaScript", "React", "Node.js", "MongoDB", "Git" },
                    PreferredSkills = new List<string> { "TypeScript", "AWS", "Docker" },
                    ExperienceLevel = "Mid",
                    SalaryMin = 90000,
                    SalaryMax = 130000,
                    RemoteOptions = "Hybrid",
                    PostedAt = DateTime.UtcNow.AddDays(-5),
                    ExpiresAt = DateTime.UtcNow.AddDays(25),
                    IsActive = true
                },
                new JobPostingDto
                {
                    Id = 2,
                    Title = "Python Data Scientist",
                    Company = "DataTech Solutions",
                    Location = "Remote",
                    Description = "Looking for a Python Data Scientist to work on machine learning projects and data analysis.",
                    RequiredSkills = new List<string> { "Python", "Pandas", "NumPy", "Scikit-learn", "SQL" },
                    PreferredSkills = new List<string> { "TensorFlow", "PyTorch", "Docker", "Kubernetes" },
                    ExperienceLevel = "Senior",
                    SalaryMin = 120000,
                    SalaryMax = 160000,
                    RemoteOptions = "Remote",
                    PostedAt = DateTime.UtcNow.AddDays(-3),
                    ExpiresAt = DateTime.UtcNow.AddDays(27),
                    IsActive = true
                },
                new JobPostingDto
                {
                    Id = 3,
                    Title = "Frontend React Developer",
                    Company = "StartupXYZ",
                    Location = "Austin, TX",
                    Description = "Seeking a Frontend Developer specialized in React to build modern web applications.",
                    RequiredSkills = new List<string> { "JavaScript", "React", "HTML5", "CSS3", "REST APIs" },
                    PreferredSkills = new List<string> { "TypeScript", "Redux", "Webpack", "Jest" },
                    ExperienceLevel = "Junior",
                    SalaryMin = 70000,
                    SalaryMax = 95000,
                    RemoteOptions = "On-site",
                    PostedAt = DateTime.UtcNow.AddDays(-2),
                    ExpiresAt = DateTime.UtcNow.AddDays(28),
                    IsActive = true
                },
                new JobPostingDto
                {
                    Id = 4,
                    Title = "DevOps Engineer",
                    Company = "CloudFirst Technologies",
                    Location = "Seattle, WA",
                    Description = "Join our DevOps team to manage cloud infrastructure and CI/CD pipelines.",
                    RequiredSkills = new List<string> { "AWS", "Docker", "Kubernetes", "Terraform", "Jenkins" },
                    PreferredSkills = new List<string> { "Python", "Go", "Ansible", "Prometheus" },
                    ExperienceLevel = "Mid",
                    SalaryMin = 110000,
                    SalaryMax = 145000,
                    RemoteOptions = "Hybrid",
                    PostedAt = DateTime.UtcNow.AddDays(-1),
                    ExpiresAt = DateTime.UtcNow.AddDays(29),
                    IsActive = true
                },
                new JobPostingDto
                {
                    Id = 5,
                    Title = "C# Backend Developer",
                    Company = "Enterprise Systems Ltd.",
                    Location = "New York, NY",
                    Description = "Looking for an experienced C# developer to work on enterprise-grade backend systems.",
                    RequiredSkills = new List<string> { "C#", ".NET Core", "ASP.NET", "SQL Server", "Web APIs" },
                    PreferredSkills = new List<string> { "Azure", "Entity Framework", "Microservices", "Redis" },
                    ExperienceLevel = "Senior",
                    SalaryMin = 115000,
                    SalaryMax = 155000,
                    RemoteOptions = "Hybrid",
                    PostedAt = DateTime.UtcNow.AddDays(-4),
                    ExpiresAt = DateTime.UtcNow.AddDays(26),
                    IsActive = true
                }
            };
        }
    }
}
