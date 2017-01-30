using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using TurnTrackerAspNetCore.Services.Data;
using TurnTrackerAspNetCore.ViewModels.Admin;

namespace TurnTrackerAspNetCore.Services.Settings
{
    public interface ISiteSettings
    {
        bool Load();
        bool Save(IServiceProvider services);
        SiteSettingsViewModel Settings { get; set; }
    }

    public class SiteSettings : ISiteSettings
    {
        private readonly ITaskData _db;
        private readonly ILogger _logger;
        public const string Protocol = "https";

        public SiteSettings(ITaskData db, ILoggerFactory loggerFactory)
        {
            _db = db;
            _logger = loggerFactory.CreateLogger<SiteSettings>();
            Load();
        }

        public bool Load()
        {
            var dbSettings = _db.GetSiteSettings().ToDictionary(x => x.Name, x => x);
            if (null == Settings)
            {
                _logger.LogError(EventIds.SiteSettingLoadError, "settings are null");
                return false;
            }
            if (Settings.Load(_logger, dbSettings))
            {
                foreach (var s in dbSettings.Values.Where(x => !x.Accessed))
                {
                    _db.Remove(s);
                    _logger.LogInformation(EventIds.SiteSettingRemoved, s.Name);
                }
                _db.Commit();
                return true;
            }
            return false;
        }

        public bool Save(IServiceProvider services)
        {
            var dbSettings = _db.GetSiteSettings().ToDictionary(x => x.Name, x => x);
            if (null == Settings)
            {
                _logger.LogError(EventIds.SiteSettingSaveError, "settings are null");
                return false;
            }
            if (Settings.Save(_logger, dbSettings, services))
            {
                foreach (var s in dbSettings.Values.Where(x => x.NeedToAdd))
                {
                    s.NeedToAdd = false;
                    _db.Add(s);
                    _logger.LogInformation(EventIds.SiteSettingAdded, s.Name);
                }
                _db.Commit();
                return true;
            }
            return false;
        }

        public SiteSettingsViewModel Settings { get; set; } = new SiteSettingsViewModel();

    }
}
