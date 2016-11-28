using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TurnTrackerAspNetCore.Entities;
using TurnTrackerAspNetCore.Services;
using TurnTrackerAspNetCore.ViewModels;

namespace TurnTrackerAspNetCore.Controllers
{
    public class AccountController : Controller
    {
        private readonly ITaskData _taskData;
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<User> _roleManager;

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, ITaskData taskData, RoleManager<User> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _taskData = taskData;
            _roleManager = roleManager;
        }

        [HttpGet]
        public IActionResult Register(string returnUrl)
        {
            return View(new RegisterUserViewModel {ReturnUrl = returnUrl});
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // ensure empty or whitespace is converted to null
                var displayName = model.DisplayName;
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    displayName = null;
                }
                var user = new User
                {
                    UserName = model.UserName,
                    DisplayName = displayName
                };

                var numberExistingUsers = _taskData.GetAllUsers().Count();

                var createResult = await _userManager.CreateAsync(user, model.Password);
                if (createResult.Succeeded)
                {
                    await _signInManager.SignInAsync(user, false);
                    if (Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    return RedirectToAction(nameof(TaskController.Index), nameof(TaskController));
                }

                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(TaskController.Index), nameof(TaskController));
        }

        [HttpGet]
        public IActionResult Login(string returnUrl)
        {
            return View(new LoginViewModel {ReturnUrl = returnUrl});
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var loginResult =
                    await _signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, false);
                if (loginResult.Succeeded)
                {
                    if (Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    else
                    {
                        return RedirectToAction(nameof(TaskController.Index), nameof(TaskController));
                    }
                }
                ModelState.AddModelError("", "Login Failed");
            }
            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            return View(user);
        }
    }
}
