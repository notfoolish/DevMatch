using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class JobMatch
    {
        public int Id { get; set; }
        
        public int GitHubProfileId { get; set; }
        
        public int JobPostingId { get; set; }
        
        [Range(0, 100)]
        public decimal MatchScore { get; set; }
        
        public string? MatchReasons { get; set; }
        
        public string? SkillsMatch { get; set; }
        
        public string? ExperienceMatch { get; set; }
        
        public string? LocationMatch { get; set; }
        
        public bool IsReviewed { get; set; } = false;
        
        public bool IsApplied { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual GitHubProfile GitHubProfile { get; set; } = null!;
        
        public virtual JobPosting JobPosting { get; set; } = null!;
    }
}
