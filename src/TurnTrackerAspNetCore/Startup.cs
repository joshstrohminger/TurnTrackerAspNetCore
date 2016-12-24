﻿using System;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TurnTrackerAspNetCore.Entities;
using TurnTrackerAspNetCore.Services;

namespace TurnTrackerAspNetCore
{
    public class Startup
    {
        public IConfiguration Configuration { get; set; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                builder.AddUserSecrets();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(x => x.UseSqlServerStorage(Configuration.GetConnectionString("TurnTracker")));
            services.AddMvc();
            services.AddSingleton(Configuration);
            services.AddSingleton<ISiteSettings, SiteSettings>();
            services.AddScoped<ITaskData, SqlTaskData>();
            services.AddSingleton<Notifier>();
            services.AddDbContext<TurnTrackerDbContext>(
                options => options.UseSqlServer(
                    Configuration.GetConnectionString("TurnTracker")));
            services.AddIdentity<User, IdentityRole>()
                .AddEntityFrameworkStores<TurnTrackerDbContext>()
                .AddDefaultTokenProviders();

            services.AddAuthorization(options =>
            {
                options.AddPolicy(nameof(Policies.CanAccessTask), policy => policy.AddRequirements(new TaskOwnerOrParticipantRequirement()));
                options.AddPolicy(nameof(Policies.CanDeleteTask), policy => policy.AddRequirements(new TaskOwnerRequirement()));
                options.AddPolicy(nameof(Policies.CanAccessAdmin), policy => policy.RequireRole(nameof(Roles.Admin)));
            });

            services.AddTransient<IEmailSender, AuthMessageSender>();
            //services.AddTransient<ISmsSender, AuthMessageSender>();
            services.Configure<AuthMessageSenderOptions>(Configuration);

            services.Configure<IdentityOptions>(options =>
            {
                //options.User.RequireUniqueEmail = true;
                //options.SignIn.RequireConfirmedEmail = true;
                //options.SignIn.RequireConfirmedPhoneNumber = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, TurnTrackerDbContext db, RoleManager<IdentityRole> roleManager, Notifier notifier)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            db.Database.Migrate();

            if (env.IsDevelopment())
            {
                loggerFactory.AddFile("Logs/turntracker-{Date}.txt");
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler(new ExceptionHandlerOptions
                {
                    ExceptionHandler = context => context.Response.WriteAsync(@"Oops, I messed up! ¯\_(ツ)_/¯")
                });
            }

            var dashboardOptions = new DashboardOptions
            {
                Authorization = new IDashboardAuthorizationFilter[]
                {
                    new TestDashboardAuth()
                }
            };

            app.UseStaticFiles();
            app.UseStatusCodePagesWithReExecute("/Error/{0}");
            app.UseIdentity();
            app.UseHangfireDashboard("/admin/hangfire", dashboardOptions);
            app.UseHangfireServer();
            app.UseMvc(ConfigureRoutes);

            ConfigureRoles(roleManager).Wait();
            ConfigureJobs(notifier);
        }

        private void ConfigureRoutes(IRouteBuilder routeBuilder)
        {
            routeBuilder.MapRoute("tasks", "tasks", new { controller = "Task", action = "Index" });
            routeBuilder.MapRoute("error", "error/{code}", new { controller = "About", action = "Error" });
            routeBuilder.MapRoute("default", "{controller=task}/{action=Index}/{id?}");
        }

        private async Task ConfigureRoles(RoleManager<IdentityRole> roleManager)
        {
            foreach (var role in Enum.GetNames(typeof(Roles)))
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private void ConfigureJobs(Notifier notifier)
        {
            RecurringJob.AddOrUpdate("My Test Job", () => notifier.TestJob(), Cron.Minutely);
        }
    }

    public class TestDashboardAuth : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return context.GetHttpContext().User.IsInRole(nameof(Roles.Admin));
        }
    }

    public class Notifier
    {
        public int Count { get; private set; }
        public void TestJob()
        {
            Count++;
        }
    }
}
