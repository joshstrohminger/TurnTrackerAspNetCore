using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
            if (_siteSettings.Save())
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
            var notes = await GetNotifications();
            return View(notes);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SendNotifications()
        {
            var notes = await GetNotifications();
            foreach (var note in notes)
            {
                note.Sent = await _emailSender.SendEmailAsync(note.Subject, note.Message, EmailCategory.Reminder, note.Addresses);
            }

            return View(nameof(PreviewNotifications), notes);
        }

        private async Task<List<NotificationEmail>> GetNotifications()
        {

            var counts = _taskData.GetTurnCounts();
            var tasks = _taskData.GetAllTasks(true).ToList();
            var latest = _taskData.GetLatestTurns(tasks.Select(x => x.Id).ToArray()).ToList();
            var taskCounts = new Dictionary<TrackedTask, TurnCount>();
            foreach (var task in tasks)
            {
                task.PopulateLatestTurnInfo(counts, taskCounts, latest);
            }

            var notifications = new List<NotificationEmail>();
            foreach (var x in taskCounts.Where(x => x.Key.Overdue))
            {
                var note = await SendNotificationEmailsAsync(x.Key, x.Value, Url, HttpContext);
                notifications.Add(note);
            }

            return notifications;
        }

        public class NotificationEmail
        {
            public string Subject { get; set; }
            public string Message { get; set; }
            public string[] Addresses { get; set; }
            public bool? Sent { get; set; }
        }

        private async Task<NotificationEmail> SendNotificationEmailsAsync(TrackedTask task, TurnCount count, IUrlHelper url, HttpContext context)
        {
            var callbackUrl = url.Action(nameof(TaskController.Details), "Task", new { id = task.Id }, protocol: context.Request.Scheme);

            var sb = new StringBuilder();
            sb.Append(task.TeamBased ? "Someone needs" : "You need");
            sb.AppendFormat(" to <a href=\"{0}\">{1}</a>. It's overdue by {2}.", callbackUrl, task.Name, GetRoundedTimeSpan(task.DueTimeSpan));
            var latest = await _userManager.FindByIdAsync(count.UserId);
            if (null == latest)
            {
                // todo maybe email should be included in TurnCount
                return null;
                // return false;
            }
            return new NotificationEmail
            {
                Message = sb.ToString(),
                Subject = $"{_siteSettings.Settings.General.Name} Reminder",
                Addresses = task.TeamBased
                        ? task.Participants.Select(x => x.User).Where(x => x.EmailConfirmed).Select(x => x.Email).ToArray()
                        : new [] { latest.Email }
            };
            //var success = await _emailSender.SendEmailAsync(
            //    $"{_siteSettings.Settings.General.Name} Reminder",
            //    sb.ToString(),
            //    EmailCategory.Reminder,
            //    task.TeamBased
            //        ? task.Participants.Select(x => x.User).Where(x => x.EmailConfirmed).Select(x => x.Email).ToArray()
            //        : new[] { latest.Email });
            //return success;
        }

        private string GetRoundedTimeSpan(TimeSpan time)
        {
            var months = time.Days / 30;
            if (months >= 1)
            {
                return string.Format("{0} month{1}", months, months > 1 ? "s" : "");
            }
            if (time.Days >= 1)
            {
                return string.Format("{0} day{1}", time.Days, time.Days > 1 ? "s" : "");
            }
            if (time.Hours >= 1)
            {
                return string.Format("{0} hour{1}", time.Hours, time.Hours > 1 ? "s" : "");
            }
            if (time.Minutes >= 1)
            {
                return string.Format("{0} minute{1}", time.Minutes, time.Minutes > 1 ? "s" : "");
            }
            if (time.Seconds >= 1)
            {
                return string.Format("{0} second{1}", time.Seconds, time.Seconds > 1 ? "s" : "");
            }

            // shouldn't ever get here
            return time.ToString(@"hh\:mm\:ss");
        }
    }
}
