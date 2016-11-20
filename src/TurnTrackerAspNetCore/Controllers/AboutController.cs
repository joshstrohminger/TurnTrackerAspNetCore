using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TurnTrackerAspNetCore.Services;

namespace TurnTrackerAspNetCore.Controllers
{
    public class AboutController : Controller
    {
        private readonly IConfiguration _configuration;

        public AboutController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return Content($"A test site for {_configuration.GetSiteName()}");
        }
    }
}
