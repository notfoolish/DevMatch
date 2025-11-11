using backend.DTOs;
using backend.Models;
using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public interface IJobService
    {
        Task<List<JobPostingDto>> GetActiveJobsAsync(string? userLocation = null);
        Task<JobPostingDto?> GetJobByIdAsync(int id);
        Task<JobPostingDto> CreateJobAsync(CreateJobPostingDto createJobDto);
        Task<JobPostingDto?> UpdateJobAsync(int id, CreateJobPostingDto updateJobDto);
        Task<bool> DeleteJobAsync(int id);
    }

    public class JobService : IJobService
    {
        private readonly DevMatchDbContext _context;
        private readonly IJoobleService _joobleService;
        private readonly ILogger<JobService> _logger;

        public JobService(DevMatchDbContext context, IJoobleService joobleService, ILogger<JobService> logger)
        {
            _context = context;
            _joobleService = joobleService;
            _logger = logger;
        }

        public async Task<List<JobPostingDto>> GetActiveJobsAsync(string? userLocation = null)
        {
            try
            {
                var allJobs = new List<JobPostingDto>();

                // Get jobs from local database
                try
                {
                    var dbJobs = await _context.JobPostings
                        .Where(j => j.IsActive && (j.ExpiresAt == null || j.ExpiresAt > DateTime.UtcNow))
                        .OrderByDescending(j => j.PostedAt)
                        .ToListAsync();

                    allJobs.AddRange(dbJobs.Select(MapToDto));
                    _logger.LogInformation($"Retrieved {dbJobs.Count} jobs from database");
                }
                catch (Exception dbEx)
                {
                    _logger.LogWarning(dbEx, "Failed to retrieve jobs from database, continuing with external sources");
                }

                // Get jobs from Jooble API - use location-based search
                try
                {
                    var searchLocation = DetermineSearchLocation(userLocation);
                    var joobleJobs = await _joobleService.GetTechJobsAsync(searchLocation);
                    allJobs.AddRange(joobleJobs);
                    _logger.LogInformation($"Retrieved {joobleJobs.Count} jobs from Jooble for location: {searchLocation}");
                }
                catch (Exception joobleEx)
                {
                    _logger.LogWarning(joobleEx, "Failed to retrieve jobs from Jooble API");
                }

                // If no jobs from any source, return sample jobs
                if (!allJobs.Any())
                {
                    _logger.LogInformation("No jobs found from any source, returning sample jobs");
                    return GetSampleJobs();
                }

                // Remove duplicates and return
                return allJobs
                    .GroupBy(j => new { j.Title, j.Company })
                    .Select(g => g.First())
                    .OrderByDescending(j => j.PostedAt)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active jobs");
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

        private string DetermineSearchLocation(string? userLocation)
        {
            if (string.IsNullOrEmpty(userLocation))
                return "remote"; // Default to remote jobs if no location specified

            // Extract country/region from user location
            var location = userLocation.ToLower().Trim();
            
            // Map common location patterns to search terms that work well with Jooble
            var locationMappings = new Dictionary<string, string>
            {
                // European countries - use country names for better results
                { "hungary", "Hungary" },
                { "budapest", "Hungary" },
                { "debrecen", "Hungary" },
                { "szeged", "Hungary" },
                { "pécs", "Hungary" },
                
                { "germany", "Germany" },
                { "berlin", "Germany" },
                { "munich", "Germany" },
                { "hamburg", "Germany" },
                
                { "united kingdom", "United Kingdom" },
                { "uk", "United Kingdom" },
                { "london", "United Kingdom" },
                { "manchester", "United Kingdom" },
                
                { "france", "France" },
                { "paris", "France" },
                { "lyon", "France" },
                
                { "netherlands", "Netherlands" },
                { "amsterdam", "Netherlands" },
                { "rotterdam", "Netherlands" },
                
                { "poland", "Poland" },
                { "warsaw", "Poland" },
                { "krakow", "Poland" },
                { "kraków", "Poland" },
                
                { "spain", "Spain" },
                { "madrid", "Spain" },
                { "barcelona", "Spain" },
                
                { "italy", "Italy" },
                { "rome", "Italy" },
                { "milan", "Italy" },
                
                // North America
                { "united states", "United States" },
                { "usa", "United States" },
                { "us", "United States" },
                { "california", "California" },
                { "san francisco", "San Francisco" },
                { "los angeles", "Los Angeles" },
                { "new york", "New York" },
                { "seattle", "Seattle" },
                { "austin", "Austin" },
                
                { "canada", "Canada" },
                { "toronto", "Toronto" },
                { "vancouver", "Vancouver" },
                { "montreal", "Montreal" },
                
                // Other regions
                { "australia", "Australia" },
                { "sydney", "Sydney" },
                { "melbourne", "Melbourne" },
                
                { "india", "India" },
                { "bangalore", "Bangalore" },
                { "mumbai", "Mumbai" },
                { "delhi", "Delhi" },
                
                { "singapore", "Singapore" },
                { "japan", "Japan" },
                { "tokyo", "Tokyo" },
            };

            // Try exact matches first
            foreach (var mapping in locationMappings)
            {
                if (location.Contains(mapping.Key))
                {
                    return mapping.Value;
                }
            }

            // If no mapping found, try to extract the first word (might be a country)
            var words = userLocation.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 0)
            {
                var firstLocation = words[^1].Trim(); // Get last part (usually country)
                return firstLocation;
            }

            // Fallback to the original location cleaned up
            return userLocation.Trim();
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
                    Title = "Full Stack JavaScript Developer",
                    Company = "TechCorp Inc.",
                    Location = "San Francisco, CA",
                    Description = "Join our team as a Full Stack Developer working with React, Node.js, and modern web technologies. Build scalable applications and work with a dynamic team.",
                    RequiredSkills = new List<string> { "JavaScript", "React", "Node.js", "HTML", "CSS" },
                    PreferredSkills = new List<string> { "TypeScript", "AWS", "Docker", "GraphQL" },
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
                    Title = "Python Backend Developer",
                    Company = "DataTech Solutions",
                    Location = "Remote",
                    Description = "Looking for a Python Backend Developer to work on scalable web applications and APIs using Django/Flask.",
                    RequiredSkills = new List<string> { "Python", "Django", "PostgreSQL", "REST API", "Git" },
                    PreferredSkills = new List<string> { "Flask", "Docker", "Kubernetes", "AWS" },
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
                    Title = "React Frontend Developer",
                    Company = "StartupXYZ",
                    Location = "Austin, TX",
                    Description = "Seeking a Frontend Developer specialized in React to build modern, responsive web applications with great UX.",
                    RequiredSkills = new List<string> { "JavaScript", "React", "HTML5", "CSS3", "Redux" },
                    PreferredSkills = new List<string> { "TypeScript", "Next.js", "Tailwind CSS", "Jest" },
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
                    Title = "Mobile App Developer (React Native)",
                    Company = "MobileTech",
                    Location = "Budapest, Hungary",
                    Description = "Join our mobile development team to create cross-platform applications using React Native and modern mobile technologies.",
                    RequiredSkills = new List<string> { "React Native", "JavaScript", "Mobile Development", "iOS", "Android" },
                    PreferredSkills = new List<string> { "TypeScript", "Redux", "Firebase", "App Store Deployment" },
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
                    Title = "C# .NET Developer",
                    Company = "Enterprise Systems Ltd.",
                    Location = "New York, NY",
                    Description = "Looking for an experienced C# .NET developer to work on enterprise-grade web applications and microservices.",
                    RequiredSkills = new List<string> { "C#", ".NET Core", "ASP.NET", "SQL Server", "Web APIs" },
                    PreferredSkills = new List<string> { "Azure", "Entity Framework", "Microservices", "Docker" },
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
