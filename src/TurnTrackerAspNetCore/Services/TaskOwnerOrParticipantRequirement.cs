using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.Services
{

    public class TaskOwnerOrParticipantRequirement : AuthorizationHandler<TaskOwnerOrParticipantRequirement, TrackedTask>, IAuthorizationRequirement
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            TaskOwnerOrParticipantRequirement requirement,
            TrackedTask task)
        {
            string id;

            if (context.User.IsInRole(Roles.Admin) ||
                (null != (id = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value) &&
                null != task &&
                (task.UserId == id || task.Participants.Any(x => x.UserId == id))))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
