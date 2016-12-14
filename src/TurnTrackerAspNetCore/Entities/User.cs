using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace TurnTrackerAspNetCore.Entities
{
    public class User : IdentityUser
    {
        [Display(Name = "Display Name"), MaxLength(100)]
        public string DisplayName { get; set; }

        public List<Participant> Participations { get; set; }

    }
}
