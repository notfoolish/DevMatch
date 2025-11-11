namespace backend.DTOs
{
    public class GitHubProfileDto
    {
        public string Login { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public string? Company { get; set; }
        public string? Blog { get; set; }
        public int PublicRepos { get; set; }
        public int Followers { get; set; }
        public int Following { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? AvatarUrl { get; set; }
        public string? HtmlUrl { get; set; }
    }

    public class GitHubRepoDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Language { get; set; }
        public int StargazersCount { get; set; }
        public int ForksCount { get; set; }
        public int Size { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? PushedAt { get; set; }
        public string? HtmlUrl { get; set; }
    }

    public class GitHubLanguageStatsDto
    {
        public Dictionary<string, int> Languages { get; set; } = new Dictionary<string, int>();
    }

    public class GitHubAnalysisResponseDto
    {
        public GitHubProfileDto? Profile { get; set; }
        public List<GitHubRepoDto> Repositories { get; set; } = new List<GitHubRepoDto>();
        public GitHubLanguageStatsDto LanguageStats { get; set; } = new GitHubLanguageStatsDto();
        public int TotalCommits { get; set; }
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    }
}
