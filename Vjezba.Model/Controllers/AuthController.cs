using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Vjezba.Model.Models;

namespace Vjezba.Model.Controllers
{
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        public AuthController(
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpPost("login-popup")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginPopup([FromForm] PopupLoginViewModel model)
        {
            var safeReturnUrl = ResolveReturnUrl(model.ReturnUrl);
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Please enter a valid email and password." });
            }

            var emailOrUserName = model.Email.Trim();
            var user = await _userManager.FindByEmailAsync(emailOrUserName)
                ?? await _userManager.FindByNameAsync(emailOrUserName);
            if (user is null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var signInResult = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (signInResult.Succeeded)
            {
                return Ok(new { redirectUrl = safeReturnUrl });
            }

            if (signInResult.RequiresTwoFactor)
            {
                return Unauthorized(new
                {
                    message = "This account requires two-factor login. Use the full login page."
                });
            }

            if (signInResult.IsLockedOut)
            {
                return Unauthorized(new { message = "Account is temporarily locked. Try again later." });
            }

            if (signInResult.IsNotAllowed)
            {
                return Unauthorized(new { message = "Sign-in is not allowed for this account." });
            }

            return Unauthorized(new { message = "Invalid email or password." });
        }

        private string ResolveReturnUrl(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return returnUrl;
            }

            return Url.Action("Index", "Home") ?? "/";
        }
    }
}
