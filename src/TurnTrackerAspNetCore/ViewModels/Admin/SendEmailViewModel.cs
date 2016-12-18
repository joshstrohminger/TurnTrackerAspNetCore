using System.ComponentModel.DataAnnotations;

namespace TurnTrackerAspNetCore.ViewModels.Admin
{
    public class SendEmailViewModel
    {
        [Required, DataType(DataType.EmailAddress), Display(Prompt = "jane.doe@email.com")]
        public string Email { get; set; }
    }
}
