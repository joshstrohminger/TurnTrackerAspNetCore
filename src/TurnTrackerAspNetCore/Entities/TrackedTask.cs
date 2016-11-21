using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTimeOffset Created { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset Modified { get; set; }

        public List<Turn> Turns { get; set; }
    }
}
