using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using TurnTrackerAspNetCore.Entities;
using TurnTrackerAspNetCore.Services;
using TurnTrackerAspNetCore.ViewModels;

namespace TurnTrackerAspNetCore.Controllers
{
    [Authorize(Policy = nameof(Policies.CanAccessAdmin))]
    public class AdminController : Controller
    {
        private readonly ITaskData _taskData;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;

        public AdminController(ITaskData taskData, RoleManager<IdentityRole> roleManager, UserManager<User> userManager, IEmailSender emailSender, ILoggerFactory loggerFactory)
        {
            _taskData = taskData;
            _roleManager = roleManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = loggerFactory.CreateLogger<AdminController>();
        }

        public IActionResult Tasks(string error = null)
        {
            var counts = _taskData.GetTurnCounts();
            var tasks = _taskData.GetAllTasks().ToList();
            var latest = _taskData.GetLatestTurns(tasks.Select(x => x.Id).ToArray()).ToList();
            var taskCounts = new Dictionary<TrackedTask, TurnCount>();
            foreach (var task in tasks)
            {
                task.PopulateLatestTurnInfo(counts, taskCounts, latest);
            }

            return View("Tasks", new TasksViewModel { TaskCounts = taskCounts, Error = error });
        }

        public IActionResult Users()
        {
            ViewBag.Roles = _roleManager.Roles.ToDictionary(x => x.Id, x => x.Name);
            ViewBag.MyUserId = _userManager.GetUserId(User);
            return View(_taskData.GetAllUsersWithRoles().OrderBy(x => x.UserName).ToList());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var myUserid = _userManager.GetUserId(User);
            if (id == myUserid)
            {
                return new ChallengeResult();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (null == user)
            {
                return new NotFoundResult();
            }
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                var myUserName = _userManager.GetUserName(User);
                _logger.LogInformation(EventIds.UserDeleted, $"{myUserName} deleted {user.UserName}");
            }
            // todo show errors
            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (null == user)
            {
                return new NotFoundResult();
            }

            var myId = _userManager.GetUserId(User);
            if (myId == id)
            {
                return new ChallengeResult();
            }

            var roles = new List<string>();
            foreach (var role in Enum.GetNames(typeof(Roles)))
            {
                if (await _userManager.IsInRoleAsync(user, role))
                {
                    roles.Add(role);
                }
            }

            var model = new EditAccountViewModel
            {
                DisplayName = user.DisplayName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                UserName = user.UserName,
                Roles = roles,
                AllRoles = Enum.GetNames(typeof(Roles)).Select(x => new SelectListItem {Value = x, Text = x, Selected = roles.Contains(x)}).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(string id, EditAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(id);
            if (null == user)
            {
                return new NotFoundResult();
            }

            var myId = _userManager.GetUserId(User);
            if (myId == id)
            {
                return new ChallengeResult();
            }

            user.DisplayName = model.DisplayName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            var results = new List<IdentityResult> {await _userManager.UpdateAsync(user)};

            var rolesToAdd = new List<string>();
            var rolesToRemove = new List<string>();
            foreach (var role in Enum.GetNames(typeof(Roles)))
            {
                var isIn = await _userManager.IsInRoleAsync(user, role);
                if ((model.Roles?.Contains(role) ?? false) != isIn)
                {
                    if (isIn)
                    {
                        rolesToRemove.Add(role);
                    }
                    else
                    {
                        rolesToAdd.Add(role);
                    }
                }
            }
            results.Add(await _userManager.AddToRolesAsync(user, rolesToAdd));
            results.Add(await _userManager.RemoveFromRolesAsync(user, rolesToRemove));

            if (results.All(x => x.Succeeded))
            {
                return RedirectToAction(nameof(Users));
            }

            foreach (var error in results.SelectMany(x => x.Errors))
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Test()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEmail(SendEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(nameof(Test), model);
            }

            var name = _userManager.GetUserName(User);
            var success = await _emailSender.SendEmailAsync("Test Email", $"This is a test from {name}.", "Confirm", model.Email);
            if (success)
            {
                return RedirectToAction(nameof(Test));
            }
            
            ModelState.AddModelError("", "Failed to send");
            return View(nameof(Test), model);
        }
    }
}
