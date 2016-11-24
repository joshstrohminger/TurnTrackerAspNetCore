using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurnTrackerAspNetCore.Entities;
using TurnTrackerAspNetCore.Services;
using TurnTrackerAspNetCore.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace TurnTrackerAspNetCore.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ITaskData _taskData;
        private readonly UserManager<User> _userManager;

        public HomeController(ITaskData taskData, UserManager<User> userManager)
        {
            _taskData = taskData;
            _userManager = userManager;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View(new HomePageViewModel { Tasks = _taskData.GetAll().ToList() });
        }

        [AllowAnonymous]
        public IActionResult Details(long id)
        {
            var task = _taskData.GetTaskDetails(id);
            if (null == task)
            {
                return RedirectToAction(nameof(Index));
            }

            var counts = task.Turns
                .GroupBy(turn => turn.User)
                .Select(group => new UserCountViewModel(group.Key, group.Count(), 0)) // TODO add turns offset here
                .OrderBy(x => x.TotalTurns)
                .ThenBy(x => x.User.UserName)
                .ToList();

            var model = new TaskDetailsViewModel
            {
                Task = task,
                ActiveUsers = counts,
                MaxTurns = counts.Max(x => x.TotalTurns)
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(EditTaskViewModel model)
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
        public IActionResult EditTask(long id)
        {
            var model = _taskData.GetTask(id);
            if (null == model)
            {
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EditTask(long id, EditTaskViewModel model)
        {
            var task = _taskData.GetTask(id);
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
            task.Modified = DateTimeOffset.UtcNow;
            _taskData.Commit();
            return RedirectToAction(nameof(Details), new {id = task.Id});
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult TakeTurn(long id)
        {
            var success = _taskData.TakeTurn(id, _userManager.GetUserId(HttpContext.User));
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

        [HttpGet]
        public IActionResult EditTurn(long id)
        {
            var turn = _taskData.GetTurn(id);
            return View(turn);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EditTurn(long id, long taskId, EditTurnViewModel model)
        {
            var turn = _taskData.GetTurn(id);
            if (null == turn)
            {
                return RedirectToAction(nameof(Details), new {id = taskId});
            }
            if (!ModelState.IsValid)
            {
                return View(turn);
            }
            turn.Taken = model.Taken;
            turn.Modified = DateTimeOffset.UtcNow;
            _taskData.Commit();
            return RedirectToAction(nameof(Details), new { id = taskId });
        }
    }
}
