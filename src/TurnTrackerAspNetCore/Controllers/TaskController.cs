using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IAuthorizationService _authorizationService;

        public TaskController(UserManager<User> userManager, ITaskData taskData, IAuthorizationService authorizationService)
        {
            _userManager = userManager;
            _taskData = taskData;
            _authorizationService = authorizationService;
        }
        
        public IActionResult Index(string error = null)
        {
            var userId = _userManager.GetUserId(HttpContext.User);
            var counts = _taskData.GetTurnCounts(userId);
            var tasks = _taskData.GetParticipations(userId).Union(_taskData.GetAllTasks().Where(x => x.UserId == userId)).ToList();
            var latest = _taskData.GetLatestTurns(tasks.Select(x => x.Id).ToArray()).ToList();
            var taskCounts = new Dictionary<TrackedTask, TurnCount>();
            foreach (var task in tasks)
            {
                task.PopulateLatestTurnInfo(counts, taskCounts, latest);
            }

            return View("Tasks", new TasksViewModel { TaskCounts = taskCounts, Error = error });
        }
        
        public async Task<IActionResult> Details(long id, string error = null)
        {
            var task = _taskData.GetTaskDetails(id);
            if (null == task)
            {
                return new NotFoundResult();
            }
            if (!await _authorizationService.AuthorizeAsync(User, task, nameof(Policies.CanAccessTask)))
            {
                return new ChallengeResult();
            }

            var userId = _userManager.GetUserId(HttpContext.User);

            var counts = _taskData.GetTurnCounts(id)
                .Select(x => new UserCountViewModel(x))
                .OrderBy(x => x.TotalTurns)
                .ThenBy(x => x.Name)
                .ToList();
            var maxTurns = counts.Count == 0 ? 0 : counts.Max(x => x.TotalTurns);

            var model = new TaskDetailsViewModel
            {
                Task = task,
                Counts = counts,
                MaxTurns = maxTurns,
                CanTakeTurn = task.Participants.Any(x => x.UserId == userId),
                CanDeleteTask = task.UserId == userId,
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
        public async Task<IActionResult> Edit(long id)
        {
            var task = _taskData.GetTaskDetails(id);

            if (null == task)
            {
                return new NotFoundResult();
            }
            if (!await _authorizationService.AuthorizeAsync(User, task, nameof(Policies.CanAccessTask)))
            {
                return new ChallengeResult();
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
        public async Task<IActionResult> Edit(long id, EditTaskViewModel model)
        {
            var task = _taskData.GetTaskDetails(id);

            if (null == task)
            {
                return new NotFoundResult();
            }
            if (!await _authorizationService.AuthorizeAsync(User, task, nameof(Policies.CanAccessTask)))
            {
                return new ChallengeResult();
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
        public async Task<IActionResult> Delete(long id)
        {
            var task = _taskData.GetTaskDetails(id);

            if (null == task)
            {
                return new NotFoundResult();
            }
            if (!await _authorizationService.AuthorizeAsync(User, task, nameof(Policies.CanDeleteTask)))
            {
                return new ChallengeResult();
            }

            _taskData.DeleteTask(task);
            _taskData.Commit();
            return RedirectToAction(nameof(Index));
        }
    }
}
