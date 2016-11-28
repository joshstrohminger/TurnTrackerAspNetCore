using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TurnTrackerAspNetCore.Entities;
using TurnTrackerAspNetCore.Services;
using TurnTrackerAspNetCore.ViewModels;

namespace TurnTrackerAspNetCore.Controllers
{
    [Authorize]
    public class TaskController : Controller
    {
        private readonly ITaskData _taskData;
        private readonly UserManager<User> _userManager;

        public TaskController(UserManager<User> userManager, ITaskData taskData)
        {
            _userManager = userManager;
            _taskData = taskData;
        }
        
        [Route("")]
        public IActionResult Index(string error = null)
        {
            Dictionary<TrackedTask, TurnCount> taskCounts;
            if (!User.Identity.IsAuthenticated)
            {
                var tasks = _taskData.GetAllTasks().ToList();
                taskCounts = tasks.ToDictionary(x => x, x => (TurnCount)null);
            }
            else
            {
                var userId = _userManager.GetUserId(HttpContext.User);
                var counts = _taskData.GetTurnCounts(userId)
                    .GroupBy(x => x.TaskId)
                    .ToDictionary(x => x.Key, x => x.ToList());
                var tasks = _taskData.GetParticipations(userId).Union(_taskData.GetAllTasks().Where(x => x.UserId == userId)).ToList();
                var latest = _taskData.GetLatestTurns(tasks.Select(x => x.Id).ToArray()).ToList();
                taskCounts = new Dictionary<TrackedTask, TurnCount>();
                foreach (var task in tasks)
                {
                    List<TurnCount> count;
                    counts.TryGetValue(task.Id, out count);
                    task.LastTaken = latest.FirstOrDefault(x => x.TaskId == task.Id)?.Taken;
                    taskCounts.Add(task, count?.FirstOrDefault());
                }
            }
            return View(new HomePageViewModel { TaskCounts = taskCounts, Error = error });
        }

        [AllowAnonymous]
        public IActionResult Details(long id, string error = null)
        {
            var task = _taskData.GetTaskDetails(id);
            var userId = _userManager.GetUserId(HttpContext.User);
            if (null == task || (User.Identity.IsAuthenticated && task.UserId != userId && task.Participants.All(x => x.UserId != userId)))
            {
                return RedirectToAction(nameof(Index), new { error = "Invalid task" });
            }

            var counts = task.Turns
                .GroupBy(turn => turn.User)
                .Select(group => new UserCountViewModel(
                    group.Key,
                    group.Count(),
                    0,
                    false))
                .ToDictionary(x => x.User, x => x);

            var newParticipants = new List<User>();
            var maxTurns = 0;

            foreach (var part in task.Participants)
            {
                UserCountViewModel count;
                if (counts.TryGetValue(part.User, out count))
                {
                    count.Activate(part.Offset);
                    maxTurns = Math.Max(count.TotalTurns, maxTurns);
                }
                else
                {
                    newParticipants.Add(part.User);
                }
            }

            var orderedCounts = counts.Values
                .Union(newParticipants.Select(x => new UserCountViewModel(x, 0, 0, true)))
                .OrderBy(x => x.TotalTurns)
                .ThenBy(x => x.User.UserName)
                .ToList();

            var model = new TaskDetailsViewModel
            {
                Task = task,
                Counts = orderedCounts,
                MaxTurns = maxTurns,
                CanTakeTurn = task.Participants.Any(x => x.UserId == userId),
                Error = error
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new EditTaskViewModel { Owner = _userManager.GetUserId(HttpContext.User) });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(EditTaskViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var userId = _userManager.GetUserId(HttpContext.User);
            var task = new TrackedTask
            {
                Name = model.Name,
                Period = model.Period,
                Unit = model.Unit,
                TeamBased = model.TeamBased,
                UserId = userId
            };
            var newTask = _taskData.Add(task);
            newTask.Participants = new List<Participant> { new Participant { UserId = userId, TaskId = newTask.Id } };
            _taskData.Commit();
            return RedirectToAction(nameof(Details), new { id = newTask.Id });
        }


        [HttpGet]
        public IActionResult Edit(long id)
        {
            var task = _taskData.GetTaskDetails(id);
            var userId = _userManager.GetUserId(HttpContext.User);
            if (null == task || (task.UserId != userId && task.Participants.All(x => x.UserId != userId)))
            {
                return RedirectToAction(nameof(Index), new { error = "Invalid task" });
            }
            var participantIds = task.Participants?.Select(x => x.UserId).ToList() ?? new List<string>();
            var model = new EditTaskViewModel
            {
                Id = task.Id,
                Name = task.Name,
                Period = task.Period,
                Unit = task.Unit,
                TeamBased = task.TeamBased,
                Owner = task.UserId,
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
        public IActionResult Edit(long id, EditTaskViewModel model)
        {
            var task = _taskData.GetTaskDetails(id);
            var userId = _userManager.GetUserId(HttpContext.User);
            if (null == task || (task.UserId != userId && task.Participants.All(x => x.UserId != userId)))
            {
                return RedirectToAction(nameof(Index), new { error = "Invalid task" });
            }
            if (!ModelState.IsValid)
            {
                return View(task);
            }
            task.Period = model.Period;
            task.TeamBased = model.TeamBased;
            task.Unit = model.Unit;
            task.UserId = model.Owner;
            task.Name = model.Name;
            model.Participants = model.Participants ?? new List<string>();

            task.Participants.RemoveAll(x => !model.Participants.Contains(x.UserId));
            foreach (var newId in model.Participants.Except(task.Participants.Select(x => x.UserId)))
            {
                task.Participants.Add(new Participant { TaskId = id, UserId = newId, Offset = 0 });
            }
            task.Modified = DateTimeOffset.UtcNow;
            _taskData.Commit();
            return RedirectToAction(nameof(Details), new { id = task.Id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Delete(long id)
        {
            var task = _taskData.GetTaskDetails(id);
            var userId = _userManager.GetUserId(HttpContext.User);
            if (null == task || (task.UserId != userId && task.Participants.All(x => x.UserId != userId)))
            {
                return RedirectToAction(nameof(Index), new { error = "Invalid task" });
            }
            _taskData.DeleteTask(task);
            _taskData.Commit();
            return RedirectToAction(nameof(Index));
        }
    }
}
