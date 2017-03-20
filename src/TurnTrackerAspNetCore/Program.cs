using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace TurnTrackerAspNetCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .CaptureStartupErrors(true)
                .UseSetting("detailedErrors", "true")
                .UseKestrel()
                //.UseAzureAppServices()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
