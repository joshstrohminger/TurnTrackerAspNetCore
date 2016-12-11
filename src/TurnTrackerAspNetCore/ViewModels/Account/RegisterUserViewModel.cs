using System.ComponentModel.DataAnnotations;

namespace TurnTrackerAspNetCore.ViewModels.Account
{
    public class RegisterUserViewModel
    {
        [Required, MaxLength(256)]
        public string UserName { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Display Name"), MaxLength(100)]
        public string DisplayName { get; set; }

        //[Display(Name = "Phone"), MaxLength(100), DataType(DataType.PhoneNumber)]
        //public string PhoneNumber { get; set; }

        [Required, MaxLength(256), DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Display(Name = "Confirm Password"), DataType(DataType.Password), Compare(nameof(Password))]
        public string ConfirmPassword { get; set; }

        public string InviteToken { get; set; }
    }
}
