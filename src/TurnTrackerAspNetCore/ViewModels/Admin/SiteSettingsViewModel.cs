using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Hangfire;
using Hangfire.Storage;
using TurnTrackerAspNetCore.Entities;
using TurnTrackerAspNetCore.Services.Jobs;
using TurnTrackerAspNetCore.Services.Settings;

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

        [Required, Display(Name = "Invite Expiration Hours"), Range(1, 7 * 24)]
        public int InviteExpirationHours { get; set; } = 3 * 24;
    }

    public class JobsSettingGroup : SiteSettingGroup
    {
        [Required]
        public JobSetting Notifications { get; } = new JobSetting(nameof(Notifications), true, Cron.Daily(18), Notifier.Start);
    }

    public class JobSetting : SiteSettingGroup
    {
        private readonly Action<IServiceProvider, JobSetting> _starter;

        public string Id { get; }
        public bool Initialized { get; private set; }

        public JobSetting()
        {
        }

        public JobSetting(string id, bool enabled, string cron, Action<IServiceProvider,JobSetting> starter)
        {
            Id = id;
            Enabled = enabled;
            CronSchedule = cron;
            _starter = starter;
        }

        [Required, Display(Name = "Enabled")]
        public bool Enabled { get; set; }

        // This is whether or not the job actually exists or has been deleted, so it might be different than the setting
        [Display(Name = "Currently Enabled")]
        public bool CurrentlyEnabled { get; set; }

        [Required, Display(Name = "Cron Schedule")]
        public string CronSchedule { get; set; }

        public override void Saved(IServiceProvider services)
        {
            if (Enabled)
            {
                _starter?.Invoke(services, this);
            }
            else
            {
                RecurringJob.RemoveIfExists(Id);
            }
            PopulateCurrentlyEnabled();
        }

        private void PopulateCurrentlyEnabled()
        {
            if (Initialized)
            {
                try
                {
                    using (var connection = JobStorage.Current.GetConnection())
                    {
                        CurrentlyEnabled = connection.GetRecurringJobs().Any(x => x.Id == Id);
                    }
                }
                catch (InvalidOperationException)
                {
                    // hangfire not loaded yet
                }
            }
        }

        public override void Loaded()
        {
            PopulateCurrentlyEnabled();
            Initialized = true; // TODO, this is a hack to fix a race condition (hangfire hasn't set JobStorage.Current yet). This prevents us from accessing JobStorage.Current until our second load.
        }
    }

    public enum RegistrationMode
    {
        Open,
        [Display(Name = "Invite Only")]
        InviteOnly,
        Closed
    }
}
