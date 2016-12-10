using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.ViewModels.Task
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
