using System.Collections.Generic;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.ViewModels.Account
{
    public class ProfileViewModel
    {
        public User User { get; set; }
        public IList<string> Roles { get; set; }
    }
}
