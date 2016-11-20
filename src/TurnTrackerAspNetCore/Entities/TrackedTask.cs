using System.ComponentModel.DataAnnotations;

namespace TurnTrackerAspNetCore.Entities
{
    public class TrackedTask
    {
        public long Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Display(Name="Team Based")]
        public bool TeamBased { get; set; }
    }
}
