using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TurnTrackerAspNetCore.Entities
{
    public class TurnCount
    {
        public string DisplayName { get; set; }
        public string UserName { get; set; }
        public string TaskName { get; set; }
        public long TaskId { get; set; }
        public int TotalTurns { get; set; }
        public string UserId { get; set; }
    }
}
