using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace TurnTrackerAspNetCore.Services
{
    public class UrlAccessor
    {
        public IUrlHelper Url { get; set; }
        public string BaseUrl { get; set; }
        public RouteData RouteData { get; set; }
        public HttpContext HttpContext { get; set; }
        public string Host { get; set; }
        public ActionContext ActionContext { get; set; }
    }
}
