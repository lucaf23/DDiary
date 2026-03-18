using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DDiary.Models;
using DDiary.Repositories;

namespace DDiary.Services
{
    /// <summary>
    /// Servizio principale per la gestione del diario.
    /// </summary>
    public interface IDiaryService
    {
        Task<DailyDiary?> GetDiaryByDateAsync(int profileId, DateTime date);
        Task<DailyDiary> GetOrCreateTodayAsync(int profileId);
        Task<IEnumerable<DailyDiary>> GetDiariesAsync(int profileId);
        Task<IEnumerable<DailyDiary>> SearchDiariesAsync(int profileId, string? query, int? year, int? month);
        Task<DailyDiary> SaveDiaryAsync(DailyDiary diary);
        Task<FoodEntry> AddFoodEntryAsync(int diaryId, int profileId, FoodEntry entry, MealType? mealTypeOverride = null);
        Task<FoodEntry> UpdateFoodEntryAsync(FoodEntry entry);
        Task DeleteFoodEntryAsync(int entryId);
        Task UpdateMealSectionAsync(MealSection section);
        MealType GetMealTypeForTime(TimeSpan time, IEnumerable<MealTimeRangeSetting> ranges);
    }

    public class DiaryService : IDiaryService
    {
        private readonly IDiaryRepository _diaryRepo;

        public DiaryService(IDiaryRepository diaryRepo)
        {
            _diaryRepo = diaryRepo;
        }

        public Task<DailyDiary?> GetDiaryByDateAsync(int profileId, DateTime date)
            => _diaryRepo.GetByDateAsync(profileId, date);

        public Task<DailyDiary> GetOrCreateTodayAsync(int profileId)
            => _diaryRepo.GetOrCreateTodayAsync(profileId);

        public Task<IEnumerable<DailyDiary>> GetDiariesAsync(int profileId)
            => _diaryRepo.GetByProfileAsync(profileId);

        public async Task<IEnumerable<DailyDiary>> SearchDiariesAsync(int profileId, string? query, int? year, int? month)
        {
            var all = await _diaryRepo.GetByProfileAsync(profileId);
            var result = new List<DailyDiary>(all);

            if (year.HasValue)
                result = result.FindAll(d => d.Date.Year == year.Value);
            if (month.HasValue)
                result = result.FindAll(d => d.Date.Month == month.Value);
            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.ToLowerInvariant();
                result = result.FindAll(d =>
                    d.Date.ToString("dd/MM/yyyy").Contains(q) ||
                    d.Notes.ToLowerInvariant().Contains(q));
            }

            return result;
        }

        public async Task<DailyDiary> SaveDiaryAsync(DailyDiary diary)
        {
            if (diary.Id == 0)
                return await _diaryRepo.AddAsync(diary);
            return await _diaryRepo.UpdateAsync(diary);
        }

        public async Task<FoodEntry> AddFoodEntryAsync(int diaryId, int profileId, FoodEntry entry, MealType? mealTypeOverride = null)
        {
            var diary = await _diaryRepo.GetByIdAsync(diaryId)
                        ?? throw new InvalidOperationException("Diario non trovato.");

            MealType targetMeal = mealTypeOverride ?? GetMealTypeForTime(entry.MealTime, AppSettings.GetDefaultMealTimeRanges());

            var section = diary.MealSections.FirstOrDefault(s => s.MealType == targetMeal);
            if (section == null)
            {
                section = new MealSection { MealType = targetMeal, DailyDiaryId = diary.Id };
                diary.MealSections.Add(section);
            }

            entry.MealSectionId = section.Id;
            entry.SortOrder = section.FoodEntries.Count;
            section.FoodEntries.Add(entry);
            section.TotalCho = section.FoodEntries.Sum(f => f.ChoGrams);

            await _diaryRepo.UpdateAsync(diary);
            return entry;
        }

        public async Task<FoodEntry> UpdateFoodEntryAsync(FoodEntry entry)
        {
            // Load the full diary to update
            var diary = await _diaryRepo.GetByIdAsync(entry.MealSection?.DailyDiaryId ?? 0);
            if (diary != null)
            {
                var section = diary.MealSections.FirstOrDefault(s => s.Id == entry.MealSectionId);
                if (section != null)
                {
                    var existing = section.FoodEntries.FirstOrDefault(f => f.Id == entry.Id);
                    if (existing != null)
                    {
                        existing.FoodName = entry.FoodName;
                        existing.PortionGrams = entry.PortionGrams;
                        existing.ChoGrams = entry.ChoGrams;
                        existing.MealTime = entry.MealTime;
                        section.TotalCho = section.FoodEntries.Sum(f => f.ChoGrams);
                    }
                    await _diaryRepo.UpdateAsync(diary);
                }
            }
            return entry;
        }

        public async Task DeleteFoodEntryAsync(int entryId)
        {
            var all = await _diaryRepo.GetAllAsync();
            foreach (var diary in all)
            {
                foreach (var section in diary.MealSections)
                {
                    var entry = section.FoodEntries.FirstOrDefault(f => f.Id == entryId);
                    if (entry != null)
                    {
                        section.FoodEntries.Remove(entry);
                        section.TotalCho = section.FoodEntries.Sum(f => f.ChoGrams);
                        await _diaryRepo.UpdateAsync(diary);
                        return;
                    }
                }
            }
        }

        public async Task UpdateMealSectionAsync(MealSection section)
        {
            var diary = await _diaryRepo.GetByIdAsync(section.DailyDiaryId);
            if (diary != null)
            {
                var existing = diary.MealSections.FirstOrDefault(s => s.Id == section.Id);
                if (existing != null)
                {
                    existing.GlycemiaBefore = section.GlycemiaBefore;
                    existing.GlycemiaAfter = section.GlycemiaAfter;
                    existing.InsulinCarbRatio = section.InsulinCarbRatio;
                    existing.TotalCho = section.TotalCho;
                    existing.Notes = section.Notes;
                    await _diaryRepo.UpdateAsync(diary);
                }
            }
        }

        public MealType GetMealTypeForTime(TimeSpan time, IEnumerable<MealTimeRangeSetting> ranges)
        {
            foreach (var range in ranges)
            {
                if (TimeSpan.TryParse(range.Start, out var start) && TimeSpan.TryParse(range.End, out var end))
                {
                    var mr = new MealTimeRange { MealType = range.MealType, StartTime = start, EndTime = end };
                    if (mr.ContainsTime(time))
                        return range.MealType;
                }
            }
            return MealType.Colazione;
        }
    }
}
