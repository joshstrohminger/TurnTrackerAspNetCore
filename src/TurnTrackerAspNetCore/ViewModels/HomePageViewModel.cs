using System.Collections.Generic;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.ViewModels
{
    public class HomePageViewModel
    {
        public List<TrackedTask> Tasks { get; set; }
        public string Error { get; set; }
    }
}
