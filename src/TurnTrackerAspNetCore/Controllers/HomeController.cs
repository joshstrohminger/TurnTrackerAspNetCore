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

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(TrackedTaskEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var task = new TrackedTask
            {
                Name = model.Name,
                Period = model.Period,
                Unit = model.Unit,
                TeamBased = model.TeamBased
            };
            var newTask = _taskData.Add(task);
            _taskData.Commit();
            return RedirectToAction(nameof(Details), new {id = newTask.Id});
        }
    }
}
