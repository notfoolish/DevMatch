using System.Text;
using System.Text.Json;
using backend.DTOs;

namespace backend.Services
{
    public interface IJoobleService
    {
        Task<List<JobPostingDto>> SearchJobsAsync(string keywords = "", string location = "", int page = 1);
        Task<List<JobPostingDto>> GetTechJobsAsync(string location = "", int page = 1);
    }

    public class JoobleService : IJoobleService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<JoobleService> _logger;
        private const string JOOBLE_API_URL = "https://jooble.org/api/";

        public JoobleService(HttpClient httpClient, IConfiguration configuration, ILogger<JoobleService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<JobPostingDto>> SearchJobsAsync(string keywords = "", string location = "", int page = 1)
        {
            try
            {
                var apiKey = _configuration["JOOBLE_API_KEY"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("Jooble API key is not configured");
                    return new List<JobPostingDto>();
                }

                var request = new JoobleRequestDto
                {
                    Keywords = keywords,
                    Location = location,
                    Page = page,
                    Radius = 50
                };

                var jsonRequest = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{JOOBLE_API_URL}{apiKey}", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Jooble API request failed with status: {response.StatusCode}");
                    return new List<JobPostingDto>();
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var joobleResponse = JsonSerializer.Deserialize<JoobleResponseDto>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (joobleResponse?.Jobs == null)
                {
                    _logger.LogWarning("No jobs returned from Jooble API");
                    return new List<JobPostingDto>();
                }

                return joobleResponse.Jobs
                    .Select(MapJoobleJobToDto)
                    .Where(job => job != null)
                    .Cast<JobPostingDto>()
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching jobs from Jooble API");
                return new List<JobPostingDto>();
            }
        }

        public async Task<List<JobPostingDto>> GetTechJobsAsync(string location = "", int page = 1)
        {
            // Search specifically for developer/tech roles only
            var devKeywords = "software developer OR web developer OR full stack developer OR frontend developer OR backend developer OR mobile developer OR react developer OR javascript developer OR python developer OR java developer OR .net developer OR php developer OR node.js developer OR angular developer OR vue developer OR software engineer OR programming OR coding";
            return await SearchJobsAsync(devKeywords, location, page);
        }

        private JobPostingDto? MapJoobleJobToDto(JoobleJobDto joobleJob)
        {
            // Filter out non-developer jobs
            if (!IsDeveloperJob(joobleJob.Title, joobleJob.Snippet))
            {
                return null; // Skip non-developer jobs
            }

            // Extract salary information
            var (salaryMin, salaryMax) = ParseSalary(joobleJob.Salary);
            
            // Extract skills from title and description
            var skills = ExtractSkillsFromText($"{joobleJob.Title} {joobleJob.Snippet}");
            
            // Determine experience level from title
            var experienceLevel = DetermineExperienceLevel(joobleJob.Title);

            // Determine if remote from location or title
            var isRemote = IsRemoteJob(joobleJob.Location, joobleJob.Title);

            // Generate a numeric ID from the string ID hash (for consistency with our system)
            var numericId = Math.Abs(joobleJob.Id.GetHashCode());

            return new JobPostingDto
            {
                Id = numericId,
                Title = joobleJob.Title,
                Company = !string.IsNullOrEmpty(joobleJob.Company) ? joobleJob.Company : joobleJob.Source,
                Location = joobleJob.Location,
                Description = joobleJob.Snippet,
                RequiredSkills = skills.Take(5).ToList(), // First 5 as required
                PreferredSkills = skills.Skip(5).Take(5).ToList(), // Next 5 as preferred
                ExperienceLevel = experienceLevel,
                SalaryMin = salaryMin,
                SalaryMax = salaryMax,
                RemoteOptions = isRemote ? "Remote" : "On-site",
                PostedAt = ParseUpdateDate(joobleJob.Updated),
                ExpiresAt = null, // Jooble doesn't provide expiry dates
                IsActive = true
            };
        }

        private bool IsDeveloperJob(string title, string description)
        {
            var devTerms = new[]
            {
                "developer", "programmer", "engineer", "coding", "programming", "software", "web development",
                "frontend", "backend", "full stack", "fullstack", "react", "javascript", "python", "java",
                "c#", ".net", "php", "node.js", "angular", "vue", "mobile app", "android", "ios"
            };

            var combinedText = $"{title} {description}".ToLower();
            
            // Must contain at least one developer term
            var hasDeveloperTerm = devTerms.Any(term => combinedText.Contains(term));
            
            // Exclude non-developer roles
            var excludeTerms = new[]
            {
                "sales", "marketing", "hr", "human resources", "admin", "administration", "manager", 
                "director", "ceo", "cto", "accountant", "finance", "legal", "lawyer", "designer"
            };
            
            var hasExcludeTerm = excludeTerms.Any(term => combinedText.Contains(term) && !combinedText.Contains("software " + term));
            
            return hasDeveloperTerm && !hasExcludeTerm;
        }

