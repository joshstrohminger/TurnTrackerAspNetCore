using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TurnTrackerAspNetCore.Entities
{
    public class Participant
    {
        public long TaskId { get; set; }
        public TrackedTask Task { get; set; }
        
        public string UserId { get; set; }
        public User User { get; set; }
    }
}
