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

        private string _mealTimeText = DateTime.Now.ToString("HH:mm");
        public string MealTimeText
        {
            get => _mealTimeText;
            set
            {
                SetProperty(ref _mealTimeText, value);
                UpdateAutoMealType();
                ValidateTime();
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
            set => SetProperty(ref _selectedMealType, value);
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

        public ICommand SaveCommand { get; }
        public ICommand SaveAndAddCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action? RequestClose;
        public event Action? RequestSaveAndAdd;

        public FoodInsertViewModel(IDiaryService diaryService, ISettingsService settingsService)
        {
            _diaryService = diaryService;
            _settingsService = settingsService;

            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => IsValid);
            SaveAndAddCommand = new RelayCommand(async () => await SaveAndAddAsync(), () => IsValid);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke());

            UpdateAutoMealType();
        }

        public Func<FoodEntry, MealType, Task>? OnSave { get; set; }

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

        private void UpdateAutoMealType()
        {
            if (ManualMealOverride) return;
            if (TimeSpan.TryParse(MealTimeText, out var time))
            {
                var ranges = _settingsService.Settings.MealTimeRanges;
                SelectedMealType = _diaryService.GetMealTypeForTime(time, ranges);
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
            MealTimeText = DateTime.Now.ToString("HH:mm");
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
