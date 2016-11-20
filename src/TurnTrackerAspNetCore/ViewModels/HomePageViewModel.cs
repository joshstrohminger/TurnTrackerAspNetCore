using System.Collections.Generic;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.ViewModels
{
    public class HomePageViewModel
    {
        public IEnumerable<TrackedTask> Tasks { get; set; }
    }
}
