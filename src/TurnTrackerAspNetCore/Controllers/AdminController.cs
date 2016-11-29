using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using TurnTrackerAspNetCore.Entities;
using TurnTrackerAspNetCore.Services;

namespace TurnTrackerAspNetCore.Controllers
{
    [Authorize(Roles = nameof(Roles.Admin))]
    public class AdminController : Controller
    {
        private readonly ITaskData _taskData;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<User> _userManager;

        public AdminController(ITaskData taskData, RoleManager<IdentityRole> roleManager, UserManager<User> userManager)
        {
            _taskData = taskData;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public IActionResult Users()
        {
            ViewBag.Roles = _roleManager.Roles.ToDictionary(x => x.Id, x => x.Name);
            ViewBag.MyUserId = _userManager.GetUserId(User);
            return View(_taskData.GetAllUsersWithRoles().OrderBy(x => x.UserName).ToList());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var myUserid = _userManager.GetUserId(User);
            if (id == myUserid)
            {
                return new ChallengeResult();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (null == user)
            {
                return new NotFoundResult();
            }
            await _userManager.DeleteAsync(user);
            // todo show errors
            return RedirectToAction(nameof(Users));
        }
    }
}
