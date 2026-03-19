using Microsoft.EntityFrameworkCore;
using DDiary.Models;
using System.IO;

namespace DDiary.Data
{
    /// <summary>
    /// DbContext principale per la persistenza locale con SQLite.
    /// </summary>
    public class DDiaryDbContext : DbContext
    {
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<DailyDiary> DailyDiaries => Set<DailyDiary>();
        public DbSet<MealSection> MealSections => Set<MealSection>();
        public DbSet<FoodEntry> FoodEntries => Set<FoodEntry>();

        public DDiaryDbContext(DbContextOptions<DDiaryDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // UserProfile
            modelBuilder.Entity<UserProfile>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
                e.Property(x => x.AccentColor).HasMaxLength(20);
                e.Property(x => x.DailyReminderTime)
                    .HasConversion(
                        v => v.ToString(@"hh\:mm"),
                        v => TimeSpan.Parse(v));
            });

            // DailyDiary
            modelBuilder.Entity<DailyDiary>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.UserProfile)
                    .WithMany(x => x.DailyDiaries)
                    .HasForeignKey(x => x.UserProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.UserProfileId, x.Date }).IsUnique();
            });

            // MealSection
            modelBuilder.Entity<MealSection>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.DailyDiary)
                    .WithMany(x => x.MealSections)
                    .HasForeignKey(x => x.DailyDiaryId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.Property(x => x.MealType).HasConversion<string>();
                e.Property(x => x.MealTime)
                    .HasConversion(
                        v => v.ToString(@"HH\:mm"),
                        v => TimeSpan.Parse(v))
                    .HasDefaultValueSql("'00:00'");
            });

            // FoodEntry
            modelBuilder.Entity<FoodEntry>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.MealSection)
                    .WithMany(x => x.FoodEntries)
                    .HasForeignKey(x => x.MealSectionId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.Property(x => x.FoodName).HasMaxLength(200).IsRequired();
                e.Property(x => x.MealTime)
                    .HasConversion(
                        v => v.ToString(@"hh\:mm"),
                        v => TimeSpan.Parse(v));
            });
        }
    }
}
