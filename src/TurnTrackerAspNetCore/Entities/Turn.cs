using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TurnTrackerAspNetCore.Entities
{
    public class Turn
    {
        public long Id { get; set; }
        public long TrackedTaskId { get; set; }
        public DateTime TakenUtc { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime ModifiedUtc { get; set; }
    }
}
