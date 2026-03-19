using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DDiary.Commands;
using DDiary.Models;
using DDiary.Services;

namespace DDiary.ViewModels
{
    /// <summary>
    /// ViewModel per la voce di alimento singola.
    /// </summary>
    public class FoodEntryViewModel : BaseViewModel
    {
        private readonly FoodEntry _model;

        public int Id => _model.Id;
        public int MealSectionId => _model.MealSectionId;

        private string _foodName;
        public string FoodName
        {
            get => _foodName;
            set { SetProperty(ref _foodName, value); _model.FoodName = value; }
        }

        private double _portionGrams;
        public double PortionGrams
        {
            get => _portionGrams;
            set { SetProperty(ref _portionGrams, value); _model.PortionGrams = value; }
        }

        private double _choGrams;
        public double ChoGrams
        {
            get => _choGrams;
            set { SetProperty(ref _choGrams, value); _model.ChoGrams = value; }
        }

        private TimeSpan _mealTime;
        public TimeSpan MealTime
        {
            get => _mealTime;
            set { SetProperty(ref _mealTime, value); _model.MealTime = value; }
        }

        public string MealTimeDisplay => _mealTime.ToString(@"hh\:mm");

        public FoodEntry Model => _model;

        public FoodEntryViewModel(FoodEntry model)
        {
            _model = model;
            _foodName = model.FoodName;
            _portionGrams = model.PortionGrams;
            _choGrams = model.ChoGrams;
            _mealTime = model.MealTime;
        }
    }

    /// <summary>
    /// ViewModel per una sezione pasto.
    /// </summary>
    public class MealSectionViewModel : BaseViewModel
    {
        private readonly MealSection _model;
        public int Id => _model.Id;
        public MealType MealType => _model.MealType;

        public string MealTypeLabel => _model.MealType switch
        {
            MealType.Colazione => "Colazione",
            MealType.MerendaMattina => "Merenda mattina",
            MealType.Pranzo => "Pranzo",
            MealType.MerendaPomeriggio => "Merenda pomeriggio",
            MealType.Cena => "Cena",
            MealType.DopoCena => "Dopo cena",
            _ => _model.MealType.ToString()
        };

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public bool HasFoodEntries => FoodEntries.Count > 0;

        public ICommand ToggleSectionCommand { get; }

        public ObservableCollection<FoodEntryViewModel> FoodEntries { get; } = new();

        private double _totalCho;
        public double TotalCho
        {
            get => _totalCho;
            set { SetProperty(ref _totalCho, value); _model.TotalCho = value; }
        }

        private double _insulinCarbRatio;
        /// <summary>Displayed in the UI as "Unità Insulina". Property name kept for backwards-compatibility with existing data.</summary>
        public double InsulinCarbRatio
        {
            get => _insulinCarbRatio;
            set { SetProperty(ref _insulinCarbRatio, value); _model.InsulinCarbRatio = value; }
        }

        private double? _glycemiaBefore;
        public double? GlycemiaBefore
        {
            get => _glycemiaBefore;
            set { SetProperty(ref _glycemiaBefore, value); _model.GlycemiaBefore = value; }
        }

        private double? _glycemiaAfter;
        public double? GlycemiaAfter
        {
            get => _glycemiaAfter;
            set { SetProperty(ref _glycemiaAfter, value); _model.GlycemiaAfter = value; }
        }

        private string _notes = string.Empty;
        public string Notes
        {
            get => _notes;
            set { SetProperty(ref _notes, value); _model.Notes = value; }
        }

        public MealSection Model => _model;

        public MealSectionViewModel(MealSection model)
        {
            _model = model;
            _totalCho = model.TotalCho;
            _insulinCarbRatio = model.InsulinCarbRatio;
            _glycemiaBefore = model.GlycemiaBefore;
            _glycemiaAfter = model.GlycemiaAfter;
            _notes = model.Notes;

            foreach (var entry in model.FoodEntries.OrderBy(f => f.SortOrder))
                FoodEntries.Add(new FoodEntryViewModel(entry));

            // Start expanded only when the section already has entries
            _isExpanded = FoodEntries.Count > 0;

            ToggleSectionCommand = new RelayCommand(() => IsExpanded = !IsExpanded);
        }

        /// <summary>Sum of all food entries' portion weights for this meal.</summary>
        public double TotalPortionGrams => FoodEntries.Sum(f => f.PortionGrams);

        public void RecalculateTotalCho()
        {
            TotalCho = FoodEntries.Sum(f => f.ChoGrams);
            OnPropertyChanged(nameof(TotalPortionGrams));
        }

        public void AddFoodEntry(FoodEntry entry)
        {
            FoodEntries.Add(new FoodEntryViewModel(entry));
            RecalculateTotalCho();
            OnPropertyChanged(nameof(HasFoodEntries));
            IsExpanded = true;
        }

        public void RemoveFoodEntry(int entryId)
        {
            var vm = FoodEntries.FirstOrDefault(f => f.Id == entryId);
            if (vm != null)
            {
                FoodEntries.Remove(vm);
                RecalculateTotalCho();
                OnPropertyChanged(nameof(HasFoodEntries));
                if (FoodEntries.Count == 0)
                    IsExpanded = false;
            }
        }
    }

