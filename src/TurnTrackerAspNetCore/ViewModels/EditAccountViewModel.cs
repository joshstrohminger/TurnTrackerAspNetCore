using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TurnTrackerAspNetCore.ViewModels
{
    public class EditAccountViewModel
    {
        public string UserName { get; set; }

        [Display(Name = "Display Name"), MaxLength(100)]
        public string DisplayName { get; set; }

        [Display(Name = "Phone"), MaxLength(100), DataType(DataType.PhoneNumber)]
        public string PhoneNumber { get; set; }

        [MaxLength(256), DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        public IEnumerable<string> Roles { get; set; }

        public List<SelectListItem> AllRoles { get; set; }
    }
}
