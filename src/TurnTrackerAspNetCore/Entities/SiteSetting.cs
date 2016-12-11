using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TurnTrackerAspNetCore.Entities
{
    public class SiteSetting
    {
        public const int MaxValueLength = 1000;

        [Key]
        [Required, MaxLength(500)]
        public string Name { get; set; }

        [Required, MaxLength(100)]
        public string Type { get; set; }
            
        [Required, MaxLength(MaxValueLength)]
        public string Value { get; set; }

        [NotMapped]
        public bool NeedToAdd { get; set; }

        [NotMapped]
        public bool Accessed { get; set; }
    }
}
