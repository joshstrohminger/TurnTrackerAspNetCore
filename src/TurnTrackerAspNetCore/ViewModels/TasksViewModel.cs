using System.Collections.Generic;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.ViewModels
{
    public class TasksViewModel
    {
        public Dictionary<TrackedTask,TurnCount> TaskCounts { get; set; }
        public string Error { get; set; }
    }
}
