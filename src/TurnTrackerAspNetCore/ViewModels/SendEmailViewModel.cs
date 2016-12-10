using System.ComponentModel.DataAnnotations;

namespace TurnTrackerAspNetCore.ViewModels
{
    public class SendEmailViewModel
    {
        [Required, DataType(DataType.EmailAddress)]
        public string Email { get; set; }
    }
}
