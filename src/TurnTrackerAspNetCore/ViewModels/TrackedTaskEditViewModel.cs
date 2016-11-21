using System.ComponentModel.DataAnnotations;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.ViewModels
{
    public class TrackedTaskEditViewModel
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public decimal Period { get; set; }

        [Required]
        public PeriodUnit Unit { get; set; }

        [Display(Name = "Team Based")]
        public bool TeamBased { get; set; }
    }
}