    /// <summary>
    /// ViewModel principale per il diario giornaliero.
    /// </summary>
    public class DiaryViewModel : BaseViewModel
    {
        private readonly IDiaryService _diaryService;
        private readonly IExportService _exportService;
        private readonly ISettingsService _settingsService;

        private DailyDiary? _model;
        private UserProfile? _userProfile;
        private int _profileId;

        public int DiaryId => _model?.Id ?? 0;

        private DateTime _date = DateTime.Today;
        public DateTime Date
        {
            get => _date;
            set { SetProperty(ref _date, value); }
        }

        private string _notes = string.Empty;
        public string Notes
        {
            get => _notes;
            set { SetProperty(ref _notes, value); if (_model != null) _model.Notes = value; }
        }

        private string _physicalActivityNotes = string.Empty;
        public string PhysicalActivityNotes
        {
            get => _physicalActivityNotes;
            set { SetProperty(ref _physicalActivityNotes, value); if (_model != null) _model.PhysicalActivityNotes = value; }
        }

        private bool _isClassicMode;
        public bool IsClassicMode
        {
            get => _isClassicMode;
            set { SetProperty(ref _isClassicMode, value); OnPropertyChanged(nameof(ViewModeToggleLabel)); }
        }

        public string ViewModeToggleLabel => _isClassicMode ? "≡ Vista moderna" : "⊞ Tabella classica";

        public ObservableCollection<MealSectionViewModel> MealSections { get; } = new();

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand SaveDiaryCommand { get; }
        public ICommand DeleteFoodEntryCommand { get; }
        public ICommand ToggleViewModeCommand { get; }
        public ICommand AddInlineFoodEntryCommand { get; }

        public DiaryViewModel(IDiaryService diaryService, IExportService exportService, ISettingsService settingsService)
        {
            _diaryService = diaryService;
            _exportService = exportService;
            _settingsService = settingsService;

            SaveDiaryCommand = new RelayCommand(async () => await SaveAsync());
            DeleteFoodEntryCommand = new RelayCommand<int>(async id => await DeleteFoodEntryAsync(id));
            ToggleViewModeCommand = new RelayCommand(async () => await ToggleViewModeAsync());
            AddInlineFoodEntryCommand = new RelayCommand<MealType>(async mt => await AddInlineFoodEntryAsync(mt));
        }

        public async Task LoadAsync(int profileId, DateTime? date = null)
        {
            IsBusy = true;
            _profileId = profileId;
            try
            {
                DailyDiary diary;
                if (date.HasValue)
                    diary = await _diaryService.GetDiaryByDateAsync(profileId, date.Value)
                            ?? await _diaryService.GetOrCreateTodayAsync(profileId);
                else
                    diary = await _diaryService.GetOrCreateTodayAsync(profileId);

                _model = diary;
                _userProfile = diary.UserProfile;
                Date = diary.Date;
                Notes = diary.Notes;
                PhysicalActivityNotes = diary.PhysicalActivityNotes;

                IsClassicMode = _settingsService.Settings.DiaryViewMode == "Classic";

                MealSections.Clear();
                foreach (var section in diary.MealSections.OrderBy(s => s.MealType))
                    MealSections.Add(new MealSectionViewModel(section));

                // Ensure all meal types are present
                foreach (MealType mt in Enum.GetValues(typeof(MealType)))
                {
                    if (!MealSections.Any(s => s.MealType == mt))
                    {
                        var newSection = new MealSection { MealType = mt, DailyDiaryId = diary.Id };
                        diary.MealSections.Add(newSection);
                        MealSections.Add(new MealSectionViewModel(newSection));
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task SaveAsync()
        {
            if (_model == null) return;
            IsBusy = true;
            try
            {
                _model.Notes = Notes;
                _model.PhysicalActivityNotes = PhysicalActivityNotes;
                await _diaryService.SaveDiaryAsync(_model);
                StatusMessage = "Diario salvato.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void AddFoodEntryToSection(MealType mealType, FoodEntry entry)
        {
            var section = MealSections.FirstOrDefault(s => s.MealType == mealType);
            section?.AddFoodEntry(entry);
        }

        private async Task DeleteFoodEntryAsync(int entryId)
        {
            await _diaryService.DeleteFoodEntryAsync(entryId);
            foreach (var section in MealSections)
                section.RemoveFoodEntry(entryId);
        }

        private async Task ToggleViewModeAsync()
        {
            IsClassicMode = !IsClassicMode;
            _settingsService.Settings.DiaryViewMode = IsClassicMode ? "Classic" : "Modern";
            await _settingsService.SaveAsync();
        }

        private async Task AddInlineFoodEntryAsync(MealType mealType)
        {
            if (_model == null) return;
            var entry = new FoodEntry
            {
                MealTime = TimeSpan.FromHours(DateTime.Now.Hour),
                FoodName = "",
                PortionGrams = 0,
                ChoGrams = 0,
                CreatedAt = DateTime.Now
            };
            var added = await _diaryService.AddFoodEntryAsync(_model.Id, _profileId, entry, mealType);
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                AddFoodEntryToSection(mealType, added);
            });
        }

        public DailyDiary? GetModel() => _model;
    }
}
