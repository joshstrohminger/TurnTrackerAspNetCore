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
using TurnTrackerAspNetCore.ViewModels.Account;
using TurnTrackerAspNetCore.ViewModels.Admin;
using TurnTrackerAspNetCore.ViewModels.Task;

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
        private readonly ISiteSettings _siteSettings;

        public AdminController(ITaskData taskData, RoleManager<IdentityRole> roleManager, UserManager<User> userManager, IEmailSender emailSender, ILoggerFactory loggerFactory, ISiteSettings siteSettings)
        {
            _taskData = taskData;
            _roleManager = roleManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _siteSettings = siteSettings;
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

        public IActionResult Users(string errorMessage = null)
        {
            ViewBag.Roles = _roleManager.Roles.ToDictionary(x => x.Id, x => x.Name);
            ViewBag.MyUserId = _userManager.GetUserId(User);
            ViewBag.ErrorMessage = errorMessage;
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
            var myName = _userManager.GetUserName(User);

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
                foreach (var role in rolesToRemove)
                {
                    _logger.LogInformation(EventIds.UserRoleRemoved, $"{role} removed from {user.UserName} by {myName}");
                }
                foreach (var role in rolesToAdd)
                {
                    _logger.LogInformation(EventIds.UserRoleAdded, $"{role} added to {user.UserName} by {myName}");
                }
                _logger.LogInformation(EventIds.UserProfileModifiedByAdmin, $"{myName} modified {user.UserName}");
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
            var success = await _emailSender.SendEmailAsync("Test Email", $"This is a test from {name}.", EmailCategory.Confirm, model.Email);
            if (success)
            {
                _logger.LogInformation(EventIds.EmailConfirmationSent, name);
                return RedirectToAction(nameof(Test));
            }
            
            ModelState.AddModelError("", "Failed to send");
            return View(nameof(Test), model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ReSendConfirmationEmail(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var success = await _emailSender.SendConfirmationEmailAsync(user, Url, HttpContext);
            return RedirectToAction(nameof(Users), new {errorMessage = success ? "" : "Failed to send email"});
        }

        [HttpGet]
        public IActionResult SiteSettings()
        {
            if (!_siteSettings.Load())
            {
                ViewBag.ErrorMessage = "Failed to load";
            }
            return View(_siteSettings.Settings);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult SiteSettings(SiteSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _siteSettings.Settings = model;
            if (_siteSettings.Save())
            {
                return RedirectToAction(nameof(SiteSettings));
            }
            ViewBag.ErrorMessage = "Failed to save";
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Invite(SendEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(nameof(Users), model);
            }

            var name = _userManager.GetUserName(User);
            var success = await _emailSender.SendEmailAsync($"{_siteSettings.Settings.General.Name} Invite", $"This is an invite to register for {_siteSettings.Settings.General.Name}. Please followt his link: link", EmailCategory.Confirm, model.Email);
            if (success)
            {
                _logger.LogInformation(EventIds.EmailInviteSent, name);
                return RedirectToAction(nameof(Users));
            }

            ModelState.AddModelError("", "Failed to send");
            return View(nameof(Users), model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable(string id)
        {
            return await SetDisabled(id, true);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Enable(string id)
        {
            return await SetDisabled(id, false);
        }

        private async Task<IActionResult> SetDisabled(string id, bool disabled)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (null == user)
            {
                return new NotFoundResult();
            }
            var result = await _userManager.SetLockoutEndDateAsync(user, disabled ? DateTimeOffset.MaxValue : (DateTimeOffset?)null);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Users));
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(nameof(Users));
        }
    }
}
