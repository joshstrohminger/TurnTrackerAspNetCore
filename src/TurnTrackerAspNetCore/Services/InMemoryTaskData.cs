using System.Collections.Generic;
using System.Linq;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.Services
{
    public class InMemoryTaskData : ITaskData
    {
        private static readonly List<Task> Tasks;

        static InMemoryTaskData()
        { 
            Tasks = new List<Task>
            {
                new Task {Id = 1, Name = "Clean Litter Box"},
                new Task {Id = 2, Name = "Take Out Trash" },
                new Task {Id = 3, Name = "Feed Snake" }
            };
        }

        public IEnumerable<Task> GetAll()
        {
            return Tasks;
        }
    }
}
