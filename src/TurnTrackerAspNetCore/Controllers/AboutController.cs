using Microsoft.AspNetCore.Mvc;

namespace TurnTrackerAspNetCore.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        // stash this here for now, maybe move it to another controller at some point
        public IActionResult Error(string code)
        {
            return View(nameof(Error), code);
        }
    }
}
