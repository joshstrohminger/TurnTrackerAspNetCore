using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.Services
{
    public class TaskOwnerRequirement : AuthorizationHandler<TaskOwnerRequirement, TrackedTask>, IAuthorizationRequirement
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            TaskOwnerRequirement requirement,
            TrackedTask task)
        {
            string id;

            if (context.User.IsInRole(nameof(Roles.Admin)) ||
                (null != (id = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value) &&
                null != task &&
                task.UserId == id))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
