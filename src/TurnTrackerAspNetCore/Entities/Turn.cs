using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace TurnTrackerAspNetCore.Entities
{
    public class Turn
    {
        public long Id { get; set; }
        public long TrackedTaskId { get; set; }
        public TrackedTask Task { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTimeOffset Taken { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTimeOffset Created { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset Modified { get; set; }
    }
}
