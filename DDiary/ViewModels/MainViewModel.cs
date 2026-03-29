using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DDiary.Commands;
using DDiary.Helpers;
using DDiary.Models;
using DDiary.Services;

namespace DDiary.ViewModels
{
    /// <summary>
    /// ViewModel principale dell'applicazione. Coordina navigazione e stato globale.
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private readonly IDiaryService _diaryService;
        private readonly IProfileService _profileService;
        private readonly ISettingsService _settingsService;
        private readonly IExportService _exportService;
        private readonly INotificationService _notificationService;

        private CancellationTokenSource? _reminderCts;

        // --- Sub ViewModels ---
        public DiaryViewModel DiaryVM { get; }
        public HistoryViewModel HistoryVM { get; }
        public SettingsViewModel SettingsVM { get; }
        public ProfileViewModel ProfileVM { get; }
        public FoodInsertViewModel FoodInsertVM { get; }

        // --- Navigation ---
        private string _currentPage = "Today";
        public string CurrentPage
        {
            get => _currentPage;
            set { SetProperty(ref _currentPage, value); UpdatePageVisibility(); }
        }

        private bool _showDiary = true;
        public bool ShowDiary { get => _showDiary; set => SetProperty(ref _showDiary, value); }
        private bool _showHistory;
        public bool ShowHistory { get => _showHistory; set => SetProperty(ref _showHistory, value); }
        private bool _showSettings;
        public bool ShowSettings { get => _showSettings; set => SetProperty(ref _showSettings, value); }
        private bool _showProfile;
        public bool ShowProfile { get => _showProfile; set => SetProperty(ref _showProfile, value); }

        // --- Sidebar ---
        private bool _isSidebarOpen = false;
        public bool IsSidebarOpen
        {
            get => _isSidebarOpen;
            set => SetProperty(ref _isSidebarOpen, value);
        }

        // --- Insert panel overlay ---
        private bool _isInsertPanelOpen;
        public bool IsInsertPanelOpen
        {
            get => _isInsertPanelOpen;
            set => SetProperty(ref _isInsertPanelOpen, value);
        }

        // --- Active profile ---
        private UserProfile? _activeProfile;
        public UserProfile? ActiveProfile
        {
            get => _activeProfile;
            set { SetProperty(ref _activeProfile, value); OnPropertyChanged(nameof(ActiveProfileName)); }
        }
        public string ActiveProfileName => _activeProfile?.DisplayName ?? "Ospite";

        // --- Status ---
        private string _statusMessage = string.Empty;
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

        // --- Commands ---
        public ICommand NavigateTodayCommand { get; }
        public ICommand NavigateHistoryCommand { get; }
        public ICommand NavigateSettingsCommand { get; }
        public ICommand NavigateProfileCommand { get; }
        public ICommand ToggleSidebarCommand { get; }
        public ICommand OpenInsertPanelCommand { get; }
        public ICommand CloseInsertPanelCommand { get; }
        public ICommand ExportPngCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand ToggleFullscreenCommand { get; }
        public ICommand CopyScreenshotCommand { get; }

        // --- Fullscreen state ---
        private bool _isFullscreen;
        public bool IsFullscreen
        {
            get => _isFullscreen;
            set { SetProperty(ref _isFullscreen, value); OnPropertyChanged(nameof(FullscreenLabel)); }
        }
        public string FullscreenLabel => _isFullscreen ? "🗗 Ripristina" : "⛶ Schermo intero";

        /// <summary>Provider per l'elemento UI del diario (usato per l'export).</summary>
        public Func<System.Windows.FrameworkElement?>? DiaryElementProvider { get; set; }

