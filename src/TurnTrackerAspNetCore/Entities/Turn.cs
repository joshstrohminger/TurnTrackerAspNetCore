using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TurnTrackerAspNetCore.Entities
{
    public class Turn
    {
        [Key]
        public long Id { get; set; }

        public long TrackedTaskId { get; set; }
        public TrackedTask Task { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }
        
        public DateTimeOffset Taken { get; set; }
        
        public DateTimeOffset Created { get; set; }
        
        public DateTimeOffset Modified { get; set; }
    }
}