        private (decimal?, decimal?) ParseSalary(string salaryText)
        {
            if (string.IsNullOrEmpty(salaryText))
                return (null, null);

            try
            {
                // Remove common currency symbols and text
                var cleanSalary = System.Text.RegularExpressions.Regex.Replace(salaryText, @"[$,€£]|per year|per month|annually", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                // Look for ranges like "50000-80000" or "50k-80k"
                var rangeMatch = System.Text.RegularExpressions.Regex.Match(cleanSalary, @"(\d+(?:\.\d+)?)[k]?\s*-\s*(\d+(?:\.\d+)?)[k]?");
                if (rangeMatch.Success)
                {
                    var min = decimal.Parse(rangeMatch.Groups[1].Value);
                    var max = decimal.Parse(rangeMatch.Groups[2].Value);
                    
                    // Handle 'k' notation
                    if (cleanSalary.Contains("k", StringComparison.OrdinalIgnoreCase))
                    {
                        min *= 1000;
                        max *= 1000;
                    }
                    
                    return (min, max);
                }
                
                // Look for single values
                var singleMatch = System.Text.RegularExpressions.Regex.Match(cleanSalary, @"(\d+(?:\.\d+)?)[k]?");
                if (singleMatch.Success)
                {
                    var salary = decimal.Parse(singleMatch.Groups[1].Value);
                    if (cleanSalary.Contains("k", StringComparison.OrdinalIgnoreCase))
                    {
                        salary *= 1000;
                    }
                    return (salary, salary);
                }
            }
            catch
            {
                // Log parsing error but continue
            }

            return (null, null);
        }

        private List<string> ExtractSkillsFromText(string text)
        {
            var skills = new List<string>();
            var developerSkills = new[]
            {
                // Programming Languages
                "JavaScript", "TypeScript", "Python", "Java", "C#", "C++", "PHP", "Ruby", "Go", "Rust", "Swift", "Kotlin",
                
                // Frontend Technologies
                "React", "Angular", "Vue.js", "Vue", "Svelte", "HTML", "CSS", "SCSS", "SASS", "Bootstrap", "Tailwind",
                
                // Backend Technologies  
                "Node.js", "Express", "Django", "Flask", "Spring", "Laravel", "ASP.NET", ".NET Core", "FastAPI",
                
                // Mobile Development
                "React Native", "Flutter", "Xamarin", "Android", "iOS", "Mobile Development",
                
                // Databases
                "SQL", "MongoDB", "PostgreSQL", "MySQL", "Redis", "SQLite", "NoSQL", "Firebase",
                
                // Cloud & DevOps
                "AWS", "Azure", "GCP", "Docker", "Kubernetes", "Jenkins", "Git", "GitHub", "GitLab",
                
                // Tools & Frameworks
                "GraphQL", "REST API", "Microservices", "Agile", "Scrum", "Test Driven Development", "TDD"
            };

            foreach (var skill in developerSkills)
            {
                if (text.Contains(skill, StringComparison.OrdinalIgnoreCase))
                {
                    skills.Add(skill);
                }
            }

            return skills.Distinct().ToList();
        }

        private string DetermineExperienceLevel(string title)
        {
            var lowerTitle = title.ToLower();
            
            if (lowerTitle.Contains("senior") || lowerTitle.Contains("lead") || lowerTitle.Contains("principal"))
                return "Senior";
            else if (lowerTitle.Contains("junior") || lowerTitle.Contains("entry") || lowerTitle.Contains("graduate"))
                return "Junior";
            else
                return "Mid";
        }

        private bool IsRemoteJob(string location, string title)
        {
            var combinedText = $"{location} {title}".ToLower();
            return combinedText.Contains("remote") || combinedText.Contains("work from home") || combinedText.Contains("wfh");
        }

        private DateTime ParseUpdateDate(string updatedText)
        {
            try
            {
                if (string.IsNullOrEmpty(updatedText))
                    return DateTime.UtcNow;

                // Try to parse various date formats
                if (DateTime.TryParse(updatedText, out var parsedDate))
                    return parsedDate;

                // Handle relative dates like "2 days ago"
                var now = DateTime.UtcNow;
                if (updatedText.Contains("day", StringComparison.OrdinalIgnoreCase))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(updatedText, @"(\d+)\s*day");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var days))
                    {
                        return now.AddDays(-days);
                    }
                }

                return now;
            }
            catch
            {
                return DateTime.UtcNow;
            }
        }
    }
}
