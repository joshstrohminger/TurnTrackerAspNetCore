using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurnTrackerAspNetCore.Entities;
using TurnTrackerAspNetCore.Services;
using TurnTrackerAspNetCore.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

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
            return View(new HomePageViewModel { Tasks = _taskData.GetAllTasks().ToList() });
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
                .Select(group => new UserCountViewModel(
                    group.Key,
                    group.Count(),
                    0, // TODO add turns offset here
                    false))
                .ToDictionary(x => x.User, x => x);

            foreach (var user in task.Participants.Select(x => x.User))
            {
                UserCountViewModel count;
                if (counts.TryGetValue(user, out count))
                {
                    count.Active = true;
                }
                else
                {
                    counts.Add(user, new UserCountViewModel(user, 0, 0, true));
                }
            }

            var orderedCounts = counts.Values
                .OrderBy(x => x.TotalTurns)
                .ThenBy(x => x.User.UserName)
                .ToList();


            var model = new TaskDetailsViewModel
            {
                Task = task,
                Counts = orderedCounts,
                MaxTurns = orderedCounts.Count > 0 ? orderedCounts.Max(x => x.TotalTurns) : 0
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
            var task = _taskData.GetTaskDetails(id);
            if (null == task)
            {
                return RedirectToAction(nameof(Index));
            }
            var participantIds = task.Participants?.Select(x => x.UserId).ToList() ?? new List<string>();
            var model = new EditTaskViewModel
            {
                Id = task.Id,
                Name = task.Name,
                Period = task.Period,
                Unit = task.Unit,
                TeamBased = task.TeamBased,
                Users = _taskData.GetAllUsers().Select(x => new SelectListItem
                {
                    Value = x.Id,
                    Text = x.DisplayName == null ? $"{x.UserName}" : $"{x.DisplayName} ({x.UserName})",
                    Selected = participantIds.Contains(x.Id)
                }).ToList()
            };
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EditTask(long id, EditTaskViewModel model)
        {
            var task = _taskData.GetTaskDetails(id);
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

            task.Participants.RemoveAll(x => !model.Participants.Contains(x.UserId));
            foreach (var newId in model.Participants.Except(task.Participants.Select(x => x.UserId)))
            {
                task.Participants.Add(new Participant { TaskId = id, UserId = newId });
            }
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
