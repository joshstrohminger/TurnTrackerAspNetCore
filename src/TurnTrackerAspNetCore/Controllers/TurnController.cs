using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TurnTrackerAspNetCore.Entities;
using TurnTrackerAspNetCore.Services;
using TurnTrackerAspNetCore.ViewModels;

namespace TurnTrackerAspNetCore.Controllers
{
    [Authorize]
    public class TurnController : Controller
    {
        private readonly ITaskData _taskData;
        private readonly UserManager<User> _userManager;

        public TurnController(ITaskData taskData, UserManager<User> userManager)
        {
            _taskData = taskData;
            _userManager = userManager;
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Take(long id)
        {
            var task = _taskData.GetTaskDetails(id);
            var userId = _userManager.GetUserId(HttpContext.User);

            if (null == task)
            {
                return RedirectToAction(nameof(TaskController.Index), "Task", new { error = "Invalid task" });
            }

            if (task.Participants.All(x => x.UserId != userId))
            {
                return RedirectToAction(nameof(TaskController.Details), "Task", new { id, error = "Must be an active user to take a turn" });
            }

            task.Turns.Add(new Turn { UserId = userId });
            _taskData.Commit();
            return RedirectToAction(nameof(TaskController.Details), "Task", new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Delete(long id)
        {
            var turn = _taskData.GetTurn(id);
            if (null == turn)
            {
                return RedirectToAction(nameof(TaskController.Details), "Task", new { id, error = "Invalid turn" });
            }

            var task = _taskData.GetTaskDetails(turn.TaskId);
            var userId = _userManager.GetUserId(HttpContext.User);
            if (task.AccessDenied(userId))
            {
                return RedirectToAction(nameof(TaskController.Details), "Task", new { id, error = "Invalid task" });
            }
            _taskData.Commit();
            return RedirectToAction(nameof(TaskController.Details), "Task", new { id = task.Id });
        }

        [HttpGet]
        public IActionResult Edit(long id)
        {
            var turn = _taskData.GetTurn(id);
            if (null == turn)
            {
                return RedirectToAction(nameof(TaskController.Index), "Task", new { error = "Invalid turn" });
            }
            var task = _taskData.GetTaskDetails(turn.TaskId);
            var userId = _userManager.GetUserId(HttpContext.User);
            if (task.AccessDenied(userId))
            {
                return RedirectToAction(nameof(TaskController.Details), "Task", new { id, error = "Invalid task" });
            }

            return View(turn);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Edit(long id, long taskId, EditTurnViewModel model)
        {
            var turn = _taskData.GetTurn(id);
            if (null == turn)
            {
                return RedirectToAction(nameof(TaskController.Details), "Task", new { id = taskId, error = "Invalid turn" });
            }
            var task = _taskData.GetTaskDetails(turn.TaskId);
            var userId = _userManager.GetUserId(HttpContext.User);
            if (task.AccessDenied(userId))
            {
                return RedirectToAction(nameof(TaskController.Details), "Task", new { id, error = "Invalid task" });
            }

            if (!ModelState.IsValid)
            {
                return View(turn);
            }
            turn.Taken = model.Taken;
            turn.Modified = DateTimeOffset.UtcNow;
            _taskData.Commit();
            return RedirectToAction(nameof(TaskController.Details), "Task", new { id = taskId });
        }
    }
}
