using System;
using System.Collections.Generic;
using DDiary.Models;

namespace DDiary.Models
{
    /// <summary>
    /// Impostazioni locali dell'applicazione, persistite su file JSON.
    /// </summary>
    public class AppSettings
    {
        public int ActiveProfileId { get; set; } = 0;
        public string Theme { get; set; } = "System";
        public string AccentColor { get; set; } = "#0078D4";
        public string BackgroundStyle { get; set; } = "Striped";
        public string FontFamily { get; set; } = "Segoe UI";
        public double FontSize { get; set; } = 14;
        public string Language { get; set; } = "it-IT";
        public string ExportFolder { get; set; } = string.Empty;
        public bool DailyReminderEnabled { get; set; } = false;
        public string DailyReminderTime { get; set; } = "08:00";
        public bool StartupReminderEnabled { get; set; } = false;
        public string DefaultStartupPage { get; set; } = "Today";
        public bool CompactMode { get; set; } = false;
        public string AnimationIntensity { get; set; } = "Normal";
        public string DiaryViewMode { get; set; } = "Modern";
        public List<MealTimeRangeSetting> MealTimeRanges { get; set; } = GetDefaultMealTimeRanges();

        public static List<MealTimeRangeSetting> GetDefaultMealTimeRanges()
        {
            return new List<MealTimeRangeSetting>
            {
                new() { MealType = MealType.Colazione,          Start = "05:00", End = "10:59" },
                new() { MealType = MealType.MerendaMattina,     Start = "11:00", End = "11:59" },
                new() { MealType = MealType.Pranzo,             Start = "12:00", End = "14:59" },
                new() { MealType = MealType.MerendaPomeriggio,  Start = "15:00", End = "17:59" },
                new() { MealType = MealType.Cena,               Start = "18:00", End = "21:59" },
                new() { MealType = MealType.DopoCena,           Start = "22:00", End = "04:59" },
            };
        }
    }

    public class MealTimeRangeSetting
    {
        public MealType MealType { get; set; }
        public string Start { get; set; } = "00:00";
        public string End { get; set; } = "23:59";
    }
}
