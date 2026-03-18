using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;

namespace DDiary.Data
{
    /// <summary>
    /// Factory per la creazione del DbContext in design time (migrations).
    /// </summary>
    public class DDiaryDbContextFactory : IDesignTimeDbContextFactory<DDiaryDbContext>
    {
        public DDiaryDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DDiaryDbContext>();
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DDiary", "ddiary.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            return new DDiaryDbContext(optionsBuilder.Options);
        }
    }
}
