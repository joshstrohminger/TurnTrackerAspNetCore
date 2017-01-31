using Microsoft.AspNetCore.Builder;
using TurnTrackerAspNetCore.Services;

namespace TurnTrackerAspNetCore.Middleware
{
    public static class NoContextAccessorMiddleware
    {
        public static IApplicationBuilder UseHostAccessor(this IApplicationBuilder app, INoContextAccessor noContextAccessor)
        {
            return app.Use((context, next) =>
            {
                noContextAccessor.UpdateHost(context.Request.Host.Value);
                return next();
            });
        }
    }
}
