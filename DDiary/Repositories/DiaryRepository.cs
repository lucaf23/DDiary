using Microsoft.EntityFrameworkCore;
using DDiary.Data;
using DDiary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DDiary.Repositories
{
    /// <summary>
    /// Implementazione EF Core del repository per i diari giornalieri.
    /// </summary>
    public class DiaryRepository : IDiaryRepository
    {
        private readonly DDiaryDbContext _context;

        public DiaryRepository(DDiaryDbContext context)
        {
            _context = context;
        }

        public async Task<DailyDiary?> GetByIdAsync(int id)
        {
            return await _context.DailyDiaries
                .Include(d => d.MealSections)
                    .ThenInclude(m => m.FoodEntries)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<IEnumerable<DailyDiary>> GetAllAsync()
        {
            return await _context.DailyDiaries
                .Include(d => d.MealSections)
                    .ThenInclude(m => m.FoodEntries)
                .OrderByDescending(d => d.Date)
                .ToListAsync();
        }

        public async Task<DailyDiary?> GetByDateAsync(int profileId, DateTime date)
        {
            return await _context.DailyDiaries
                .Include(d => d.MealSections)
                    .ThenInclude(m => m.FoodEntries)
                .FirstOrDefaultAsync(d => d.UserProfileId == profileId && d.Date.Date == date.Date);
        }

        public async Task<IEnumerable<DailyDiary>> GetByProfileAsync(int profileId)
        {
            return await _context.DailyDiaries
                .Include(d => d.MealSections)
                    .ThenInclude(m => m.FoodEntries)
                .Where(d => d.UserProfileId == profileId)
                .OrderByDescending(d => d.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<DailyDiary>> GetByMonthAsync(int profileId, int year, int month)
        {
            return await _context.DailyDiaries
                .Include(d => d.MealSections)
                    .ThenInclude(m => m.FoodEntries)
                .Where(d => d.UserProfileId == profileId && d.Date.Year == year && d.Date.Month == month)
                .OrderByDescending(d => d.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<DailyDiary>> GetByYearAsync(int profileId, int year)
        {
            return await _context.DailyDiaries
                .Include(d => d.MealSections)
                    .ThenInclude(m => m.FoodEntries)
                .Where(d => d.UserProfileId == profileId && d.Date.Year == year)
                .OrderByDescending(d => d.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<DailyDiary>> GetByWeekAsync(int profileId, DateTime weekStart)
        {
            var weekEnd = weekStart.AddDays(6);
            return await _context.DailyDiaries
                .Include(d => d.MealSections)
                    .ThenInclude(m => m.FoodEntries)
                .Where(d => d.UserProfileId == profileId && d.Date.Date >= weekStart.Date && d.Date.Date <= weekEnd.Date)
                .OrderByDescending(d => d.Date)
                .ToListAsync();
        }

        public async Task<DailyDiary> GetOrCreateTodayAsync(int profileId)
        {
            var today = DateTime.Today;
            var diary = await GetByDateAsync(profileId, today);
            if (diary == null)
            {
                diary = new DailyDiary
                {
                    UserProfileId = profileId,
                    Date = today,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                // Create all meal sections
                foreach (MealType mt in Enum.GetValues(typeof(MealType)))
                {
                    diary.MealSections.Add(new MealSection { MealType = mt });
                }
                diary = await AddAsync(diary);
            }
            return diary;
        }

        public async Task<DailyDiary> AddAsync(DailyDiary entity)
        {
            entity.CreatedAt = DateTime.Now;
            entity.UpdatedAt = DateTime.Now;
            _context.DailyDiaries.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<DailyDiary> UpdateAsync(DailyDiary entity)
        {
            entity.UpdatedAt = DateTime.Now;
            _context.DailyDiaries.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(int id)
        {
            var diary = await _context.DailyDiaries.FindAsync(id);
            if (diary != null)
            {
                _context.DailyDiaries.Remove(diary);
                await _context.SaveChangesAsync();
            }
        }
    }
}
