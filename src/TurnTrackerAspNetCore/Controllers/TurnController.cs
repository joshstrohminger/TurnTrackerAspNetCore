﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TurnTrackerAspNetCore.Entities;
using TurnTrackerAspNetCore.Services;
using TurnTrackerAspNetCore.Services.Data;
using TurnTrackerAspNetCore.ViewModels.Turn;

namespace TurnTrackerAspNetCore.Controllers
{
    [Authorize]
    public class TurnController : Controller
    {
        private readonly ITaskData _taskData;
        private readonly UserManager<User> _userManager;
        private readonly IAuthorizationService _authorizationService;

        public TurnController(ITaskData taskData, UserManager<User> userManager, IAuthorizationService authorizationService)
        {
            _taskData = taskData;
            _userManager = userManager;
            _authorizationService = authorizationService;
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Take(long id, DateTimeOffset? time = null)
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
            // TODO use timezone, this probably only works because the server is currently in the same time zone when developing
            var userId = _userManager.GetUserId(HttpContext.User);
            var turn = new Turn {UserId = userId};
            if (time.HasValue && time.Value < DateTimeOffset.UtcNow.AddMinutes(30))
            {
                // don't allow a time very far into the future
                turn.Taken = time.Value;
            }
            task.Turns.Add(turn);
            _taskData.Commit();
            return RedirectToAction(nameof(TaskController.Details), "Task", new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var turn = _taskData.GetTurn(id);
            if (null == turn)
            {
                return new NotFoundResult();
            }

            var task = _taskData.GetTaskDetails(turn.TaskId);
            if (null == task)
            {
                return new NotFoundResult();
            }
            if (!await _authorizationService.AuthorizeAsync(User, task, nameof(Policies.CanAccessTask)))
            {
                return new ChallengeResult();
            }

            task.Turns.Remove(turn);
            _taskData.Commit();
            return RedirectToAction(nameof(TaskController.Details), "Task", new { id = task.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var turn = _taskData.GetTurn(id);
            if (null == turn)
            {
                return new NotFoundResult();
            }

            var task = _taskData.GetTaskDetails(turn.TaskId);
            if (null == task)
            {
                return new NotFoundResult();
            }
            if (!await _authorizationService.AuthorizeAsync(User, task, nameof(Policies.CanAccessTask)))
            {
                return new ChallengeResult();
            }

            return View(turn);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, EditTurnViewModel model)
        {
            var turn = _taskData.GetTurn(id);
            if (null == turn)
            {
                return new NotFoundResult();
            }

            var task = _taskData.GetTaskDetails(turn.TaskId);
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
                return View(turn);
            }

            turn.Taken = model.Taken;
            turn.Modified = DateTimeOffset.UtcNow;
            _taskData.Commit();
            return RedirectToAction(nameof(TaskController.Details), "Task", new { id = task.Id });
        }
    }
}
