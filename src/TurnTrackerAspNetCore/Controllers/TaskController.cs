﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TurnTrackerAspNetCore.Entities;
using TurnTrackerAspNetCore.Services;
using TurnTrackerAspNetCore.Services.Data;
using TurnTrackerAspNetCore.ViewModels.Task;

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

            var counts = _taskData.GetTurnCounts(id).ToList();
            task.PopulateLatestTurnInfo(
                new Dictionary<long, List<TurnCount>> { { task.Id, counts } },
                new Dictionary<TrackedTask, TurnCount>(),
                task.Turns);
            var maxTurns = counts.Max(x => x.TotalTurns, 0);

            var model = new TaskDetailsViewModel
            {
                Task = task,
                Counts = counts.Select(x => new UserCountViewModel(x)).ToList(),
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
            var owner = _userManager.GetUserId(HttpContext.User);
            return View(new EditTaskViewModel
            {
                Owner = owner,
                Users = _taskData.GetAllUsers().Select(x => new SelectListItem
                {
                    Value = x.Id,
                    Text = x.DisplayName == null ? $"{x.UserName}" : $"{x.DisplayName} ({x.UserName})",
                    Selected = owner == x.Id
                }).ToList()
            });
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
            // ensure the creator is a participant
            newTask.Participants =
                new[] {userId}.Union(model.Participants ?? new List<string>())
                    .Distinct()
                    .Select(x => new Participant {UserId = x, TaskId = newTask.Id})
                    .ToList();
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

            // update participants and give you users the max count as an offset
            var counts = _taskData.GetTurnCounts(task.Id).ToList();
            var max = counts.Max(x => x.TotalTurns, 0);
            task.Participants.RemoveAll(x => !model.Participants.Contains(x.UserId));
            foreach (var newId in model.Participants.Except(task.Participants.Select(x => x.UserId)))
            {
                var offset = max - task.Turns.Count(x => x.UserId == newId);
                task.Participants.Add(new Participant { TaskId = id, UserId = newId, Offset = offset });
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
