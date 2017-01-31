using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace TurnTrackerAspNetCore.Middleware
{
    public static class TimezoneOffsetReader
    {
        public static string CookieKey => "TimezoneOffset";
    }
}
