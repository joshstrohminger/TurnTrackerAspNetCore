using Microsoft.AspNetCore.Mvc;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var model = new Task {Id = 1, Name = "Empty Litter Boxes"};
            return View(model);
        }
    }
}
