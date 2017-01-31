using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace TurnTrackerAspNetCore.Services
{
    public interface INoContextAccessor
    {
        IUrlHelper UrlHelper { get; }
        string Host { get; }

        void UpdateHost(string host);
        void UpdateRouter(IRouter router);
    }

    public class NoContextAccessor : INoContextAccessor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private IRouter _router;

        public string Host { get; private set; }

        public NoContextAccessor(IServiceProvider serviceProvider, IUrlHelperFactory urlHelperFactory)
        {
            _serviceProvider = serviceProvider;
            _urlHelperFactory = urlHelperFactory;
        }
        
        public IUrlHelper UrlHelper { get; private set; }

        public void UpdateHost(string host)
        {
            Host = host;
            BuildUrlHelper();
        }

        public void UpdateRouter(IRouter router)
        {
            _router = router;
            BuildUrlHelper();
        }

        private void BuildUrlHelper()
        {
            if (null != UrlHelper || null == _router || null == Host)
            {
                return;
            }

            var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
            httpContext.Request.Host = new HostString(Host);

            var routeData = new RouteData();
            routeData.Routers.Add(_router);

            UrlHelper = _urlHelperFactory.GetUrlHelper(new ActionContext(
                httpContext,
                routeData,
                new ActionDescriptor()));
        }
    }
}
