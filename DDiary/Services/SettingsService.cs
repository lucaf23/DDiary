using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using DDiary.Models;

namespace DDiary.Services
{
    /// <summary>
    /// Servizio per la gestione e persistenza delle impostazioni dell'applicazione.
    /// </summary>
    public interface ISettingsService
    {
        AppSettings Settings { get; }
        Task LoadAsync();
        Task SaveAsync();
        void Apply(AppSettings settings);
    }

    public class SettingsService : ISettingsService
    {
        private static readonly string SettingsFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DDiary");
        private static readonly string SettingsFile =
            Path.Combine(SettingsFolder, "settings.json");

        public AppSettings Settings { get; private set; } = new AppSettings();

        public async Task LoadAsync()
        {
            try
            {
                Directory.CreateDirectory(SettingsFolder);
                if (File.Exists(SettingsFile))
                {
                    var json = await File.ReadAllTextAsync(SettingsFile);
                    Settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    Settings = new AppSettings();
                    await SaveAsync();
                }
            }
            catch
            {
                Settings = new AppSettings();
            }
        }

        public async Task SaveAsync()
        {
            Directory.CreateDirectory(SettingsFolder);
            var json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
            await File.WriteAllTextAsync(SettingsFile, json);
        }

        public void Apply(AppSettings settings)
        {
            Settings = settings;
        }
    }
}
