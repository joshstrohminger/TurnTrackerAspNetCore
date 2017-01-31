using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using TurnTrackerAspNetCore.Entities;
using TurnTrackerAspNetCore.Services;
using TurnTrackerAspNetCore.Services.Data;
using TurnTrackerAspNetCore.Services.Jobs;
using TurnTrackerAspNetCore.Services.Settings;
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
        private readonly Notifier _notifier;
        private readonly IServiceProvider _services;

        public AdminController(ITaskData taskData, RoleManager<IdentityRole> roleManager, UserManager<User> userManager, IEmailSender emailSender, ILoggerFactory loggerFactory, ISiteSettings siteSettings, Notifier notifier, IServiceProvider services)
        {
            _taskData = taskData;
            _roleManager = roleManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _siteSettings = siteSettings;
            _notifier = notifier;
            _services = services;
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

            return View(new TasksViewModel { TaskCounts = taskCounts, Error = error });
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
            if(user.Email != model.Email)
            {
                user.EmailConfirmed = false;
            }
            user.Email = model.Email;
            //user.PhoneNumber = model.PhoneNumber;

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
        public IActionResult Invites(string errorMessage = null, string infoMessage = null)
        {
            ViewBag.ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? null : errorMessage;
            ViewBag.InfoMessage = string.IsNullOrWhiteSpace(infoMessage) ? null : infoMessage;
            return View(_taskData.GetAllInvites().OrderByDescending(x => x.Sent).ThenByDescending(x => x.Created).ToList());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddInvite(SendEmailViewModel model)
        {
            if (_siteSettings.Settings.General.RegistrationMode != RegistrationMode.InviteOnly)
            {
                return RedirectToAction(nameof(Invites), new {errorMessage = "Registration Mode must be set to InviteOnly"});
            }

            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Invites), new {errorMessage = "Invalid email"});
            }

            var user = await _userManager.GetUserAsync(User);

            var invite = new Invite
            {
                Email = model.Email,
                InviterId = user.Id,
                Expires = DateTimeOffset.UtcNow.AddHours(_siteSettings.Settings.General.InviteExpirationHours)
            };

            _taskData.AddInvite(invite);
            _taskData.Commit();

            if (invite.Token == Guid.Empty)
            {
                return RedirectToAction(nameof(Invites), new {errorMessage = "Failed to generate invite"});
            }
            
            var success = await _emailSender.SendInviteEmailAsync(invite, Url, HttpContext);
            if (success)
            {
                _logger.LogInformation(EventIds.EmailInviteSent, $"{user.UserName} invited '{invite.Email}'");
                invite.Sent = DateTimeOffset.UtcNow;
                _taskData.Commit();
                return RedirectToAction(nameof(Invites), new {errorMessage = (string)null, infoMessage = "Sent invite"});
            }
            
            return RedirectToAction(nameof(Invites), new { errorMessage = "Failed to send email" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult DeleteInvite(Guid token)
        {
            var invite = _taskData.GetInvite(token);
            if (null == invite)
            {
                return new NotFoundResult();
            }
            _taskData.DeleteInvite(invite);
            _taskData.Commit();
            return RedirectToAction(nameof(Invites), new {errorMessage = (string)null, infoMessage = "Deleted Invite"});
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
            if (_siteSettings.Save(_services))
            {
                return RedirectToAction(nameof(SiteSettings));
            }
            ViewBag.ErrorMessage = "Failed to save";
            return View(model);
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

        public async Task<IActionResult> PreviewNotifications()
        {
            var notes = await _notifier.GetNotificationsAsync();
            ViewBag.NoteCount = Notifier.RunCount;
            return View(notes);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SendNotifications()
        {
            var notes = await _notifier.SendNotificationsAsync();
            return View(nameof(PreviewNotifications), notes);
        }

        public IActionResult Test(int id = -1)
        {
            var model = typeof(Cron)
                .GetMethods()
                .Where(x => x.IsStatic && x.IsPublic)
                .Select((x,i) => new MethodThing
                {
                    Name = x.Name,
                    Params = x.GetParameters(),
                    Id = i
                }).ToList();
            if (id >= 0 && id < model.Count)
            {
                ViewBag.Row = model[id];
                if (null == ViewBag.Info && (ViewBag.Row.Params?.Length ?? 0) == 0)
                {
                    return TestCron(id, new CronInfo {Name = ViewBag.Row.Name});
                }
            }
            return View(nameof(Test), model);
        }

        public IActionResult TestCron(int id, CronInfo info)
        {
            var types = new List<Type>();
            var values = new List<object>();

            if (info.DayOfWeek.HasValue)
            {
                types.Add(typeof(DayOfWeek));
                values.Add(info.DayOfWeek.Value);
            }

            foreach (var p in typeof(CronInfo).GetProperties().Where(x => x.PropertyType == typeof(int?)))
            {
                var i = (int?) p.GetValue(info);
                if (i.HasValue)
                {
                    types.Add(typeof(int));
                    values.Add(i.Value);
                }
            }

            var method = typeof(Cron).GetMethod(info.Name, types.ToArray());
            ViewBag.Cron = method.Invoke(null, values.ToArray());
            ViewBag.Info = info;
            return Test(id);
        }

        public class MethodThing
        {
            public string Name { get; set; }
            public ParameterInfo[] Params { get; set; }
            public int Id { get; set; }
        }

        public class CronInfo
        {
            public string Name { get; set; }
            public DayOfWeek? DayOfWeek { get; set; }
            public int? Interval { get; set; }
            public int? Month { get; set; }
            public int? Day { get; set; }
            public int? Hour { get; set; }
            public int? Minute { get; set; }
        }
    }
}
