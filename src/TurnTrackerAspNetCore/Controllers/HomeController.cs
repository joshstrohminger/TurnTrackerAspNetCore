using Microsoft.AspNetCore.Mvc;
using TurnTrackerAspNetCore.Entities;
using TurnTrackerAspNetCore.Services;
using TurnTrackerAspNetCore.ViewModels;
using System.Linq;

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
            return View(new HomePageViewModel { Tasks = _taskData.GetAll() });
        }

        public IActionResult Details(long id)
        {
            var task = _taskData.Get(id);
            if (null == task)
            {
                return RedirectToAction(nameof(Index));
            }
            return View(task);
        }
    }
}
