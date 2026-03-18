using System;

namespace DDiary.Models
{
    /// <summary>
    /// Fascia oraria per l'auto-mapping pasto per ora.
    /// </summary>
    public class MealTimeRange
    {
        public MealType MealType { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public bool ContainsTime(TimeSpan time)
        {
            if (StartTime <= EndTime)
                return time >= StartTime && time <= EndTime;
            // Wrapping midnight (e.g. 22:00 - 04:59)
            return time >= StartTime || time <= EndTime;
        }
    }
}
