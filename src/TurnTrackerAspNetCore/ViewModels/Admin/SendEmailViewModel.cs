using System.ComponentModel.DataAnnotations;

namespace TurnTrackerAspNetCore.ViewModels.Admin
{
    public class SendEmailViewModel
    {
        [Required, DataType(DataType.EmailAddress)]
        public string Email { get; set; }
    }
}
