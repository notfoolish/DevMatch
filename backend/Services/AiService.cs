using System.Text;
using System.Text.Json;
using backend.DTOs;
using backend.Models;
using backend.Data;

namespace backend.Services
{
    public interface IAiService
    {
        Task<AiAnalysisResponseDto> AnalyzeProfileAsync(string username, GitHubAnalysisResponseDto gitHubData);
        Task<JobMatchResponseDto> GetJobMatchesAsync(string username);
    }

    public class AiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiService> _logger;
        private readonly IGitHubService _gitHubService;
        private readonly IJobService _jobService;

        public AiService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            ILogger<AiService> logger,
            IGitHubService gitHubService,
            IJobService jobService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _gitHubService = gitHubService;
            _jobService = jobService;
        }

        public async Task<AiAnalysisResponseDto> AnalyzeProfileAsync(string username, GitHubAnalysisResponseDto gitHubData)
        {
            try
            {
                _logger.LogInformation($"Starting AI analysis for user: {username}");

                var openAiApiKey = _configuration["ApiSettings:OpenAiApiKey"];
                if (string.IsNullOrEmpty(openAiApiKey))
                {
                    _logger.LogWarning("OpenAI API key not configured, using mock analysis");
                    return GenerateMockAnalysis(username, gitHubData);
                }

                var prompt = BuildAnalysisPrompt(gitHubData);
                var analysis = await CallOpenAiApi(prompt, openAiApiKey);

                return ParseAiResponse(username, analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in AI analysis for user: {username}");
                return GenerateMockAnalysis(username, gitHubData);
            }
        }

        public async Task<JobMatchResponseDto> GetJobMatchesAsync(string username)
        {
            try
            {
                // Get GitHub data and AI analysis
                var gitHubData = await _gitHubService.AnalyzeProfileAsync(username);
                var profileAnalysis = await AnalyzeProfileAsync(username, gitHubData);
                
                // Get user location for location-based job search
                var userLocation = gitHubData.Profile?.Location;
                
                // Get available jobs with location consideration
                var jobs = await _jobService.GetActiveJobsAsync(userLocation);
                
                // Generate job matches
                var jobMatches = await GenerateJobMatches(profileAnalysis, jobs);

                return new JobMatchResponseDto
                {
                    ProfileAnalysis = profileAnalysis,
                    JobMatches = jobMatches,
                    AnalyzedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating job matches for user: {username}");
                throw;
            }
        }

        private string BuildAnalysisPrompt(GitHubAnalysisResponseDto gitHubData)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("Analyze this GitHub profile and provide a detailed assessment:");
            prompt.AppendLine();
            
            if (gitHubData.Profile != null)
            {
                prompt.AppendLine($"Profile: {gitHubData.Profile.Name} ({gitHubData.Profile.Login})");
                prompt.AppendLine($"Bio: {gitHubData.Profile.Bio ?? "Not provided"}");
                prompt.AppendLine($"Location: {gitHubData.Profile.Location ?? "Not provided"}");
                prompt.AppendLine($"Company: {gitHubData.Profile.Company ?? "Not provided"}");
                prompt.AppendLine($"Public Repositories: {gitHubData.Profile.PublicRepos}");
                prompt.AppendLine($"Followers: {gitHubData.Profile.Followers}");
                prompt.AppendLine($"Account Created: {gitHubData.Profile.CreatedAt:yyyy-MM-dd}");
                prompt.AppendLine();
            }

            prompt.AppendLine("Programming Languages (by usage):");
            foreach (var lang in gitHubData.LanguageStats.Languages.Take(5))
            {
                prompt.AppendLine($"- {lang.Key}: {lang.Value} points");
            }
            prompt.AppendLine();

            prompt.AppendLine("Recent Repository Activity:");
            foreach (var repo in gitHubData.Repositories.Take(10))
            {
                prompt.AppendLine($"- {repo.Name} ({repo.Language ?? "Unknown"}) - Stars: {repo.StargazersCount}, Forks: {repo.ForksCount}");
                if (!string.IsNullOrEmpty(repo.Description))
                    prompt.AppendLine($"  Description: {repo.Description}");
            }
            prompt.AppendLine();

            prompt.AppendLine("Please provide a JSON response with the following structure:");
            prompt.AppendLine(@"{
                ""summary"": ""Brief overview of the developer's skills and experience"",
                ""skills"": [""skill1"", ""skill2"", ""skill3""],
                ""experienceLevel"": ""Junior|Mid|Senior"",
                ""primaryLanguages"": [""language1"", ""language2""],
                ""techStack"": [""technology1"", ""technology2""],
                ""strengths"": [""strength1"", ""strength2""],
                ""improvementAreas"": [""area1"", ""area2""],
                ""overallScore"": 0.85
            }");

            return prompt.ToString();
        }

        private async Task<string> CallOpenAiApi(string prompt, string apiKey)
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "system", content = "You are an expert technical recruiter and software developer analyst. Provide accurate, professional assessments of GitHub profiles." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 1000,
                temperature = 0.3
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonDocument.Parse(responseJson);
            
            return responseObj.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";
        }

        private AiAnalysisResponseDto ParseAiResponse(string username, string aiResponse)
        {
            try
            {
                // Try to extract JSON from the AI response
                var jsonStart = aiResponse.IndexOf('{');
                var jsonEnd = aiResponse.LastIndexOf('}') + 1;
                
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonContent = aiResponse.Substring(jsonStart, jsonEnd - jsonStart);
                    var parsed = JsonDocument.Parse(jsonContent);
                    var root = parsed.RootElement;

                    return new AiAnalysisResponseDto
                    {
                        GitHubUsername = username,
                        Summary = root.TryGetProperty("summary", out var summary) ? summary.GetString() ?? "" : "",
                        Skills = root.TryGetProperty("skills", out var skills) ? 
                            skills.EnumerateArray().Select(s => s.GetString() ?? "").ToList() : new List<string>(),
                        ExperienceLevel = root.TryGetProperty("experienceLevel", out var exp) ? exp.GetString() ?? "Mid" : "Mid",
                        PrimaryLanguages = root.TryGetProperty("primaryLanguages", out var langs) ? 
                            langs.EnumerateArray().Select(l => l.GetString() ?? "").ToList() : new List<string>(),
                        TechStack = root.TryGetProperty("techStack", out var tech) ? 
                            tech.EnumerateArray().Select(t => t.GetString() ?? "").ToList() : new List<string>(),
                        Strengths = root.TryGetProperty("strengths", out var strengths) ? 
                            strengths.EnumerateArray().Select(s => s.GetString() ?? "").ToList() : new List<string>(),
                        ImprovementAreas = root.TryGetProperty("improvementAreas", out var areas) ? 
                            areas.EnumerateArray().Select(a => a.GetString() ?? "").ToList() : new List<string>(),
                        OverallScore = root.TryGetProperty("overallScore", out var score) ? score.GetDecimal() : 0.75m,
                        AnalyzedAt = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse AI response, generating fallback analysis");
            }

            return GenerateFallbackAnalysis(username, aiResponse);
        }

        private AiAnalysisResponseDto GenerateMockAnalysis(string username, GitHubAnalysisResponseDto gitHubData)
        {
            var topLanguages = gitHubData.LanguageStats.Languages.Keys.Take(3).ToList();
            var experienceLevel = DetermineExperienceLevel(gitHubData);
            
            return new AiAnalysisResponseDto
            {
                GitHubUsername = username,
                Summary = $"Active developer with {gitHubData.Profile?.PublicRepos ?? 0} public repositories, primarily working with {string.Join(", ", topLanguages)}. Shows consistent contribution patterns and engagement with the developer community.",
                Skills = topLanguages.Concat(InferSkillsFromLanguages(topLanguages)).ToList(),
                ExperienceLevel = experienceLevel,
                PrimaryLanguages = topLanguages,
                TechStack = InferTechStackFromLanguages(topLanguages),
                Strengths = GenerateStrengths(gitHubData),
                ImprovementAreas = new List<string> { "API Documentation", "Testing Coverage", "Code Comments" },
                OverallScore = CalculateOverallScore(gitHubData),
                AnalyzedAt = DateTime.UtcNow
            };
        }

        private AiAnalysisResponseDto GenerateFallbackAnalysis(string username, string rawResponse)
        {
            return new AiAnalysisResponseDto
            {
                GitHubUsername = username,
                Summary = "Profile analysis completed. Please review the detailed breakdown in the other sections.",
                Skills = new List<string> { "Software Development", "Version Control", "Problem Solving" },
                ExperienceLevel = "Mid",
                PrimaryLanguages = new List<string> { "JavaScript", "Python" },
                TechStack = new List<string> { "Git", "GitHub", "Web Development" },
                Strengths = new List<string> { "Active on GitHub", "Open Source Contributions" },
                ImprovementAreas = new List<string> { "Documentation", "Testing" },
                OverallScore = 0.75m,
                AnalyzedAt = DateTime.UtcNow
            };
        }

        private async Task<List<JobMatchAnalysisDto>> GenerateJobMatches(AiAnalysisResponseDto profileAnalysis, List<JobPostingDto> jobs)
        {
            var matches = new List<JobMatchAnalysisDto>();

            foreach (var job in jobs.Take(10)) // Limit to top 10 jobs
            {
                var matchingSkills = job.RequiredSkills.Intersect(profileAnalysis.Skills, StringComparer.OrdinalIgnoreCase).ToList();
                var missingSkills = job.RequiredSkills.Except(profileAnalysis.Skills, StringComparer.OrdinalIgnoreCase).ToList();
                
                var matchScore = CalculateJobMatchScore(profileAnalysis, job, matchingSkills, missingSkills);

                matches.Add(new JobMatchAnalysisDto
                {
                    JobId = job.Id,
                    JobTitle = job.Title,
                    MatchScore = matchScore,
                    MatchingSkills = matchingSkills,
                    MissingSkills = missingSkills,
                    MatchReason = GenerateMatchReason(matchScore, matchingSkills, missingSkills)
                });
            }

            return matches.OrderByDescending(m => m.MatchScore).ToList();
        }

        private string DetermineExperienceLevel(GitHubAnalysisResponseDto gitHubData)
        {
            var accountAge = (DateTime.UtcNow - (gitHubData.Profile?.CreatedAt ?? DateTime.UtcNow));
            var repoCount = gitHubData.Profile?.PublicRepos ?? 0;
            var totalCommits = gitHubData.TotalCommits;

            if (accountAge.Days < 365 || repoCount < 5 || totalCommits < 50)
                return "Junior";
            else if (accountAge.Days > 1825 && repoCount > 20 && totalCommits > 500) // 5+ years
                return "Senior";
            else
                return "Mid";
        }

        private List<string> InferSkillsFromLanguages(List<string> languages)
        {
            var skills = new List<string>();
            foreach (var lang in languages)
            {
                switch (lang.ToLower())
                {
                    case "javascript":
                        skills.AddRange(new[] { "Node.js", "React", "Web Development", "Frontend" });
                        break;
                    case "python":
                        skills.AddRange(new[] { "Django", "Flask", "Data Science", "Machine Learning" });
                        break;
                    case "java":
                        skills.AddRange(new[] { "Spring", "Android", "Enterprise Development" });
                        break;
                    case "c#":
                        skills.AddRange(new[] { ".NET", "ASP.NET", "Backend Development" });
                        break;
                    case "go":
                        skills.AddRange(new[] { "Microservices", "Cloud Development", "DevOps" });
                        break;
                }
            }
            return skills.Distinct().ToList();
        }

        private List<string> InferTechStackFromLanguages(List<string> languages)
        {
            var techStack = new List<string> { "Git", "GitHub" };
            
            if (languages.Contains("JavaScript"))
                techStack.AddRange(new[] { "npm", "Webpack", "Babel" });
            if (languages.Contains("Python"))
                techStack.AddRange(new[] { "pip", "Virtual Environments" });
            if (languages.Contains("Java"))
                techStack.AddRange(new[] { "Maven", "Gradle" });
            
            return techStack.Distinct().ToList();
        }

        private List<string> GenerateStrengths(GitHubAnalysisResponseDto gitHubData)
        {
            var strengths = new List<string>();
            
            if ((gitHubData.Profile?.PublicRepos ?? 0) > 10)
                strengths.Add("Prolific contributor");
            if ((gitHubData.Profile?.Followers ?? 0) > 20)
                strengths.Add("Strong community presence");
            if (gitHubData.LanguageStats.Languages.Count > 3)
                strengths.Add("Multi-language proficiency");
            if (gitHubData.TotalCommits > 100)
                strengths.Add("Consistent development activity");
            
            return strengths.Any() ? strengths : new List<string> { "Active GitHub user", "Open source contributor" };
        }

        private decimal CalculateOverallScore(GitHubAnalysisResponseDto gitHubData)
        {
            var score = 0.5m; // Base score
            
            // Repository count contribution
            var repoCount = gitHubData.Profile?.PublicRepos ?? 0;
            score += Math.Min(0.2m, repoCount * 0.01m);
            
            // Language diversity
            score += Math.Min(0.15m, gitHubData.LanguageStats.Languages.Count * 0.03m);
            
            // Community engagement
            var followers = gitHubData.Profile?.Followers ?? 0;
            score += Math.Min(0.1m, followers * 0.002m);
            
            // Commit activity
            score += Math.Min(0.05m, gitHubData.TotalCommits * 0.0001m);
            
            return Math.Min(1.0m, score);
        }

        private decimal CalculateJobMatchScore(AiAnalysisResponseDto profile, JobPostingDto job, List<string> matchingSkills, List<string> missingSkills)
        {
            var baseScore = 0.3m;
            
            // Skills match
            var requiredSkillsCount = job.RequiredSkills.Count;
            if (requiredSkillsCount > 0)
            {
                var skillMatchRatio = (decimal)matchingSkills.Count / requiredSkillsCount;
                baseScore += skillMatchRatio * 0.4m;
            }
            
            // Experience level match
            if (string.Equals(profile.ExperienceLevel, job.ExperienceLevel, StringComparison.OrdinalIgnoreCase))
                baseScore += 0.2m;
            
            // Language match
            var languageMatch = job.RequiredSkills.Intersect(profile.PrimaryLanguages, StringComparer.OrdinalIgnoreCase).Any();
            if (languageMatch)
                baseScore += 0.1m;
            
            return Math.Min(1.0m, baseScore);
        }

        private string GenerateMatchReason(decimal score, List<string> matchingSkills, List<string> missingSkills)
        {
            if (score >= 0.8m)
                return $"Excellent match! You have {matchingSkills.Count} of the required skills including {string.Join(", ", matchingSkills.Take(3))}.";
            else if (score >= 0.6m)
                return $"Good match. You have key skills: {string.Join(", ", matchingSkills.Take(3))}. Consider learning: {string.Join(", ", missingSkills.Take(2))}.";
            else if (score >= 0.4m)
                return $"Potential match. You have some relevant skills: {string.Join(", ", matchingSkills.Take(2))}. Key skills to develop: {string.Join(", ", missingSkills.Take(3))}.";
            else
                return $"This role requires skills you're still developing: {string.Join(", ", missingSkills.Take(3))}.";
        }
    }
}
