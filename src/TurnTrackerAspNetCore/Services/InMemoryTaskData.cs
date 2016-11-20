using System.Collections.Generic;
using System.Linq;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.Services
{
    public class InMemoryTaskData : ITaskData
    {
        private static readonly List<TrackedTask> Tasks;

        static InMemoryTaskData()
        { 
            Tasks = new List<TrackedTask>
            {
                new TrackedTask {Id = 1, Name = "Clean Litter Box"},
                new TrackedTask {Id = 2, Name = "Take Out Trash" },
                new TrackedTask {Id = 3, Name = "Feed Snake" }
            };
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
    }
}
