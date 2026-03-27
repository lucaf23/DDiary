using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DDiary.Commands;
using DDiary.Models;
using DDiary.Services;

namespace DDiary.ViewModels
{
    /// <summary>
    /// ViewModel per l'inserimento rapido di un alimento.
    /// </summary>
    public class FoodInsertViewModel : BaseViewModel
    {
        private readonly IDiaryService _diaryService;
        private readonly ISettingsService _settingsService;

        private bool _updatingFromMealType = false;

        private static int RoundMinuteTo5(int minute) => (minute / 5) * 5;

        // ── Hour / Minute as integers ──
        private int _mealHour = DateTime.Now.Hour;
        public int MealHour
        {
            get => _mealHour;
            set
            {
                value = ((value % 24) + 24) % 24;
                SetProperty(ref _mealHour, value);
                OnPropertyChanged(nameof(MealHourDisplay));
                SyncTimeTextFromParts();
            }
        }

        private int _mealMinute = RoundMinuteTo5(DateTime.Now.Minute);
        public int MealMinute
        {
            get => _mealMinute;
            set
            {
                value = ((value % 60) + 60) % 60;
                SetProperty(ref _mealMinute, value);
                OnPropertyChanged(nameof(MealMinuteDisplay));
                SyncTimeTextFromParts();
            }
        }

        public string MealHourDisplay => _mealHour.ToString("D2");
        public string MealMinuteDisplay => _mealMinute.ToString("D2");

        /// <summary>
        /// Ora del pasto come <see cref="TimeSpan"/> (formato 24h).
        /// Proprietà di convenienza per leggere o impostare l'orario in un colpo solo.
        /// </summary>
        public TimeSpan? MealTime
        {
            get => new TimeSpan(_mealHour, _mealMinute, 0);
            set
            {
                if (!value.HasValue) return;
                if (value.Value.Hours == _mealHour && value.Value.Minutes == _mealMinute) return;
                MealHour = value.Value.Hours;
                MealMinute = value.Value.Minutes;
            }
        }

        private string _mealTimeText = DateTime.Now.ToString("HH:mm");
        public string MealTimeText
        {
            get => _mealTimeText;
            set
            {
                if (!_updatingFromMealType)
                {
                    if (TimeSpan.TryParse(value, out var ts))
                    {
                        SetProperty(ref _mealTimeText, value);
                        MealHour = ts.Hours;
                        MealMinute = ts.Minutes;
                        UpdateAutoMealType();
                    }
                    else
                    {
                        // Revert to last valid value
                        OnPropertyChanged(nameof(MealTimeText));
                    }
                    ValidateTime();
                }
                else
                {
                    SetProperty(ref _mealTimeText, value);
                    ValidateTime();
                }
            }
        }

        private string _foodName = string.Empty;
        public string FoodName
        {
            get => _foodName;
            set { SetProperty(ref _foodName, value); ValidateFoodName(); }
        }

        private string _portionGramsText = "0";
        public string PortionGramsText
        {
            get => _portionGramsText;
            set { SetProperty(ref _portionGramsText, value); ValidateGrams(); }
        }

        private string _choGramsText = "0";
        public string ChoGramsText
        {
            get => _choGramsText;
            set { SetProperty(ref _choGramsText, value); ValidateCho(); }
        }

        private MealType _selectedMealType = MealType.Colazione;
        public MealType SelectedMealType
        {
            get => _selectedMealType;
            set
            {
                if (SetProperty(ref _selectedMealType, value))
                {
                    OnPropertyChanged(nameof(SelectedMealTypeOption));
                    SetTimeFromMealType(value);
                }
            }
        }

        /// <summary>Bound to ComboBox SelectedItem to avoid SelectedValue/SelectedValuePath display quirks.</summary>
        public MealTypeOption? SelectedMealTypeOption
        {
            get => MealTypeOptions.FirstOrDefault(o => o.Value == _selectedMealType);
            set
            {
                if (value != null && value.Value != _selectedMealType)
                {
                    _selectedMealType = value.Value;
                    OnPropertyChanged(nameof(SelectedMealType));
                    OnPropertyChanged(nameof(SelectedMealTypeOption));
                    SetTimeFromMealType(value.Value);
                }
            }
        }

