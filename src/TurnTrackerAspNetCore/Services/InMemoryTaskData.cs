using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.Services
{
    public class InMemoryTaskData : ITaskData
    {
        private static readonly List<TrackedTask> Tasks;
        private static readonly List<Turn> Turns = new List<Turn>();

        static InMemoryTaskData()
        {
            var modified = DateTime.UtcNow;
            var created = modified.AddDays(-5);
            Tasks = new List<TrackedTask>
            {
                new TrackedTask {Id = 1, Name = "Clean Litter Box", Unit = PeriodUnit.Days, Period = 2m, CreatedUtc = created, ModifiedUtc = modified},
                new TrackedTask {Id = 2, Name = "Take Out Trash", Unit = PeriodUnit.Weeks, Period = 1m, CreatedUtc = created, ModifiedUtc = modified},
                new TrackedTask {Id = 3, Name = "Feed Snake", Unit = PeriodUnit.Days, Period = 20m, CreatedUtc = created, ModifiedUtc = modified}
            };

            var turnId = 1;
            foreach (var task in Tasks)
            {
                task.Turns = new List<Turn>();
                for (var i = 0; i < task.Id; i++)
                {
                    var date = created.AddHours(i);
                    task.Turns.Add(new Turn {Id = turnId++, TakenUtc = date, CreatedUtc = date, ModifiedUtc = date});
                }
                Turns.AddRange(task.Turns);
                Turns = Turns.OrderByDescending(x => x.TakenUtc).ToList();
            }
        }

        public IEnumerable<TrackedTask> GetAll()
        {
            return Tasks;
        }

        public TrackedTask Get(long id)
        {
            return Tasks.FirstOrDefault(task => task.Id == id);
        }

        public TrackedTask Add(TrackedTask newTask)
        {
            newTask.Id = Tasks.Max(x => x.Id) + 1;
            Tasks.Add(newTask);
            return newTask;
        }

        public void Commit()
        {
            // do nothing in memory
        }

        public bool TakeTurn(long taskId)
        {
            var task = Get(taskId);
            if (null == task)
            {
                return false;
            }
            var date = DateTime.UtcNow;
            task.Turns.Add(new Turn {Id = Turns.Max(x => x.Id) + 1, CreatedUtc = date, TakenUtc = date, ModifiedUtc = date, TrackedTaskId = taskId});
            return true;
        }
    }
}
