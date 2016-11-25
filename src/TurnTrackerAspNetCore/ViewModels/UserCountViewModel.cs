using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.ViewModels
{
    public class UserCountViewModel
    {
        public User User { get; set; }
        public int TurnsTaken { get; set; }
        public int TurnsOffset { get; set; }
        public int TotalTurns { get; set; }
        public bool Active { get; set; }

        public UserCountViewModel(User user, int taken, int offset, bool active)
        {
            User = user;
            TurnsTaken = taken;
            TurnsOffset = offset;
            TotalTurns = TurnsTaken + TurnsOffset;
            Active = active;
        }
    }
}
