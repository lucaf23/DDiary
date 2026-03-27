using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DDiary.Data;
using DDiary.Helpers;
using DDiary.Repositories;
using DDiary.Services;
using DDiary.ViewModels;
using DDiary.Views;

namespace DDiary
{
    /// <summary>
    /// Entry point e composition root dell'applicazione DDiary.
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private ServiceProvider? _serviceProvider;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Ensure DB exists and is migrated
            await EnsureDatabaseAsync();

            // Load settings first to apply theme
            var settings = _serviceProvider.GetRequiredService<ISettingsService>();
            await settings.LoadAsync();
            ThemeManager.ApplyTheme(settings.Settings.Theme);

            // Show main window
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            // Startup notification
            if (settings.Settings.StartupReminderEnabled)
            {
                var notif = _serviceProvider.GetRequiredService<INotificationService>();
                notif.ShowStartupReminder("utente");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Database
            var dbFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DDiary");
            Directory.CreateDirectory(dbFolder);
            var dbPath = Path.Combine(dbFolder, "ddiary.db");

            services.AddDbContext<DDiaryDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // Repositories
            services.AddScoped<IDiaryRepository, DiaryRepository>();
            services.AddScoped<IProfileRepository, ProfileRepository>();

            // Services (singleton so state is shared across VMs)
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddScoped<IDiaryService, DiaryService>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<IExportService, ExportService>();

            // ViewModels
            services.AddScoped<DiaryViewModel>();
            services.AddScoped<HistoryViewModel>();
            services.AddScoped<SettingsViewModel>();
            services.AddScoped<ProfileViewModel>();
            services.AddScoped<FoodInsertViewModel>();
            services.AddScoped<MainViewModel>();

            // Views
            services.AddTransient<MainWindow>(sp =>
            {
                var vm = sp.GetRequiredService<MainViewModel>();
                var window = new MainWindow { DataContext = vm };
                return window;
            });
        }

        private async Task EnsureDatabaseAsync()
        {
            using var scope = _serviceProvider!.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<DDiaryDbContext>();
            await ctx.Database.EnsureCreatedAsync();

            // Handle schema updates for existing databases (add new columns gracefully)
            try
            {
                await ctx.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE MealSections ADD COLUMN MealTime TEXT NOT NULL DEFAULT '00:00'");
            }
            catch { /* Column already exists — safe to ignore */ }
        }
    }
}