        public MainViewModel(
            IDiaryService diaryService,
            IProfileService profileService,
            ISettingsService settingsService,
            IExportService exportService,
            INotificationService notificationService,
            DiaryViewModel diaryVM,
            HistoryViewModel historyVM,
            SettingsViewModel settingsVM,
            ProfileViewModel profileVM,
            FoodInsertViewModel foodInsertVM)
        {
            _diaryService = diaryService;
            _profileService = profileService;
            _settingsService = settingsService;
            _exportService = exportService;
            _notificationService = notificationService;

            DiaryVM = diaryVM;
            HistoryVM = historyVM;
            SettingsVM = settingsVM;
            ProfileVM = profileVM;
            FoodInsertVM = foodInsertVM;

            NavigateTodayCommand = new RelayCommand(() => CurrentPage = "Today");
            NavigateHistoryCommand = new RelayCommand(() => CurrentPage = "History");
            NavigateSettingsCommand = new RelayCommand(() => CurrentPage = "Settings");
            NavigateProfileCommand = new RelayCommand(() => CurrentPage = "Profile");
            ToggleSidebarCommand = new RelayCommand(() => IsSidebarOpen = !IsSidebarOpen);
            OpenInsertPanelCommand = new RelayCommand(OpenInsertPanel);
            CloseInsertPanelCommand = new RelayCommand(CloseInsertPanel);
            ExportPngCommand = new RelayCommand(async () => await ExportPngAsync());
            ExportPdfCommand = new RelayCommand(async () => await ExportPdfAsync());
            ToggleFullscreenCommand = new RelayCommand(ToggleFullscreen);
            CopyScreenshotCommand = new RelayCommand(CopyScreenshot);

            // Wire events
            HistoryVM.DiarySelected += date => _ = OpenDiaryByDateAsync(date);
            HistoryVM.OpenOrCreateDiaryRequested += date => _ = OpenOrCreateDiaryForDateAsync(date);
            FoodInsertVM.RequestClose += CloseInsertPanel;
            FoodInsertVM.RequestSaveAndAdd += () => { /* keep panel open, reset form */ };
            ProfileVM.ProfileActivated += async p => await OnProfileActivated(p);
            SettingsVM.SettingsSaved += OnSettingsSaved;
            SettingsVM.RequestClose += OnSettingsCloseRequested;
        }

        public async Task InitializeAsync()
        {
            IsBusy = true;
            try
            {
                await _settingsService.LoadAsync();

                // Load profiles
                await ProfileVM.LoadAsync();

                // Get or create active profile
                var profiles = await _profileService.GetAllProfilesAsync();
                var profileList = new System.Collections.Generic.List<UserProfile>(profiles);

                int activeId = _settingsService.Settings.ActiveProfileId;
                _activeProfile = profileList.Find(p => p.Id == activeId) ?? profileList.FirstOrDefault();

                if (_activeProfile == null)
                {
                    _activeProfile = await _profileService.CreateProfileAsync("Utente");
                    _settingsService.Settings.ActiveProfileId = _activeProfile.Id;
                    await _settingsService.SaveAsync();
                }

                OnPropertyChanged(nameof(ActiveProfile));
                OnPropertyChanged(nameof(ActiveProfileName));

                // Load today's diary
                await DiaryVM.LoadAsync(_activeProfile.Id);

                // Load history
                await HistoryVM.LoadAsync(_activeProfile.Id);

                // Setup notifications
                SetupNotifications();

                // Navigate to default page
                //CurrentPage = _settingsService.Settings.DefaultStartupPage;
                CurrentPage = "Today";
                UpdatePageVisibility();
                // Wire insert command save
                FoodInsertVM.OnSave = async (entry, mealType) =>
                {
                    var model = DiaryVM.GetModel();
                    if (model != null)
                    {
                        var added = await _diaryService.AddFoodEntryAsync(model.Id, _activeProfile!.Id, entry, mealType);
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            DiaryVM.AddFoodEntryToSection(mealType, added);
                        });
                    }
                };
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OpenInsertPanel()
        {
            IsInsertPanelOpen = true;
        }

        private void CloseInsertPanel()
        {
            IsInsertPanelOpen = false;
        }

