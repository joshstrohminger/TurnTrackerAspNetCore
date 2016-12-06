using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace TurnTrackerAspNetCore.Entities
{
    public enum PeriodUnit
    {
        Minutes,
        Hours,
        Days,
        Weeks,
        Months,
        Years
    }

    public static class TrackedTaskExtensions
    {
        public static TimeSpan GetPeriod(this TrackedTask task)
        {
            switch (task.Unit)
            {
                case PeriodUnit.Minutes:
                    return TimeSpan.FromMinutes((double)task.Period);
                case PeriodUnit.Hours:
                    return TimeSpan.FromHours((double)task.Period);
                case PeriodUnit.Days:
                    return TimeSpan.FromDays((double)task.Period);
                case PeriodUnit.Weeks:
                    return TimeSpan.FromDays(7 * (double)task.Period);
                case PeriodUnit.Months:
                    return TimeSpan.FromDays(365.0 / 12 * (double)task.Period);
                case PeriodUnit.Years:
                    return TimeSpan.FromDays(365 * (double)task.Period);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static bool AccessDenied(this TrackedTask task, string userId)
        {
            return task == null || (task.UserId != userId && (task.Participants?.All(x => x.UserId != userId) ?? true));
        }

        public static void PopulateLatestTurnInfo(this TrackedTask task, Dictionary<long, List<TurnCount>> counts, Dictionary<TrackedTask, TurnCount> taskCounts, List<Turn> latest )
        {
            List<TurnCount> count;
            counts.TryGetValue(task.Id, out count);
            task.LastTaken = latest.FirstOrDefault(x => x.TaskId == task.Id)?.Taken;
            taskCounts.Add(task, count?.FirstOrDefault());

            if (null == task.LastTaken)
            {
                task.Overdue = true;
            }
            else
            {
                var period = task.GetPeriod();
                var elapsed = DateTimeOffset.UtcNow - task.LastTaken.Value;
                task.Overdue = 0m != task.Period && elapsed > period;
                task.DueTimeSpan = elapsed - period;
            }
        }
    }

    public class TrackedTask
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public decimal Period { get; set; }

        [Required]
        public PeriodUnit Unit { get; set; }

        [Display(Name="Team Based")]
        public bool TeamBased { get; set; }
        
        public DateTimeOffset Created { get; set; }
        
        public DateTimeOffset Modified { get; set; }

        public List<Turn> Turns { get; set; }

        public List<Participant> Participants { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }

        [NotMapped]
        public DateTimeOffset? LastTaken { get; set; }

        [NotMapped]
        public bool Overdue { get; set; }

        [NotMapped]
        public TimeSpan DueTimeSpan { get; set; }
    }
}
