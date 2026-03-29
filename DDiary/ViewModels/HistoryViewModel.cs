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
    /// ViewModel per il pannello storico con filtri e ricerca.
    /// </summary>
    public class HistoryViewModel : BaseViewModel
    {
        private readonly IDiaryService _diaryService;

        public ObservableCollection<DiaryHistoryItemViewModel> Diaries { get; } = new();

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { SetProperty(ref _searchText, value); _ = ApplyFiltersAsync(); }
        }

        private int? _filterYear;
        public int? FilterYear
        {
            get => _filterYear;
            set { SetProperty(ref _filterYear, value); _ = ApplyFiltersAsync(); }
        }

        private int? _filterMonth;
        public int? FilterMonth
        {
            get => _filterMonth;
            set { SetProperty(ref _filterMonth, value); _ = ApplyFiltersAsync(); }
        }

        private bool _sortNewest = true;
        public bool SortNewest
        {
            get => _sortNewest;
            set { SetProperty(ref _sortNewest, value); _ = ApplyFiltersAsync(); }
        }

        private bool _isEmpty;
        public bool IsEmpty
        {
            get => _isEmpty;
            set => SetProperty(ref _isEmpty, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand GoToTodayCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand OpenOrCreateDiaryCommand { get; }
        public ICommand SetYesterdayCommand { get; }

        public event Action<DateTime>? DiarySelected;
        public event Action<DateTime>? OpenOrCreateDiaryRequested;

        private DateTime _newDiaryDate = DateTime.Today;
        public DateTime NewDiaryDate
        {
            get => _newDiaryDate;
            set => SetProperty(ref _newDiaryDate, value);
        }

        public HistoryViewModel(IDiaryService diaryService)
        {
            _diaryService = diaryService;
            RefreshCommand = new RelayCommand(async () => await LoadAsync(0));
            GoToTodayCommand = new RelayCommand(() => { NewDiaryDate = DateTime.Today; OpenOrCreateDiaryRequested?.Invoke(DateTime.Today); });
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            OpenOrCreateDiaryCommand = new RelayCommand(() => OpenOrCreateDiaryRequested?.Invoke(NewDiaryDate));
            SetYesterdayCommand = new RelayCommand(() => NewDiaryDate = DateTime.Today.AddDays(-1));
        }

        private int _currentProfileId;

        public async Task LoadAsync(int profileId)
        {
            _currentProfileId = profileId;
            await ApplyFiltersAsync();
        }

        private async Task ApplyFiltersAsync()
        {
            IsBusy = true;
            try
            {
                var all = await _diaryService.SearchDiariesAsync(_currentProfileId, SearchText, FilterYear, FilterMonth);
                var list = SortNewest
                    ? all.OrderByDescending(d => d.Date).ToList()
                    : all.OrderBy(d => d.Date).ToList();

                Diaries.Clear();
                foreach (var d in list)
                    Diaries.Add(new DiaryHistoryItemViewModel(d));

                IsEmpty = !Diaries.Any();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ClearFilters()
        {
            _searchText = string.Empty;
            _filterYear = null;
            _filterMonth = null;
            OnPropertyChanged(nameof(SearchText));
            OnPropertyChanged(nameof(FilterYear));
            OnPropertyChanged(nameof(FilterMonth));
            _ = ApplyFiltersAsync();
        }

        public void SelectDiary(DiaryHistoryItemViewModel item)
        {
            DiarySelected?.Invoke(item.Date);
        }
    }

    public class DiaryHistoryItemViewModel : BaseViewModel
    {
        private readonly DailyDiary _model;
        public int Id => _model.Id;
        public DateTime Date => _model.Date;
        public string DateDisplay => _model.Date.ToString("dddd dd MMMM yyyy");
        public string ShortDate => _model.Date.ToString("dd/MM/yyyy");
        public bool IsToday => _model.Date.Date == DateTime.Today;

        public int FoodEntriesCount => _model.MealSections.Sum(s => s.FoodEntries.Count);
        public double TotalDayCho => _model.MealSections.Sum(s => s.TotalCho);

        public DiaryHistoryItemViewModel(DailyDiary model)
        {
            _model = model;
        }
    }
}
