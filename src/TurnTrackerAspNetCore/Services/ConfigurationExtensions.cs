using Microsoft.Extensions.Configuration;

namespace TurnTrackerAspNetCore.Services
{
    public static class ConfigurationExtensions
    {
        public static string GetSiteName(this IConfiguration config)
        {
            return config["SiteName"];
        }
    }
}
