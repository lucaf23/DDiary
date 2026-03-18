using System;
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
    /// ViewModel per la gestione dei profili utente locali.
    /// </summary>
    public class ProfileViewModel : BaseViewModel
    {
        private readonly IProfileService _profileService;
        private readonly ISettingsService _settingsService;

        public ObservableCollection<UserProfile> Profiles { get; } = new();

        private UserProfile? _selectedProfile;
        public UserProfile? SelectedProfile
        {
            get => _selectedProfile;
            set => SetProperty(ref _selectedProfile, value);
        }

        private string _newProfileName = string.Empty;
        public string NewProfileName
        {
            get => _newProfileName;
            set => SetProperty(ref _newProfileName, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public ICommand CreateProfileCommand { get; }
        public ICommand DeleteProfileCommand { get; }
        public ICommand SelectProfileCommand { get; }
        public ICommand SaveProfileCommand { get; }

        public event Action<UserProfile>? ProfileActivated;

        public ProfileViewModel(IProfileService profileService, ISettingsService settingsService)
        {
            _profileService = profileService;
            _settingsService = settingsService;

            CreateProfileCommand = new RelayCommand(async () => await CreateProfileAsync(), () => !string.IsNullOrWhiteSpace(NewProfileName));
            DeleteProfileCommand = new RelayCommand<int>(async id => await DeleteProfileAsync(id));
            SelectProfileCommand = new RelayCommand<UserProfile>(p => ActivateProfile(p!));
            SaveProfileCommand = new RelayCommand(async () => await SaveProfileAsync(), () => SelectedProfile != null);
        }

        public async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                var profiles = await _profileService.GetAllProfilesAsync();
                Profiles.Clear();
                foreach (var p in profiles)
                    Profiles.Add(p);

                // Select current active profile
                var activeId = _settingsService.Settings.ActiveProfileId;
                SelectedProfile = Profiles.FirstOrDefault(p => p.Id == activeId) ?? Profiles.FirstOrDefault();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CreateProfileAsync()
        {
            if (string.IsNullOrWhiteSpace(NewProfileName)) return;
            var profile = await _profileService.CreateProfileAsync(NewProfileName.Trim());
            Profiles.Add(profile);
            NewProfileName = string.Empty;
            SelectedProfile = profile;
        }

        private async Task DeleteProfileAsync(int id)
        {
            if (Profiles.Count <= 1) return; // Don't delete last profile
            await _profileService.DeleteProfileAsync(id);
            var profile = Profiles.FirstOrDefault(p => p.Id == id);
            if (profile != null) Profiles.Remove(profile);
            if (SelectedProfile?.Id == id)
                SelectedProfile = Profiles.FirstOrDefault();
        }

        private void ActivateProfile(UserProfile profile)
        {
            SelectedProfile = profile;
            _settingsService.Settings.ActiveProfileId = profile.Id;
            if (!string.IsNullOrWhiteSpace(profile.PreferredTheme))
            {
                _settingsService.Settings.Theme = profile.PreferredTheme;
                ThemeManager.ApplyTheme(profile.PreferredTheme);
            }
            _ = _settingsService.SaveAsync();
            ProfileActivated?.Invoke(profile);
        }

        private async Task SaveProfileAsync()
        {
            if (SelectedProfile == null) return;
            await _profileService.UpdateProfileAsync(SelectedProfile);

            if (!string.IsNullOrWhiteSpace(SelectedProfile.PreferredTheme))
            {
                _settingsService.Settings.Theme = SelectedProfile.PreferredTheme;
                await _settingsService.SaveAsync();
                ThemeManager.ApplyTheme(SelectedProfile.PreferredTheme);
            }
        }

        public List<string> ThemeOptions { get; } = new() { "Light", "Dark", "System" };
    }
}
