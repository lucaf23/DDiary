using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DDiary.Commands;
using DDiary.Helpers;
using DDiary.Models;
using DDiary.Services;

namespace DDiary.ViewModels
{
    /// <summary>
    /// ViewModel per la schermata impostazioni.
    /// </summary>
    public class SettingsViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;

        private string _theme = "System";
        public string Theme
        {
            get => _theme;
            set
            {
                if (SetProperty(ref _theme, value))
                    ThemeManager.ApplyTheme(value);
            }
        }

        private string _accentColor = "#0078D4";
        public string AccentColor
        {
            get => _accentColor;
            set => SetProperty(ref _accentColor, value);
        }

        private string _backgroundStyle = "Striped";
        public string BackgroundStyle
        {
            get => _backgroundStyle;
            set => SetProperty(ref _backgroundStyle, value);
        }

        private string _fontFamily = "Segoe UI";
        public string FontFamily
        {
            get => _fontFamily;
            set => SetProperty(ref _fontFamily, value);
        }

        private double _fontSize = 14;
        public double FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }

        private string _language = "it-IT";
        public string Language
        {
            get => _language;
            set => SetProperty(ref _language, value);
        }

        private string _exportFolder = string.Empty;
        public string ExportFolder
        {
            get => _exportFolder;
            set => SetProperty(ref _exportFolder, value);
        }

        private bool _dailyReminderEnabled;
        public bool DailyReminderEnabled
        {
            get => _dailyReminderEnabled;
            set => SetProperty(ref _dailyReminderEnabled, value);
        }

        private string _dailyReminderTime = "08:00";
        public string DailyReminderTime
        {
            get => _dailyReminderTime;
            set => SetProperty(ref _dailyReminderTime, value);
        }

        private bool _startupReminderEnabled;
        public bool StartupReminderEnabled
        {
            get => _startupReminderEnabled;
            set => SetProperty(ref _startupReminderEnabled, value);
        }

        private bool _compactMode;
        public bool CompactMode
        {
            get => _compactMode;
            set => SetProperty(ref _compactMode, value);
        }

        private string _animationIntensity = "Normal";
        public string AnimationIntensity
        {
            get => _animationIntensity;
            set => SetProperty(ref _animationIntensity, value);
        }

        public ObservableCollection<MealTimeRangeSetting> MealTimeRanges { get; } = new();

        public List<string> Themes { get; } = new() { "Light", "Dark", "System" };
        public List<string> AnimationIntensities { get; } = new() { "None", "Reduced", "Normal", "Full" };
        public List<string> BackgroundStyles { get; } = new() { "Default", "Striped", "Grid", "Dots" };

        public ICommand SaveCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand BrowseFolderCommand { get; }
        public ICommand CloseCommand { get; }

        public event Action? SettingsSaved;
        public event Action? RequestClose;

        public SettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            SaveCommand = new RelayCommand(async () => await SaveAsync());
            ResetCommand = new RelayCommand(async () => await ResetAsync());
            BrowseFolderCommand = new RelayCommand(BrowseFolder);
            CloseCommand = new RelayCommand(Close);

            LoadFromSettings();
        }

        private void LoadFromSettings()
        {
            var s = _settingsService.Settings;
            Theme = s.Theme;
            AccentColor = s.AccentColor;
            BackgroundStyle = s.BackgroundStyle;
            FontFamily = s.FontFamily;
            FontSize = s.FontSize;
            Language = s.Language;
            ExportFolder = s.ExportFolder;
            DailyReminderEnabled = s.DailyReminderEnabled;
            DailyReminderTime = s.DailyReminderTime;
            StartupReminderEnabled = s.StartupReminderEnabled;
            CompactMode = s.CompactMode;
            AnimationIntensity = s.AnimationIntensity;

            MealTimeRanges.Clear();
            foreach (var r in s.MealTimeRanges)
                MealTimeRanges.Add(new MealTimeRangeSetting { MealType = r.MealType, Start = r.Start, End = r.End });
        }

        private async Task SaveAsync()
        {
            var s = _settingsService.Settings;
            s.Theme = Theme;
            s.AccentColor = AccentColor;
            s.BackgroundStyle = BackgroundStyle;
            s.FontFamily = FontFamily;
            s.FontSize = FontSize;
            s.Language = Language;
            s.ExportFolder = ExportFolder;
            s.DailyReminderEnabled = DailyReminderEnabled;
            s.DailyReminderTime = DailyReminderTime;
            s.StartupReminderEnabled = StartupReminderEnabled;
            s.CompactMode = CompactMode;
            s.AnimationIntensity = AnimationIntensity;
            s.MealTimeRanges = MealTimeRanges.ToList();

            await _settingsService.SaveAsync();
            SettingsSaved?.Invoke();
            RequestClose?.Invoke();
        }

        private async Task ResetAsync()
        {
            _settingsService.Apply(new AppSettings());
            await _settingsService.SaveAsync();
            LoadFromSettings();
        }

        private void BrowseFolder()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Seleziona cartella di esportazione",
                SelectedPath = ExportFolder
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                ExportFolder = dialog.SelectedPath;
        }

        private void Close()
        {
            RequestClose?.Invoke();
        }
    }
}
