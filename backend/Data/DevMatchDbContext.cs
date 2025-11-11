using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data
{
    public class DevMatchDbContext : DbContext
    {
        public DevMatchDbContext(DbContextOptions<DevMatchDbContext> options) : base(options)
        {
        }
        
        public DbSet<GitHubProfile> GitHubProfiles { get; set; }
        public DbSet<JobPosting> JobPostings { get; set; }
        public DbSet<JobMatch> JobMatches { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // GitHubProfile configuration
            modelBuilder.Entity<GitHubProfile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.GitHubUsername).IsUnique();
                entity.Property(e => e.GitHubUsername).HasMaxLength(100);
                entity.Property(e => e.FullName).HasMaxLength(200);
                entity.Property(e => e.Bio).HasMaxLength(500);
                entity.Property(e => e.Location).HasMaxLength(100);
                entity.Property(e => e.Company).HasMaxLength(100);
                entity.Property(e => e.ExperienceLevel).HasMaxLength(50);
                entity.Property(e => e.MatchScore).HasPrecision(5, 2);
            });
            
            // JobPosting configuration
            modelBuilder.Entity<JobPosting>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Company).HasMaxLength(100);
                entity.Property(e => e.Location).HasMaxLength(100);
                entity.Property(e => e.ExperienceLevel).HasMaxLength(50);
                entity.Property(e => e.SalaryMin).HasPrecision(10, 2);
                entity.Property(e => e.SalaryMax).HasPrecision(10, 2);
                entity.Property(e => e.SalaryCurrency).HasMaxLength(3);
            });
            
            // JobMatch configuration
            modelBuilder.Entity<JobMatch>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MatchScore).HasPrecision(5, 2);
                
                // Relationships
                entity.HasOne(e => e.GitHubProfile)
                      .WithMany(e => e.JobMatches)
                      .HasForeignKey(e => e.GitHubProfileId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.HasOne(e => e.JobPosting)
                      .WithMany(e => e.JobMatches)
                      .HasForeignKey(e => e.JobPostingId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                // Composite index for unique matches
                entity.HasIndex(e => new { e.GitHubProfileId, e.JobPostingId }).IsUnique();
            });
        }
    }
}
