using System.ComponentModel.DataAnnotations;

namespace TurnTrackerAspNetCore.ViewModels.Account
{
    public class ForgotPasswordViewModel
    {
        //[Required]
        //[EmailAddress]
        //public string Email { get; set; }

        [Required]
        public string UserName { get; set; }
    }
}
