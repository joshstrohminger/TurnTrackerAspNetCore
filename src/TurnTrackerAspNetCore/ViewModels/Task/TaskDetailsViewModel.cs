﻿using System.Collections.Generic;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.ViewModels.Task
{
    public class TaskDetailsViewModel
    {
        public TrackedTask Task { get; set; }
        public List<UserCountViewModel> Counts { get; set; }
        public int MaxTurns { get; set; }
        public bool CanTakeTurn { get; set; }
        public bool CanDeleteTask { get; set; }
        public string Error { get; set; }
    }
}
