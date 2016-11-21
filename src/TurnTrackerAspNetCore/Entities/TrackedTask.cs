using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TurnTrackerAspNetCore.Entities
{
    public enum PeriodUnit
    {
        Minutes,
        Hours,
        Days,
        Weeks,
        Months,
        Years
    }
    public class TrackedTask
    {
        public long Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public decimal Period { get; set; }

        [Required]
        public PeriodUnit Unit { get; set; }

        [Display(Name="Team Based")]
        public bool TeamBased { get; set; }

        public DateTime CreatedUtc { get; set; }

        public DateTime ModifiedUtc { get; set; }

        public List<Turn> Turns { get; set; }
    }
}
