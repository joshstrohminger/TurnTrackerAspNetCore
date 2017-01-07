using System.ComponentModel.DataAnnotations;
using Hangfire;
using TurnTrackerAspNetCore.Entities;
using TurnTrackerAspNetCore.Services;

namespace TurnTrackerAspNetCore.ViewModels.Admin
{
    public class SiteSettingsViewModel : SiteSettingGroup
    {
        [Required, Display(Name = "General Settings")]
        public GeneralSiteSettingGroup General { get; } = new GeneralSiteSettingGroup();

        [Required, Display(Name = "Job Control")]
        public JobsSettingGroup Jobs { get; } = new JobsSettingGroup();
    }

    public class GeneralSiteSettingGroup : SiteSettingGroup
    {
        [Required, MaxLength(SiteSetting.MaxValueLength)]
        public string Name { get; set; } = "Turn Tracker";

        [Required, Display(Name = "Registration Mode")]
        public RegistrationMode RegistrationMode { get; set; } = RegistrationMode.Open;

        [Required, Display(Name = "API Enabled")]
        public bool ApiEnabled { get; set; } = true;

        [Required, Display(Name = "Invite Expiration Hours"), Range(1, 168)]
        public int InviteExpirationHours { get; set; } = 72;
    }

    public class JobsSettingGroup : SiteSettingGroup
    {
        [Required]
        public JobSetting Notifications { get; } = new JobSetting(nameof(Notifications), true, Cron.Daily(18));
    }

    public class JobSetting : SiteSettingGroup
    {

        public string Id { get; }

        public JobSetting()
        {
        }

        public JobSetting(string id, bool enabled, string cron)
        {
            Id = id;
            Enabled = enabled;
            CronSchedule = cron;
        }

        [Required, Display(Name = "Enabled")]
        public bool Enabled { get; set; }

        // This is whether or not the job actually exists or has been deleted, so it might be different than the setting
        [Display(Name = "Currently Enabled")]
        public bool CurrentlyEnabled { get; set; }

        [Required, Display(Name = "Cron Schedule")]
        public string CronSchedule { get; set; }
    }

    public enum RegistrationMode
    {
        Open,
        [Display(Name = "Invite Only")]
        InviteOnly,
        Closed
    }
}
