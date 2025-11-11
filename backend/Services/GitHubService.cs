using System.Text.Json;
using backend.DTOs;

namespace backend.Services
{
    public interface IGitHubService
    {
        Task<GitHubAnalysisResponseDto> AnalyzeProfileAsync(string username);
    }

    public class GitHubService : IGitHubService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GitHubService> _logger;

        public GitHubService(HttpClient httpClient, IConfiguration configuration, ILogger<GitHubService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            var gitHubToken = _configuration["ApiSettings:GitHubToken"];
            if (!string.IsNullOrEmpty(gitHubToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("token", gitHubToken);
            }
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DevMatch/1.0");
        }

        public async Task<GitHubAnalysisResponseDto> AnalyzeProfileAsync(string username)
        {
            try
            {
                _logger.LogInformation($"Starting analysis for GitHub user: {username}");

                var profile = await GetProfileAsync(username);
                var repositories = await GetRepositoriesAsync(username);
                var languageStats = CalculateLanguageStats(repositories);

                return new GitHubAnalysisResponseDto
                {
                    Profile = profile,
                    Repositories = repositories,
                    LanguageStats = languageStats,
                    TotalCommits = await EstimateTotalCommitsAsync(username, repositories),
                    AnalyzedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing GitHub profile for user: {username}");
                throw;
            }
        }

        private async Task<GitHubProfileDto> GetProfileAsync(string username)
        {
            var response = await _httpClient.GetAsync($"https://api.github.com/users/{username}");
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new ArgumentException($"GitHub user '{username}' not found");
                }
                throw new HttpRequestException($"GitHub API error: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;

            return new GitHubProfileDto
            {
                Login = root.GetProperty("login").GetString() ?? "",
                Name = root.GetProperty("name").GetString() ?? "",
                Bio = root.TryGetProperty("bio", out var bio) ? bio.GetString() : null,
                Location = root.TryGetProperty("location", out var location) ? location.GetString() : null,
                Company = root.TryGetProperty("company", out var company) ? company.GetString() : null,
                Blog = root.TryGetProperty("blog", out var blog) ? blog.GetString() : null,
                PublicRepos = root.GetProperty("public_repos").GetInt32(),
                Followers = root.GetProperty("followers").GetInt32(),
                Following = root.GetProperty("following").GetInt32(),
                CreatedAt = DateTime.Parse(root.GetProperty("created_at").GetString() ?? DateTime.UtcNow.ToString()),
                UpdatedAt = DateTime.Parse(root.GetProperty("updated_at").GetString() ?? DateTime.UtcNow.ToString()),
                AvatarUrl = root.GetProperty("avatar_url").GetString() ?? "",
                HtmlUrl = root.GetProperty("html_url").GetString() ?? ""
            };
        }

        private async Task<List<GitHubRepoDto>> GetRepositoriesAsync(string username)
        {
            var repos = new List<GitHubRepoDto>();
            var page = 1;
            const int perPage = 100;

            while (page <= 5) // Limit to first 5 pages (500 repos max)
            {
                var response = await _httpClient.GetAsync($"https://api.github.com/users/{username}/repos?page={page}&per_page={perPage}&sort=updated");
                
                if (!response.IsSuccessStatusCode) break;

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var repoArray = jsonDoc.RootElement;

                if (repoArray.GetArrayLength() == 0) break;

                foreach (var repo in repoArray.EnumerateArray())
                {
                    // Skip forks unless they have significant activity
                    if (repo.GetProperty("fork").GetBoolean() && repo.GetProperty("stargazers_count").GetInt32() == 0)
                        continue;

                    repos.Add(new GitHubRepoDto
                    {
                        Name = repo.GetProperty("name").GetString() ?? "",
                        Description = repo.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                        Language = repo.TryGetProperty("language", out var lang) ? lang.GetString() : null,
                        StargazersCount = repo.GetProperty("stargazers_count").GetInt32(),
                        ForksCount = repo.GetProperty("forks_count").GetInt32(),
                        Size = repo.GetProperty("size").GetInt32(),
                        CreatedAt = DateTime.Parse(repo.GetProperty("created_at").GetString() ?? DateTime.UtcNow.ToString()),
                        UpdatedAt = DateTime.Parse(repo.GetProperty("updated_at").GetString() ?? DateTime.UtcNow.ToString()),
                        PushedAt = repo.TryGetProperty("pushed_at", out var pushed) && !pushed.ValueKind.Equals(JsonValueKind.Null) 
                            ? DateTime.Parse(pushed.GetString() ?? DateTime.UtcNow.ToString()) 
                            : null,
                        HtmlUrl = repo.GetProperty("html_url").GetString() ?? ""
                    });
                }

                page++;
            }

            return repos.OrderByDescending(r => r.UpdatedAt).Take(50).ToList(); // Return most recent 50
        }

        private GitHubLanguageStatsDto CalculateLanguageStats(List<GitHubRepoDto> repositories)
        {
            var languageStats = new Dictionary<string, int>();

            foreach (var repo in repositories)
            {
                if (!string.IsNullOrEmpty(repo.Language))
                {
                    var weight = 1 + repo.StargazersCount + repo.ForksCount + (repo.Size / 1000);
                    if (languageStats.ContainsKey(repo.Language))
                        languageStats[repo.Language] += weight;
                    else
                        languageStats[repo.Language] = weight;
                }
            }

            return new GitHubLanguageStatsDto
            {
                Languages = languageStats.OrderByDescending(kvp => kvp.Value)
                                       .Take(10)
                                       .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }

        private async Task<int> EstimateTotalCommitsAsync(string username, List<GitHubRepoDto> repositories)
        {
            // Simple estimation based on repository activity
            // In a real application, you might want to fetch actual commit counts
            var totalCommits = 0;
            var activeRepos = repositories.Where(r => r.PushedAt.HasValue && r.PushedAt.Value > DateTime.UtcNow.AddYears(-2)).Take(10);

            foreach (var repo in activeRepos)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"https://api.github.com/repos/{username}/{repo.Name}/contributors");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var contributors = JsonDocument.Parse(content).RootElement;
                        
                        foreach (var contributor in contributors.EnumerateArray())
                        {
                            if (contributor.GetProperty("login").GetString() == username)
                            {
                                totalCommits += contributor.GetProperty("contributions").GetInt32();
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    // Estimate based on repo size and activity
                    totalCommits += Math.Max(1, repo.Size / 100);
                }

                // Add small delay to avoid rate limiting
                await Task.Delay(100);
            }

            return Math.Max(totalCommits, repositories.Count * 2); // Minimum estimate
        }
    }
}
