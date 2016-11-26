using System;
using System.Collections.Generic;
using System.Linq;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.Services
{
    public class InMemoryTaskData : ITaskData
    {
        private static readonly List<TrackedTask> Tasks;
        private static readonly List<Turn> Turns = new List<Turn>();
        private static readonly List<User> Users = new List<User>();

        static InMemoryTaskData()
        {
            var modified = DateTimeOffset.UtcNow;
            var created = modified.AddDays(-5);
            Tasks = new List<TrackedTask>
            {
                new TrackedTask {Id = 1, Name = "Clean Litter Box", Unit = PeriodUnit.Days, Period = 2m, Created = created, Modified = modified},
                new TrackedTask {Id = 2, Name = "Take Out Trash", Unit = PeriodUnit.Weeks, Period = 1m, Created = created, Modified = modified},
                new TrackedTask {Id = 3, Name = "Feed Snake", Unit = PeriodUnit.Days, Period = 20m, Created = created, Modified = modified}
            };

            var turnId = 1;
            foreach (var task in Tasks)
            {
                task.Turns = new List<Turn>();
                for (var i = 0; i < task.Id; i++)
                {
                    var date = created.AddHours(i);
                    task.Turns.Add(new Turn {Id = turnId++, Taken = date, Created = date, Modified = date, TrackedTaskId = task.Id, Task = task});
                }
                Turns.AddRange(task.Turns);
                Turns = Turns.OrderByDescending(x => x.Taken).ToList();
            }
        }

        public IEnumerable<TrackedTask> GetAllTasks()
        {
            return Tasks;
        }

        public IEnumerable<User> GetAllUsers()
        {
            return Users;
        }

        public TrackedTask GetTask(long id)
        {
            return Tasks.FirstOrDefault(task => task.Id == id);
        }

        public TrackedTask GetTaskDetails(long id)
        {
            return GetTask(id);
        }

        public IEnumerable<TrackedTask> GetParticipations(string userId)
        {
            return Users.Where(x => x.Id == userId).SelectMany(x => x.Participations).Select(x => x.Task).ToList();
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

        public bool DeleteTask(long id)
        {
            var task = GetTask(id);
            if (null != task)
            {
                Tasks.Remove(task);
                return true;
            }
            return false;
        }

        public void DeleteTask(TrackedTask task)
        {
            Tasks.Remove(task);
        }

        public long DeleteTurn(long id)
        {
            var turn = Turns.FirstOrDefault(x => x.Id == id);
            if (null != turn)
            {
                Turns.Remove(turn);
                turn.Task.Turns.Remove(turn);
                return turn.Task.Id;
            }
            return 0;
        }

        public void DeleteTurn(Turn turn)
        {
            foreach (var task in Tasks)
            {
                task.Turns.Remove(turn);
            }
            Turns.Remove(turn);
        }

        public Turn GetTurn(long id)
        {
            return Turns.FirstOrDefault(x => x.Id == id);
        }
    }
}