        private async Task OpenDiaryByDateAsync(DateTime date)
        {
            CurrentPage = "Today";
            if (_activeProfile != null)
                await DiaryVM.LoadAsync(_activeProfile.Id, date);
        }

        private async Task OpenOrCreateDiaryForDateAsync(DateTime date)
        {
            if (_activeProfile == null) return;
            IsBusy = true;
            try
            {
                IsSidebarOpen = false;
                await _diaryService.GetOrCreateDiaryForDateAsync(_activeProfile.Id, date);
                CurrentPage = "Today";
                await DiaryVM.LoadAsync(_activeProfile.Id, date);
                await HistoryVM.LoadAsync(_activeProfile.Id);
                StatusMessage = $"Diario del {date:dd/MM/yyyy} aperto.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Errore: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnProfileActivated(UserProfile profile)
        {
            _activeProfile = profile;
            OnPropertyChanged(nameof(ActiveProfile));
            OnPropertyChanged(nameof(ActiveProfileName));
            await DiaryVM.LoadAsync(profile.Id);
            await HistoryVM.LoadAsync(profile.Id);
            CurrentPage = "Today";
        }

        private void OnSettingsSaved()
        {
            ThemeManager.ApplyTheme(_settingsService.Settings.Theme);
            SetupNotifications();
            StatusMessage = "Impostazioni salvate.";
            CurrentPage = "Today";
        }

        private void OnSettingsCloseRequested()
        {
            CurrentPage = "Today";
        }

        private void ToggleFullscreen()
        {
            var w = System.Windows.Application.Current.MainWindow;
            if (IsFullscreen)
            {
                w.WindowStyle = WindowStyle.SingleBorderWindow;
                w.ResizeMode = ResizeMode.CanResize;
                w.WindowState = WindowState.Normal;
                IsFullscreen = false;
            }
            else
            {
                w.WindowState = WindowState.Normal;
                w.WindowStyle = WindowStyle.None;
                w.ResizeMode = ResizeMode.NoResize;
                w.WindowState = WindowState.Maximized;
                IsFullscreen = true;
                IsSidebarOpen = false;
            }
        }

        private void CopyScreenshot()
        {
            try
            {
                var element = DiaryElementProvider?.Invoke();
                if (element == null) return;
                _exportService.CopyToClipboard(element);
                StatusMessage = "Screenshot copiato negli appunti.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Errore screenshot: {ex.Message}";
            }
        }

        private void SetupNotifications()
        {
            _reminderCts?.Cancel();
            _reminderCts = new CancellationTokenSource();

            var s = _settingsService.Settings;
            var profileName = ActiveProfileName;

            if (s.DailyReminderEnabled && TimeSpan.TryParse(s.DailyReminderTime, out var reminderTime))
                _notificationService.ScheduleDailyReminder(reminderTime, profileName, _reminderCts.Token);
        }

        private async Task ExportPngAsync()
        {
            try
            {
                var element = DiaryElementProvider?.Invoke();
                if (element == null || _activeProfile == null) return;
                var model = DiaryVM.GetModel();
                if (model == null) return;
                var path = await _exportService.ExportAsPngAsync(element, model, _activeProfile);
                StatusMessage = $"PNG esportato: {path}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Errore esportazione PNG: {ex.Message}";
            }
        }

        private async Task ExportPdfAsync()
        {
            try
            {
                if (_activeProfile == null) return;
                var model = DiaryVM.GetModel();
                if (model == null) return;
                var path = await _exportService.ExportAsPdfAsync(model, _activeProfile);
                StatusMessage = $"PDF esportato: {path}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Errore esportazione PDF: {ex.Message}";
            }
        }

        private void UpdatePageVisibility()
        {
            ShowDiary = _currentPage == "Today";
            ShowHistory = _currentPage == "History";
            ShowSettings = _currentPage == "Settings";
            ShowProfile = _currentPage == "Profile";
        }
    }
}
