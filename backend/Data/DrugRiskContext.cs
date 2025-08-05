using DrugRiskAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DrugRiskAPI.Data
{
    public class DrugRiskContext : DbContext
    {
        public DrugRiskContext(DbContextOptions<DrugRiskContext> options) : base(options)
        {
        }

        public DbSet<UserRun> UserRuns { get; set; }
        public DbSet<VcfData> VcfData { get; set; }
        public DbSet<RiskAssessment> RiskAssessments { get; set; }
        public DbSet<DrugAlternative> DrugAlternatives { get; set; }
        public DbSet<AnalyticsEvent> AnalyticsEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserRun>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.RiskScore).HasPrecision(5, 4);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<VcfData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasOne(v => v.UserRun)
                    .WithMany(u => u.VcfData)
                    .HasForeignKey(v => v.UserRunId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RiskAssessment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.RiskScore).HasPrecision(5, 4);
                entity.Property(e => e.Confidence).HasPrecision(5, 4);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(r => r.UserRun)
                    .WithOne(u => u.RiskAssessment)
                    .HasForeignKey<RiskAssessment>(r => r.UserRunId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DrugAlternative>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.ConfidenceScore).HasPrecision(5, 4);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(d => d.UserRun)
                    .WithMany(u => u.DrugAlternatives)
                    .HasForeignKey(d => d.UserRunId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AnalyticsEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(a => a.UserRun)
                    .WithMany(u => u.AnalyticsEvents)
                    .HasForeignKey(a => a.UserRunId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
} 