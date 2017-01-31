using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using TurnTrackerAspNetCore.Entities;
using TurnTrackerAspNetCore.Services.Data;
using TurnTrackerAspNetCore.ViewModels.Admin;

namespace TurnTrackerAspNetCore.Services.Jobs
{
    public class Notifier
    {
        private readonly IEmailSender _emailSender;
        private readonly ITaskData _taskData;
        private readonly INoContextAccessor _noContextAccessor;

        public static int RunCount { get; private set; }

        public void TestJob(string host)
        {
            RunCount++;

            // override the host in case it hasn't been set yet (no request has run before this job)
            _noContextAccessor.UpdateHost(host);
        }

        // todo, move notification sending into authmessagesender but leave the logic for
        // todo, generating them in it's own class, maybe NotificationManager or something
        // todo, like that which will have more responsibilities later

        public Notifier(IEmailSender emailSender, ITaskData taskData, INoContextAccessor noContextAccessor)
        {
            _emailSender = emailSender;
            _taskData = taskData;
            _noContextAccessor = noContextAccessor;
        }

        public static void Start(IServiceProvider services, JobSetting setting)
        {
            var notifier = services.GetRequiredService<Notifier>();
            var noContextAccessor = services.GetRequiredService<INoContextAccessor>();
            RecurringJob.AddOrUpdate(setting.Id, () => notifier.TestJob(noContextAccessor.Host), setting.CronSchedule);
        }

        public async Task<List<NotificationEmail>> SendNotificationsAsync()
        {
            var notes = await GetNotificationsAsync();
            foreach (var note in notes)
            {
                note.Sent = await _emailSender.SendEmailAsync(note.Subject, note.Message, EmailCategory.Reminder, note.Addresses);
            }
            return notes;
        }

        public async Task<List<NotificationEmail>> GetNotificationsAsync()
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
                var note = await _emailSender.CreateNotificationEmailAsync(x.Key, x.Value, _noContextAccessor.UrlHelper);
                if (null != note)
                {
                    notifications.Add(note);
                }
            }

            return notifications;
        }
    }
}