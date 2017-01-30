using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;

namespace TurnTrackerAspNetCore.Services
{
    public class RouterAccessor
    {
        public IRouter Router { get; set; }
    }
}
