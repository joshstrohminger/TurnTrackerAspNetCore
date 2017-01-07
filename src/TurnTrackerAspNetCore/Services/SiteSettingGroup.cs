using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.Services
{
    public abstract class SiteSettingGroup
    {
        private readonly PropertyInfo[] _properties;

        protected SiteSettingGroup()
        {
            _properties = GetType().GetProperties();
        }

        public bool Load(ILogger logger, Dictionary<string, SiteSetting> settings, string parentPath = "")
        {
            return _properties
                .Where(property => null != property.GetCustomAttribute<RequiredAttribute>() )
                .Aggregate(true, (success, property) => success && LoadProperty(logger, settings, property, parentPath));
        }

        private bool LoadProperty(ILogger logger, Dictionary<string, SiteSetting> settings, PropertyInfo property, string parentPath = "")
        {
            var name = $"{parentPath}{property.Name}";
            if (typeof(SiteSettingGroup).IsAssignableFrom(property.PropertyType))
            {
                return ((SiteSettingGroup) property.GetValue(this)).Load(logger, settings, $"{name}.");
            }

            SiteSetting setting;
            if (!settings.TryGetValue(name, out setting))
            {
                logger.LogWarning(EventIds.SiteSettingLoadError, $"Couldn't find setting for '{name}'");
                return true;
            }
            setting.Accessed = true;

            var type = property.PropertyType;
            if (type.Name != setting.Type)
            {
                logger.LogError(EventIds.SiteSettingLoadError, $"Type '{type.Name}' of property '{name}' doesn't match DB type '{setting.Type}'");
                return false;
            }

            try
            {
                if (type == typeof(string))
                {
                    property.SetValue(this, setting.Value);
                }
                else if (type == typeof(int) || type.GetTypeInfo().IsEnum)
                {
                    property.SetValue(this, int.Parse(setting.Value));
                }
                else if (type == typeof(long))
                {
                    property.SetValue(this, long.Parse(setting.Value));
                }
                else if (type == typeof(bool))
                {
                    property.SetValue(this, bool.Parse(setting.Value));
                }
                else
                {
                    logger.LogError(EventIds.SiteSettingLoadError,
                        $"Unexpected type '{type.Name}' for '{name}'");
                    return false;
                }
            }
            catch (Exception e)
            {
                logger.LogError(EventIds.SiteSettingLoadError, e,
                    $"Exception while setting value '{setting.Value}' for Type '{type.Name}' for '{name}'");
                return false;
            }

            return true;
        }

        public bool Save(ILogger logger, Dictionary<string, SiteSetting> settings, string parentPath = "")
        {
            return _properties
                .Where(property => null != property.GetCustomAttribute<RequiredAttribute>())
                .Aggregate(true, (success, property) => success && SaveProperty(logger, settings, property, parentPath));
        }

        private bool SaveProperty(ILogger logger, Dictionary<string, SiteSetting> settings, PropertyInfo property, string parentPath = "")
        {
            var name = $"{parentPath}{property.Name}";
            if (typeof(SiteSettingGroup).IsAssignableFrom(property.PropertyType))
            {
                return ((SiteSettingGroup)property.GetValue(this)).Save(logger, settings, $"{name}.");
            }

            SiteSetting setting;
            if (!settings.TryGetValue(name, out setting))
            {
                setting = new SiteSetting {Name = name, Type = property.PropertyType.Name, NeedToAdd = true};
                settings.Add(name, setting);
            }

            try
            {
                if (property.PropertyType.GetTypeInfo().IsEnum)
                {
                    setting.Value = ((int)property.GetValue(this)).ToString();
                }
                else
                {
                    setting.Value = property.GetValue(this)?.ToString() ?? "";
                }
            }
            catch (Exception e)
            {
                logger.LogError(EventIds.SiteSettingSaveError, e, $"Exception while getting value '{setting.Value}' for Type '{setting.Type}' for '{name}'");
                return false;
            }

            return true;
        }
    }
}