        private bool _manualMealOverride = false;
        public bool ManualMealOverride
        {
            get => _manualMealOverride;
            set { SetProperty(ref _manualMealOverride, value); if (!value) UpdateAutoMealType(); }
        }

        public ObservableCollection<MealTypeOption> MealTypeOptions { get; } = new(
            Enum.GetValues<MealType>().Select(mt => new MealTypeOption(mt)));

        // Validation
        private string _timeError = string.Empty;
        public string TimeError { get => _timeError; set => SetProperty(ref _timeError, value); }

        private string _nameError = string.Empty;
        public string NameError { get => _nameError; set => SetProperty(ref _nameError, value); }

        private string _gramsError = string.Empty;
        public string GramsError { get => _gramsError; set => SetProperty(ref _gramsError, value); }

        private string _choError = string.Empty;
        public string ChoError { get => _choError; set => SetProperty(ref _choError, value); }

        public bool IsValid =>
            string.IsNullOrEmpty(TimeError) &&
            string.IsNullOrEmpty(NameError) &&
            string.IsNullOrEmpty(GramsError) &&
            string.IsNullOrEmpty(ChoError) &&
            !string.IsNullOrWhiteSpace(FoodName) &&
            TimeSpan.TryParse(MealTimeText, out _);

        public ICommand SaveCommand { get; init; }
        public ICommand SaveAndAddCommand { get; init; }
        public ICommand CancelCommand { get; init; }

        public ICommand IncrementHourCommand { get; init; }
        public ICommand DecrementHourCommand { get; init; }
        public ICommand IncrementMinuteCommand { get; init; }
        public ICommand DecrementMinuteCommand { get; init; }

        public event Action? RequestClose;
        public event Action? RequestSaveAndAdd;

        public FoodInsertViewModel(IDiaryService diaryService, ISettingsService settingsService)
        {
            _diaryService = diaryService;
            _settingsService = settingsService;

            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => IsValid);
            SaveAndAddCommand = new RelayCommand(async () => await SaveAndAddAsync(), () => IsValid);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke());
            
            IncrementHourCommand = new RelayCommand(() => MealHour = (MealHour + 1) % 24);
            DecrementHourCommand = new RelayCommand(() => MealHour = (MealHour - 1 + 24) % 24);
            IncrementMinuteCommand = new RelayCommand(() => MealMinute = (MealMinute + 5) % 60);
            DecrementMinuteCommand = new RelayCommand(() => MealMinute = (MealMinute - 5 + 60) % 60);

