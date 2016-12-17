using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using TurnTrackerAspNetCore.Entities;
using TurnTrackerAspNetCore.Services;
using TurnTrackerAspNetCore.ViewModels.Account;
using TurnTrackerAspNetCore.ViewModels.Admin;

namespace TurnTrackerAspNetCore.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly ITaskData _taskData;
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger _logger;
        //private readonly ISmsSender _smsSender;
        private readonly IEmailSender _emailSender;
        private readonly ISiteSettings _siteSettings;

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, ITaskData taskData, IAuthorizationService authorizationService, ILoggerFactory loggerFactory, /*ISmsSender smsSender,*/ IEmailSender emailSender, ISiteSettings siteSettings)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _taskData = taskData;
            _authorizationService = authorizationService;
            //_smsSender = smsSender;
            _emailSender = emailSender;
            _siteSettings = siteSettings;
            _logger = loggerFactory.CreateLogger<AccountController>();
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        #region Register
        [HttpGet, AllowAnonymous]
        public IActionResult Register(string inviteToken = null)
        {
            switch (_siteSettings.Settings.General.RegistrationMode)
            {
                case RegistrationMode.Open:
                    return View();
                case RegistrationMode.InviteOnly:
                    if (string.IsNullOrWhiteSpace(inviteToken))
                    {
                        return View("RegistrationInviteOnly");
                    }
                    if (!IsValidRegistrationToken(inviteToken))
                    {
                        ViewBag.ErrorMessage = "Invalid Token";
                        return View("RegistrationInviteOnly");
                    }
                    return View(nameof(Register), new RegisterUserViewModel { InviteToken = inviteToken });
                case RegistrationMode.Closed:
                default:
                    return View("RegistrationClosed");
            }
        }

        private bool IsValidRegistrationToken(string token)
        {
            // todo check if it's a legit token
            return false;
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterUserViewModel model)
        {
            switch (_siteSettings.Settings.General.RegistrationMode)
            {
                case RegistrationMode.Open:
                    break;
                case RegistrationMode.InviteOnly:
                    if (string.IsNullOrWhiteSpace(model.InviteToken))
                    {
                        return View("RegistrationInviteOnly");
                    }
                    if (!IsValidRegistrationToken(model.InviteToken))
                    {
                        ViewBag.ErrorMessage = "Invalid Token";
                        return View("RegistrationInviteOnly");
                    }
                    break;
                case RegistrationMode.Closed:
                default:
                    return View("RegistrationClosed");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // ensure empty or whitespace is converted to null
            var displayName = model.DisplayName;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = null;
            }
            var user = new User
            {
                UserName = model.UserName,
                DisplayName = displayName,
                Email = model.Email,
                //PhoneNumber = model.PhoneNumber
            };

            var numberExistingUsers = _taskData.GetAllUsers().Count();

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation(EventIds.UserRegistered, user.UserName);
                if (user.UserName.ToLower() == "admin" || 0 == numberExistingUsers)
                {
                    result = await _userManager.AddToRoleAsync(user, nameof(Roles.Admin));
                    if (result.Succeeded)
                    {
                        _logger.LogInformation(EventIds.UserRoleAdded,
                            $"{nameof(Roles.Admin)} added to user {user.UserName} at registration");
                    }
                }
            }

            if (result.Succeeded)
            {
                // Send an email with this link
                var success = await _emailSender.SendConfirmationEmailAsync(user, Url, HttpContext);
                //await _signInManager.SignInAsync(user, false);
                //_logger.LogInformation(EventIds.UserLoggedIn, user.UserName);
                return RedirectToAction(nameof(SendConfirmationEmail), new { errorMessage = success ? "" : "Failed to send confirmation email", infoMessage = success ? "Sent confirmation email" : "" });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }
        #endregion Register

        #region Login
        [HttpGet, AllowAnonymous]
        public IActionResult Login(string returnUrl)
        {
            return View(new LoginViewModel {ReturnUrl = returnUrl});
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var loginResult = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, true);
                if (loginResult.Succeeded)
                {
                    _logger.LogInformation(EventIds.UserLoggedIn, model.UserName);

                    var user = await _userManager.FindByNameAsync(model.UserName);
                    if (!user.EmailConfirmed)
                    {
                        return RedirectToAction(nameof(SendConfirmationEmail));
                    }

                    if (Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    return RedirectToAction(nameof(TaskController.Index), "Task");
                }
                if (loginResult.RequiresTwoFactor)
                {
                    return RedirectToAction(nameof(SendCode), new { ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
                }
                if (loginResult.IsLockedOut)
                {
                    _logger.LogWarning(EventIds.UserLockedOut, model.UserName);
                    return View("LockedOut");
                }
                ModelState.AddModelError("", "Login Failed");
            }
            return View(model);
        }
        #endregion Login

        #region Send Confirmation Email

        [HttpGet]
        public async Task<IActionResult> SendConfirmationEmail(string errorMessage = null, string infoMessage = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user.EmailConfirmed)
            {
                RedirectToAction(nameof(TaskController.Index), "Task");
            }

            ViewBag.ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? null : errorMessage;
            ViewBag.InfoMessage = string.IsNullOrWhiteSpace(infoMessage) ? null : infoMessage;
            return View(nameof(SendConfirmationEmail), user.Email);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SendConfirmationEmail()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user.EmailConfirmed)
            {
                RedirectToAction(nameof(TaskController.Index), "Task");
            }

            var success = await _emailSender.SendConfirmationEmailAsync(user, Url, HttpContext);
            return RedirectToAction(nameof(SendConfirmationEmail), new {errorMessage = success ? "" : "Failed to send confirmation email", infoMessage = success ? "Sent confirmation email" : ""});
        }
        #endregion Send Confirmation Email

        #region Logout
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var name = _userManager.GetUserName(User);
            await _signInManager.SignOutAsync();
            _logger.LogInformation(EventIds.UserLoggedOut, name);
            return RedirectToAction(nameof(TaskController.Index), "Task");
        }
        #endregion

        #region Profile
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var roles = await _userManager.GetRolesAsync(user);
            return View(new ProfileViewModel
            {
                User = user,
                Roles = roles
            });
        }
        #endregion Profile

        #region Edit Profile
        [HttpGet]
        public async Task<IActionResult> Edit(string returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            if (!string.IsNullOrWhiteSpace(returnUrl) && !Url.IsLocalUrl(returnUrl))
            {
                returnUrl = null;
            }

            var model = new EditAccountViewModel
            {
                DisplayName = user.DisplayName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                UserName = user.UserName,
                ReturnUrl = returnUrl
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            
            var user = await _userManager.GetUserAsync(HttpContext.User);
            user.DisplayName = model.DisplayName;
            if (user.Email != model.Email)
            {
                user.EmailConfirmed = false;
                user.Email = model.Email;
            }
            //user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation(EventIds.UserUpdatedProfile, user.UserName);
                if (!user.EmailConfirmed)
                {
                    var success = await _emailSender.SendConfirmationEmailAsync(user, Url, HttpContext);
                    //await _signInManager.SignInAsync(user, false);
                    //_logger.LogInformation(EventIds.UserLoggedIn, user.UserName);
                    return RedirectToAction(nameof(SendConfirmationEmail), new { errorMessage = success ? "" : "Failed to send confirmation email", infoMessage = success ? "Sent confirmation email" : "" });
                }
                if(!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }
        #endregion Edit Profile

        #region External Login
        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }
        
        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return View(nameof(Login));
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (result.Succeeded)
            {
                _logger.LogInformation(5, $"User logged in with {info.LoginProvider} provider.");
                return RedirectToLocal(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl });
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning(EventIds.UserLockedOut, $"{info.LoginProvider} provider");
                return View("LockedOut");
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["LoginProvider"] = info.LoginProvider;
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new User { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation(6, "User created an account using {Name} provider.", info.LoginProvider);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }
        #endregion External Login

        #region Confirm Email
        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return View("Error");
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }
        #endregion Confirm Email

        #region Forgot Password
        [HttpGet, AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        
        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.UserName);// ?? await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=532713
                // Send an email with this link
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action(nameof(ResetPassword), "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
                await _emailSender.SendEmailAsync("Reset Password",
                   $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>",
                   EmailCategory.Confirm, user.Email);
                return View("ForgotPasswordConfirmation");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }
        
        [HttpGet, AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }
        #endregion Forgot Password

        #region Reset Password
        [HttpGet, AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            return code == null ? View("Error") : View();
        }
        
        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation), "Account");
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, false);
                return RedirectToAction(nameof(ResetPasswordConfirmation), "Account");
            }
            AddErrors(result);
            return View();
        }
        
        [HttpGet, AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
        #endregion Reset Password

        #region Send Code
        [HttpGet, AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl = null, bool rememberMe = false)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            var userFactors = await _userManager.GetValidTwoFactorProvidersAsync(user);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }

            // Generate the token and send it
            var code = await _userManager.GenerateTwoFactorTokenAsync(user, model.SelectedProvider);
            if (string.IsNullOrWhiteSpace(code))
            {
                return View("Error");
            }

            var message = "Your security code is: " + code;
            if (model.SelectedProvider == "Email")
            {
                await _emailSender.SendEmailAsync("Security Code", message, EmailCategory.Confirm, await _userManager.GetEmailAsync(user));
            }
            //else if (model.SelectedProvider == "Phone")
            //{
            //    await _smsSender.SendSmsAsync(await _userManager.GetPhoneNumberAsync(user), message);
            //}

            return RedirectToAction(nameof(VerifyCode), new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }
        #endregion Send Code

        #region Verify Code
        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> VerifyCode(string provider, bool rememberMe, string returnUrl = null)
        {
            // Require that the user has already logged in via username/password or external login
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }
        
        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes.
            // If a user enters incorrect codes for a specified amount of time then the user account
            // will be locked out for a specified amount of time.
            var result = await _signInManager.TwoFactorSignInAsync(model.Provider, model.Code, model.RememberMe, model.RememberBrowser);
            if (result.Succeeded)
            {
                return RedirectToLocal(model.ReturnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning(7, "User account locked out.");
                _logger.LogWarning(EventIds.UserLockedOut, $"{model.Provider} provider");
                return View("LockedOut");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid code.");
                return View(model);
            }
        }
        #endregion Verify Code

        #region Helpers
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(TaskController.Index), "Task");
            }
        }
        #endregion Helpers
    }
}
