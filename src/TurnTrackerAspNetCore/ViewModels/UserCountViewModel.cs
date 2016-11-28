using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.ViewModels
{
    public class UserCountViewModel
    {
        public string Name { get; set; }
        public int TotalTurns { get; set; }

        public UserCountViewModel(TurnCount count)
        {
            Name = count.DisplayName ?? count.UserName;
            TotalTurns = count.TotalTurns;
        }
    }
}
