using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DDiary.Models;

namespace DDiary.Repositories
{
    /// <summary>
    /// Repository specializzato per i diari giornalieri.
    /// </summary>
    public interface IDiaryRepository : IRepository<DailyDiary>
    {
        Task<DailyDiary?> GetByDateAsync(int profileId, DateTime date);
        Task<IEnumerable<DailyDiary>> GetByProfileAsync(int profileId);
        Task<IEnumerable<DailyDiary>> GetByMonthAsync(int profileId, int year, int month);
        Task<IEnumerable<DailyDiary>> GetByYearAsync(int profileId, int year);
        Task<IEnumerable<DailyDiary>> GetByWeekAsync(int profileId, DateTime weekStart);
        Task<DailyDiary> GetOrCreateTodayAsync(int profileId);
    }
}