            UpdateAutoMealType();
        }

        public Func<FoodEntry, MealType, Task>? OnSave { get; set; }

        /// <summary>
        /// Imposta l'orario del pasto direttamente da un <see cref="TimeSpan"/> (usato dal TimePicker nativo WinUI 3).
        /// </summary>
        public void SetMealTime(TimeSpan time)
        {
            MealHour   = time.Hours;
            MealMinute = time.Minutes;
        }

        private async Task SaveAsync()
        {
            if (!IsValid) return;
            await SaveEntryAsync();
            RequestClose?.Invoke();
        }

        private async Task SaveAndAddAsync()
        {
            if (!IsValid) return;
            await SaveEntryAsync();
            RequestSaveAndAdd?.Invoke();
            ResetForm();
        }

        private async Task SaveEntryAsync()
        {
            var time = TimeSpan.Parse(MealTimeText);
            var entry = new FoodEntry
            {
                MealTime = time,
                FoodName = FoodName.Trim(),
                PortionGrams = double.TryParse(PortionGramsText, out var pg) ? pg : 0,
                ChoGrams = double.TryParse(ChoGramsText, out var cg) ? cg : 0,
                CreatedAt = DateTime.Now
            };
            if (OnSave != null)
                await OnSave(entry, SelectedMealType);
        }

        private void SetTimeFromMealType(MealType mealType)
        {
            var ranges = _settingsService.Settings.MealTimeRanges;
            var range = ranges.FirstOrDefault(r => r.MealType == mealType);
            if (range != null && TimeSpan.TryParse(range.Start, out var start))
            {
                _updatingFromMealType = true;
                _mealHour = start.Hours;
                _mealMinute = start.Minutes;
                var newText = $"{_mealHour:D2}:{_mealMinute:D2}";
                _mealTimeText = newText;
                OnPropertyChanged(nameof(MealHour));
                OnPropertyChanged(nameof(MealMinute));
                OnPropertyChanged(nameof(MealHourDisplay));
                OnPropertyChanged(nameof(MealMinuteDisplay));
                OnPropertyChanged(nameof(MealTimeText));
                OnPropertyChanged(nameof(MealTime));
                ValidateTime();
                _updatingFromMealType = false;
            }
        }

        private void SyncTimeTextFromParts()
        {
            if (_updatingFromMealType) return;
            var newText = $"{_mealHour:D2}:{_mealMinute:D2}";
            _mealTimeText = newText;
            OnPropertyChanged(nameof(MealTimeText));
            OnPropertyChanged(nameof(MealTime));
            UpdateAutoMealType();
            ValidateTime();
        }

        private void UpdateAutoMealType()
        {
            if (ManualMealOverride) return;
            if (TimeSpan.TryParse(MealTimeText, out var time))
            {
                var ranges = _settingsService.Settings.MealTimeRanges;
                var newType = _diaryService.GetMealTypeForTime(time, ranges);
                // Update the backing field directly to avoid triggering SetTimeFromMealType
                if (_selectedMealType != newType)
                {
                    _selectedMealType = newType;
                    OnPropertyChanged(nameof(SelectedMealType));
                    OnPropertyChanged(nameof(SelectedMealTypeOption));
                }
            }
        }

        private void ValidateTime()
        {
            TimeError = TimeSpan.TryParse(MealTimeText, out _) ? string.Empty : "Formato ora non valido (HH:mm)";
            OnPropertyChanged(nameof(IsValid));
        }

        private void ValidateFoodName()
        {
            NameError = string.IsNullOrWhiteSpace(FoodName) ? "Il nome alimento è obbligatorio" : string.Empty;
            OnPropertyChanged(nameof(IsValid));
        }

        private void ValidateGrams()
        {
            if (!double.TryParse(PortionGramsText, out var v) || v < 0)
                GramsError = "Valore non valido (>= 0)";
            else
                GramsError = string.Empty;
            OnPropertyChanged(nameof(IsValid));
        }

        private void ValidateCho()
        {
            if (!double.TryParse(ChoGramsText, out var v) || v < 0)
                ChoError = "Valore non valido (>= 0)";
            else
                ChoError = string.Empty;
            OnPropertyChanged(nameof(IsValid));
        }

        private void ResetForm()
        {
            _mealHour = DateTime.Now.Hour;
            _mealMinute = RoundMinuteTo5(DateTime.Now.Minute);
            OnPropertyChanged(nameof(MealHour));
            OnPropertyChanged(nameof(MealMinute));
            OnPropertyChanged(nameof(MealHourDisplay));
            OnPropertyChanged(nameof(MealMinuteDisplay));
            MealTimeText = $"{_mealHour:D2}:{_mealMinute:D2}";
            FoodName = string.Empty;
            PortionGramsText = "0";
            ChoGramsText = "0";
            ManualMealOverride = false;
            TimeError = NameError = GramsError = ChoError = string.Empty;
        }
    }

    public class MealTypeOption
    {
        public MealType Value { get; }
        public string Label { get; }

        public MealTypeOption(MealType type)
        {
            Value = type;
            Label = type switch
            {
                MealType.Colazione => "Colazione",
                MealType.MerendaMattina => "Merenda mattina",
                MealType.Pranzo => "Pranzo",
                MealType.MerendaPomeriggio => "Merenda pomeriggio",
                MealType.Cena => "Cena",
                MealType.DopoCena => "Dopo cena",
                _ => type.ToString()
            };
        }
    }
}
