namespace backend.DTOs
{
    public class AiAnalysisRequestDto
    {
        public string GitHubUsername { get; set; } = string.Empty;
        public GitHubAnalysisResponseDto? GitHubData { get; set; }
    }

    public class AiAnalysisResponseDto
    {
        public string GitHubUsername { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<string> Skills { get; set; } = new List<string>();
        public string ExperienceLevel { get; set; } = string.Empty;
        public List<string> PrimaryLanguages { get; set; } = new List<string>();
        public List<string> TechStack { get; set; } = new List<string>();
        public List<string> Strengths { get; set; } = new List<string>();
        public List<string> ImprovementAreas { get; set; } = new List<string>();
        public decimal OverallScore { get; set; }
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    }

    public class JobMatchAnalysisDto
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public decimal MatchScore { get; set; }
        public List<string> MatchingSkills { get; set; } = new List<string>();
        public List<string> MissingSkills { get; set; } = new List<string>();
        public string MatchReason { get; set; } = string.Empty;
    }

    public class JobMatchResponseDto
    {
        public AiAnalysisResponseDto? ProfileAnalysis { get; set; }
        public List<JobMatchAnalysisDto> JobMatches { get; set; } = new List<JobMatchAnalysisDto>();
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    }
}
