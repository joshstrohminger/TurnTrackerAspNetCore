﻿using System;
using System.Threading.Tasks;
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
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSingleton(Configuration);
            services.AddScoped<ITaskData, SqlTaskData>();
            services.AddDbContext<TurnTrackerDbContext>(
                options => options.UseSqlServer(
                    Configuration.GetConnectionString("TurnTracker")));
            services.AddIdentity<User, IdentityRole>()
                .AddEntityFrameworkStores<TurnTrackerDbContext>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy(nameof(Policies.CanAccessTask), policy => policy.AddRequirements(new TaskOwnerOrParticipantRequirement()));
                options.AddPolicy(nameof(Policies.CanAccessAdmin), policy => policy.RequireRole(nameof(Roles.Admin)));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, TurnTrackerDbContext db, RoleManager<IdentityRole> roleManager)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            db.Database.Migrate();
            ConfigureRoles(roleManager).Wait();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler(new ExceptionHandlerOptions
                {
                    ExceptionHandler = context => context.Response.WriteAsync(@"I messed up! ¯\_(ツ)_/¯")
                });
            }

            app.UseStaticFiles();
            //app.UseStatusCodePages();
            app.UseIdentity();
            app.UseMvc(ConfigureRoutes);
        }

        private void ConfigureRoutes(IRouteBuilder routeBuilder)
        {
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
    }
}
