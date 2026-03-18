using System;

namespace DDiary.Models
{
    /// <summary>
    /// Rappresenta il profilo di un utente locale dell'app DDiary.
    /// </summary>
    public class UserProfile
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string PreferredLanguage { get; set; } = "it-IT";
        public string PreferredTheme { get; set; } = "System";
        public string PreferredFontFamily { get; set; } = "Segoe UI";
        public double PreferredFontSize { get; set; } = 14;
        public string PreferredExportFolder { get; set; } = string.Empty;
        public bool StartupReminderEnabled { get; set; } = false;
        public bool DailyReminderEnabled { get; set; } = false;
        public TimeSpan DailyReminderTime { get; set; } = new TimeSpan(8, 0, 0);
        public string BackgroundStyle { get; set; } = "Default";
        public string AccentColor { get; set; } = "#0078D4";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<DailyDiary> DailyDiaries { get; set; } = new List<DailyDiary>();
    }
}
