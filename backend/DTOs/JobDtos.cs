namespace backend.DTOs
{
    public class JobPostingDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Description { get; set; }
        public List<string> RequiredSkills { get; set; } = new List<string>();
        public List<string> PreferredSkills { get; set; } = new List<string>();
        public string? ExperienceLevel { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public string? RemoteOptions { get; set; }
        public DateTime PostedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CreateJobPostingDto
    {
        public string Title { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Description { get; set; }
        public List<string> RequiredSkills { get; set; } = new List<string>();
        public List<string> PreferredSkills { get; set; } = new List<string>();
        public string? ExperienceLevel { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public string? RemoteOptions { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
