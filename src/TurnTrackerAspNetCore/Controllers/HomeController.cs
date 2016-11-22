using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurnTrackerAspNetCore.Entities;
using TurnTrackerAspNetCore.Services;
using TurnTrackerAspNetCore.ViewModels;

namespace TurnTrackerAspNetCore.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ITaskData _taskData;

        public HomeController(ITaskData taskData)
        {
            _taskData = taskData;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View(new HomePageViewModel { Tasks = _taskData.GetAll().ToList() });
        }

        [AllowAnonymous]
        public IActionResult Details(long id)
        {
            var task = _taskData.GetDetails(id);
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

        [HttpPost, ValidateAntiForgeryToken]
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

        [HttpGet]
        public IActionResult Edit(long id)
        {
            var model = _taskData.Get(id);
            if (null == model)
            {
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Edit(long id, TrackedTaskEditViewModel model)
        {
            var task = _taskData.Get(id);
            if (null == task)
            {
                return RedirectToAction(nameof(Index));
            }
            if (!ModelState.IsValid)
            {
                return View(task);
            }
            task.Period = model.Period;
            task.TeamBased = model.TeamBased;
            task.Unit = model.Unit;
            task.Name = model.Name;
            _taskData.Commit();
            return RedirectToAction(nameof(Details), new {id = task.Id});
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult TakeTurn(long id)
        {
            var success = _taskData.TakeTurn(id);
            _taskData.Commit();
            return success ? RedirectToAction(nameof(Details), new {id}) : RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult DeleteTask(long id)
        {
            var success = _taskData.DeleteTask(id);
            _taskData.Commit();
            return success ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Details), new {id});
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult DeleteTurn(long id)
        {
            var taskId = _taskData.DeleteTurn(id);
            _taskData.Commit();
            return RedirectToAction(nameof(Details), new {id = taskId});
        }
    }
}
