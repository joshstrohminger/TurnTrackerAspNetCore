using System;
using System.ComponentModel.DataAnnotations;

namespace TurnTrackerAspNetCore.Entities
{
    public class Turn
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long TrackedTaskId { get; set; }
        public TrackedTask Task { get; set; }

        [Required]
        public string UserId { get; set; }
        public User User { get; set; }
        
        public DateTimeOffset Taken { get; set; }
        
        public DateTimeOffset Created { get; set; }
        
        public DateTimeOffset Modified { get; set; }
    }
}
