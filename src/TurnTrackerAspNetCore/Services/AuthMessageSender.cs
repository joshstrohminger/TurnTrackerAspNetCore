using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TurnTrackerAspNetCore.Controllers;
using TurnTrackerAspNetCore.Entities;
using TurnTrackerAspNetCore.Services.Settings;

namespace TurnTrackerAspNetCore.Services
{
    public class AuthMessageSenderOptions
    {
        public string SendGridKey { get; set; }
    }

    public enum EmailCategory
    {
        Confirm,
        Invite,
        Reminder
    }

    public class NotificationEmail
    {
        public string Subject { get; set; }
        public string Message { get; set; }
        public string[] Addresses { get; set; }
        public bool? Sent { get; set; }
    }

    public interface IEmailSender
    {
        Task<bool> SendEmailAsync(string subject, string message, EmailCategory category, params string[] destinations);
        Task<bool> SendConfirmationEmailAsync(User user, IUrlHelper url, HttpContext context);
        Task<bool> SendInviteEmailAsync(Invite invite, IUrlHelper url, HttpContext context);

        Task<NotificationEmail> CreateNotificationEmailAsync(TrackedTask task, TurnCount count, IUrlHelper url, string host,
            string protocol = SiteSettings.Protocol);
    }

    public interface ISmsSender
    {
        Task SendSmsAsync(string number, string message);
    }

    public class AuthMessageSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;
        private readonly UserManager<User> _userManager;
        private readonly ISiteSettings _siteSettings;

        public AuthMessageSender(IOptions<AuthMessageSenderOptions> optionsAccessor, IConfiguration config,
            ILoggerFactory loggerFactory, UserManager<User> userManager, ISiteSettings siteSettings)
        {
            _config = config;
            _userManager = userManager;
            _siteSettings = siteSettings;
            _logger = loggerFactory.CreateLogger<AuthMessageSender>();
            Options = optionsAccessor.Value;
        }

        public AuthMessageSenderOptions Options { get; } //set only via Secret Manager

        public async Task<bool> SendEmailAsync(string subject, string message, EmailCategory category, params string[] destinations)
        {
            if (destinations.Length == 0)
            {
                _logger.LogError($"No destinations provided to {nameof(SendEmailAsync)}");
                return false;
            }

            var section = _config.GetSection("Mail").GetSection(category.ToString());
            var fromAddress = section["Address"];
            var fromName = section["Name"];
            if (string.IsNullOrWhiteSpace(fromAddress) || string.IsNullOrWhiteSpace(fromName))
            {
                _logger.LogError($"Couldn't find 'Address' and 'Name' for email category '{category}'");
                return false;
            }

            var myMessage = new SendGrid.SendGridMessage
            {
                From = new System.Net.Mail.MailAddress(fromAddress, string.Format(fromName, _siteSettings.Settings.General.Name)),
                Subject = subject,
                Text = message,
                Html = message
            };
            foreach (var to in destinations)
            {
                myMessage.AddTo(to);
            }

            var transportWeb = new SendGrid.Web(Options.SendGridKey);
            try
            {
                await transportWeb.DeliverAsync(myMessage);
                return true;
            }
            catch (InvalidApiRequestException e)
            {
                _logger.LogError(EventIds.EmailError, string.Join(Environment.NewLine, new[] {e.Message}.Union(e.Errors ?? new string[0])));
            }
            catch (Exception e)
            {
                _logger.LogError(EventIds.EmailErrorUnknown, e, $"Category '{category}', Subject '{subject}'");
            }
            return false;
        }

        public async Task<bool> SendConfirmationEmailAsync(User user, IUrlHelper url, HttpContext context)
        {
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = url.Action(nameof(AccountController.ConfirmEmail), "Account", new { userId = user.Id, code }, context.Request.Scheme);
            var success = await SendEmailAsync("Confirm your account",
               $"Please confirm your account by clicking this <a href='{callbackUrl}'>link</a>",
               EmailCategory.Confirm, user.Email);
            return success;
        }

        public async Task<bool> SendInviteEmailAsync(Invite invite, IUrlHelper url, HttpContext context)
        {
            var callbackUrl = url.Action(nameof(AccountController.Register), "Account", new { inviteToken = invite.Token}, protocol: context.Request.Scheme);
            var success = await SendEmailAsync($"{_siteSettings.Settings.General.Name} Invite",
                $"This is an invite to register for {_siteSettings.Settings.General.Name}. Please follow this <a href='{callbackUrl}'>link</a><br>It is only valid for {_siteSettings.Settings.General.InviteExpirationHours} hours.",
                EmailCategory.Invite, invite.Email);
            return success;
        }

        public async Task<NotificationEmail> CreateNotificationEmailAsync(TrackedTask task, TurnCount count, IUrlHelper url, string host, string protocol = SiteSettings.Protocol)
        {
            //var parts = host.Split(':');
            //if (parts.Length == 2)
            {
                url.ActionContext.HttpContext.Request.Host = new HostString(host);
            }
            var callbackUrl = url.Action(nameof(TaskController.Details), "Task", new { id = task.Id }, protocol);

            var sb = new StringBuilder();
            sb.Append(task.TeamBased ? "Someone needs" : "You need");
            sb.AppendFormat(" to <a href=\"{0}\">{1}</a>.", callbackUrl, task.Name);
            if ((task.Turns?.Count ?? 0) == 0)
            {
                sb.Append(" No turns have been taken.");
            }
            else
            {
                sb.AppendFormat(" It's overdue by {0}.", GetRoundedTimeSpan(task.DueTimeSpan));
            }

            var latest = await _userManager.FindByIdAsync(count.UserId);
            if (null == latest)
            {
                // todo maybe email should be included in TurnCount
                return null;
            }
            return new NotificationEmail
            {
                Message = sb.ToString(),
                Subject = $"{_siteSettings.Settings.General.Name} Reminder",
                Addresses = task.TeamBased
                        ? task.Participants.Select(x => x.User).Where(x => x.EmailConfirmed).Select(x => x.Email).ToArray()
                        : new[] { latest.Email }
            };
        }

        private static string GetRoundedTimeSpan(TimeSpan time)
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
