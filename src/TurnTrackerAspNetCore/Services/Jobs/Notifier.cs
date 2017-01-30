using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
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
        private IUrlHelper _url;
        private IUrlHelperFactory _urlHelperFactory;
        private string _host;
        public static int Count { get; private set; }
        public void TestJob()
        {
            Count++;
            if (_emailSender != null)
            {
                Count += 2;
            }
        }

        //public void TestJob(string )

        public void TestJob(string host)
        {
            Count += 5;
            _host = host;
        }

        public void TestJob(HttpContext h, RouteData d)
        {
            Count += 10;
            var a = new ActionContext(h, d, new ActionDescriptor());
            _url = _urlHelperFactory.GetUrlHelper(a);
        }

        // todo, move notification sending into authmessagesender but leave the logic for
        // todo, generating them in it's own class, maybe NotificationManager or something
        // todo, like that which will have more responsibilities later

        public Notifier(IEmailSender emailSender, ITaskData taskData,
            IUrlHelperFactory urlHelperFactory,
            RouterAccessor routerAccessor,
            IServiceProvider serviceProvider)
        {
            _emailSender = emailSender;
            _urlHelperFactory = urlHelperFactory;
            _taskData = taskData;
            var routeData = new RouteData();
            var httpContext = new DefaultHttpContext {RequestServices = serviceProvider};
            routeData.Routers.Add(routerAccessor.Router);
            _url = urlHelperFactory.GetUrlHelper(new ActionContext(
                httpContext,
                routeData,
                new ActionDescriptor()));
        }

        public static void Start(IServiceProvider services, JobSetting setting)
        {
            var notifier = services.GetRequiredService<Notifier>();
            var url = services.GetRequiredService<UrlAccessor>();
            notifier._host = url.Host;
            RecurringJob.AddOrUpdate(setting.Id, () => notifier.TestJob(url.Host), setting.CronSchedule);
        }

        public async Task<List<NotificationEmail>> GetNotifications()
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
                var note = await _emailSender.CreateNotificationEmailAsync(x.Key, x.Value, _url, _host);
                if (null != note)
                {
                    notifications.Add(note);
                }
            }

            return notifications;
        }
    }
}