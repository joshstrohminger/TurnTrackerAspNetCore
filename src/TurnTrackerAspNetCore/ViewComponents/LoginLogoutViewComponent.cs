using Microsoft.AspNetCore.Mvc;

namespace TurnTrackerAspNetCore.ViewComponents
{
    public class LoginLogoutViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
