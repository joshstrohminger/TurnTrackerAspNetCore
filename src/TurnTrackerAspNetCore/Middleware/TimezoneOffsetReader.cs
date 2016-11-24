using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace TurnTrackerAspNetCore.Middleware
{
    public static class TimezoneOffsetReader
    {
        public static string CookieKey => "TimezoneOffset";

        public static IApplicationBuilder UseTimezoneOffsetReader(this IApplicationBuilder app)
        {
            return app.Use((context, next) =>
            {
                string offset;
                if (!context.Request.Cookies.TryGetValue(CookieKey, out offset))
                {
                    offset = "0";
                }
                context.Response.Cookies.Append(CookieKey, offset);
                return next();
            });
        }

        //public static TimeSpan GetTimezoneOffset(this IResponseCookies cookies)
        //{
        //    var offset = cookies.
        //    int minutes;
        //    int.TryParse(offset, out minutes);
        //    return TimeSpan.FromMinutes(minutes);
        //}
    }
}
