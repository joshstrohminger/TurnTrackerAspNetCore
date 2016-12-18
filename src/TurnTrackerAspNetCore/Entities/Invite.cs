using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TurnTrackerAspNetCore.Entities
{
    public class Invite
    {
        [Key]
        public Guid Token { get; set; }

        [Required, MaxLength(256)]
        public string Email { get; set; }

        [Required]
        public string InviterId { get; set; }
        public User Inviter { get; set; }
        
        public string InviteeId { get; set; }
        public User Invitee { get; set; }

        public DateTimeOffset? Used { get; set; }

        public DateTimeOffset? Sent { get; set; }

        [Required]
        public DateTimeOffset Expires { get; set; }

        public DateTimeOffset Created { get; set; }
    }
}
