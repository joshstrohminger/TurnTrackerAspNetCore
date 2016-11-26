using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.ViewModels
{
    public class EditTaskViewModel
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public decimal Period { get; set; }

        [Required]
        public PeriodUnit Unit { get; set; }

        [Display(Name = "Team Based")]
        public bool TeamBased { get; set; }

        public IEnumerable<string> Participants { get; set; }

        public List<SelectListItem> Users { get; set; }

        public long Id { get; set; }
    }
}
