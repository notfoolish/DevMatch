using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class GitHubProfile
    {
        public int Id { get; set; }
        
        [Required]
        public string GitHubUsername { get; set; } = string.Empty;
        
        [Required]
        public string FullName { get; set; } = string.Empty;
        
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
        
        // AI Analysis Results
        public string? SkillsAnalysis { get; set; }
        
        public string? ExperienceLevel { get; set; }
        
        public string? PrimaryLanguages { get; set; }
        
        public string? TechStack { get; set; }
        
        public decimal? MatchScore { get; set; }
        
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<JobMatch> JobMatches { get; set; } = new List<JobMatch>();
    }
}
