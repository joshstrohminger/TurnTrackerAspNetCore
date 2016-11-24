using System.Collections.Generic;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.ViewModels
{
    public class TaskDetailsViewModel
    {
        public TrackedTask Task { get; set; }
        public List<UserCountViewModel> ActiveUsers { get; set; }
        public int MaxTurns { get; set; }
    }
}
