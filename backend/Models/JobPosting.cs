using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class JobPosting
    {
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Company { get; set; } = string.Empty;
        
        public string? Location { get; set; }
        
        public string? Description { get; set; }
        
        public string? Requirements { get; set; }
        
        public string? TechStack { get; set; }
        
        public string? ExperienceLevel { get; set; }
        
        public decimal? SalaryMin { get; set; }
        
        public decimal? SalaryMax { get; set; }
        
        public string? SalaryCurrency { get; set; }
        
        public bool IsRemote { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime PostedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ExpiresAt { get; set; }
        
        public string? ApplicationUrl { get; set; }
        
        // Navigation properties
        public virtual ICollection<JobMatch> JobMatches { get; set; } = new List<JobMatch>();
    }
}
