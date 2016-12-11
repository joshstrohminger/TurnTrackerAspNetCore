using System.ComponentModel.DataAnnotations;
using TurnTrackerAspNetCore.Entities;
using TurnTrackerAspNetCore.Services;

namespace TurnTrackerAspNetCore.ViewModels.Admin
{
    public class SiteSettingsViewModel : SiteSettingGroup
    {
        public GeneralSiteSettingGroup General { get; } = new GeneralSiteSettingGroup();
    }

    public class GeneralSiteSettingGroup : SiteSettingGroup
    {
        [Required, MaxLength(SiteSetting.MaxValueLength)]
        public string Name { get; set; } = "Turn Tracker";

        [Required, Display(Name = "Registration Mode")]
        public RegistrationMode RegistrationMode { get; set; } = RegistrationMode.Open;

        [Required, Display(Name = "New Thing")]
        public bool NewThing { get; set; } = true;
    }

    public enum RegistrationMode
    {
        Open,
        [Display(Name = "Invite Only")]
        InviteOnly,
        Closed
    }
}
