using System.ComponentModel.DataAnnotations;

namespace TurnTrackerAspNetCore.ViewModels
{
    public class TrackedTaskEditViewModel
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Display(Name="Team Based")]
        public bool TeamBased { get; set; }
    }
}
