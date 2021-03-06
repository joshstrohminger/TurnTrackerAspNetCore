﻿using System;
using System.Reflection;
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
using TurnTrackerAspNetCore.ViewModels.Admin;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using TurnTrackerAspNetCore.Middleware;
using TurnTrackerAspNetCore.Services.Data;
using TurnTrackerAspNetCore.Services.Jobs;
using TurnTrackerAspNetCore.Services.Settings;

namespace TurnTrackerAspNetCore
{
    public class MyOptions
    {
        public bool ShowErrors { get; set; }
        public bool AzureLogging { get; set; }
    }

    public class Startup
    {
        private IConfiguration Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

            if (env.IsDevelopment())
            {
                builder.AddUserSecrets<Startup>();
            }
            
            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(x => x.UseSqlServerStorage(Configuration.GetConnectionString("TurnTracker")));
            services.AddMvc();
            services.AddSingleton(Configuration);
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddScoped<ITaskData, SqlTaskData>();
            services.AddSingleton<INoContextAccessor, NoContextAccessor>();
            services.AddSingleton<ISiteSettings, SiteSettings>();
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
            services.Configure<AuthMessageSenderOptions>(Configuration);
            services.Configure<MyOptions>(Configuration);

            services.Configure<IdentityOptions>(options =>
            {
                //options.User.RequireUniqueEmail = true;
                //options.SignIn.RequireConfirmedEmail = true;
                //options.SignIn.RequireConfirmedPhoneNumber = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, TurnTrackerDbContext db, RoleManager<IdentityRole> roleManager, IServiceProvider services, INoContextAccessor noContextAccessor, IOptions<MyOptions> myOptionsAccessor)
        {
            var myOptions = myOptionsAccessor.Value;
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            if (myOptions.AzureLogging)
            {
                //loggerFactory.AddAzureWebAppDiagnostics();
            }
            if (env.IsDevelopment())
            {
                loggerFactory.AddFile("Logs/turntracker-{Date}.txt");
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
            else
            {
                if (myOptions.ShowErrors)
                {
                    app.UseDeveloperExceptionPage();
                    app.UseDatabaseErrorPage();
                }
                app.UseExceptionHandler(new ExceptionHandlerOptions
                {
                    ExceptionHandler = context => context.Response.WriteAsync(@"Oops, I messed up! ¯\_(ツ)_/¯")
                });
            }
            db.Database.Migrate();

            var dashboardOptions = new DashboardOptions
            {
                Authorization = new IDashboardAuthorizationFilter[]
                {
                    new HangfireAdminDashboardAuth()
                }
            };

            app.UseStaticFiles();
            app.UseStatusCodePagesWithReExecute("/Error/{0}");
            app.UseIdentity();
            app.UseHostAccessor(noContextAccessor);
            app.UseHangfireDashboard("/admin/hangfire", dashboardOptions);
            app.UseHangfireServer();
            app.UseMvc(builder => ConfigureRoutes(builder, noContextAccessor));

            ConfigureRoles(roleManager).Wait();
            ConfigureJobs(services.GetRequiredService<ISiteSettings>(), services);
        }

        private static void ConfigureRoutes(IRouteBuilder routeBuilder, INoContextAccessor noContextAccessor)
        {
            routeBuilder.MapRoute("tasks", "tasks", new { controller = "Task", action = "Index" });
            routeBuilder.MapRoute("error", "error/{code}", new { controller = "About", action = "Error" });
            routeBuilder.MapRoute("default", "{controller=task}/{action=Index}/{id?}");
            noContextAccessor.UpdateRouter(routeBuilder.Build());
        }

        private static async Task ConfigureRoles(RoleManager<IdentityRole> roleManager)
        {
            foreach (var role in Enum.GetNames(typeof(Roles)))
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static void ConfigureJobs(ISiteSettings siteSettings, IServiceProvider services)
        {
            foreach (var job in siteSettings.Settings.Jobs.GetType().GetProperties()
                .Where(p => p.PropertyType == typeof(JobSetting)))
            {
                ((JobSetting)job.GetValue(siteSettings.Settings.Jobs)).Saved(services);
            }
        }

        private class HangfireAdminDashboardAuth : IDashboardAuthorizationFilter
        {
            public bool Authorize(DashboardContext context)
            {
                return context.GetHttpContext().User.IsInRole(nameof(Roles.Admin));
            }
        }
    }
}
