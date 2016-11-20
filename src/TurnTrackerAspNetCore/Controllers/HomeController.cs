using Microsoft.AspNetCore.Mvc;
using TurnTrackerAspNetCore.Entities;
using TurnTrackerAspNetCore.Services;
using TurnTrackerAspNetCore.ViewModels;

namespace TurnTrackerAspNetCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ITaskData _taskData;

        public HomeController(ITaskData taskData)
        {
            _taskData = taskData;
        }

        public IActionResult Index()
        {
            var model = new HomePageViewModel {Tasks = _taskData.GetAll()};
            return View(model);
        }
    }
}
