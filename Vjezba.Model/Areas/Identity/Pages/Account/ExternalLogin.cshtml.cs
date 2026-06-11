using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vjezba.Model.Data;
using Vjezba.Model.Models;

namespace Vjezba.Model.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        public ExternalLoginModel(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string ProviderDisplayName { get; set; } = "External provider";

        public string ReturnUrl { get; set; } = "/";

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [RegularExpression(@"^\d{11}$", ErrorMessage = "OIB must contain exactly 11 digits.")]
            public string OIB { get; set; } = string.Empty;

            [Required]
            [RegularExpression(@"^\d{13}$", ErrorMessage = "JMBG must contain exactly 13 digits.")]
            public string JMBG { get; set; } = string.Empty;
        }

        public IActionResult OnGet()
        {
            return RedirectToPage("./Login");
        }

        public IActionResult OnPost(string provider, string? returnUrl = null)
        {
            var safeReturnUrl = ResolveReturnUrl(returnUrl);
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl = safeReturnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
        {
            ReturnUrl = ResolveReturnUrl(returnUrl);

            if (!string.IsNullOrWhiteSpace(remoteError))
            {
                return RedirectToPage("./Login", new { ReturnUrl, errorMessage = remoteError });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info is null)
            {
                return RedirectToPage("./Login", new { ReturnUrl });
            }

            ProviderDisplayName = info.ProviderDisplayName ?? info.LoginProvider;

            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: true);

            if (result.Succeeded)
            {
                return LocalRedirect(ReturnUrl);
            }

            if (result.IsLockedOut)
            {
                return RedirectToPage("./Login", new { ReturnUrl });
            }

            Input = new InputModel
            {
                Email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty
            };

            return Page();
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string? returnUrl = null)
        {
            ReturnUrl = ResolveReturnUrl(returnUrl);
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info is null)
            {
                return RedirectToPage("./Login", new { ReturnUrl });
            }

            ProviderDisplayName = info.ProviderDisplayName ?? info.LoginProvider;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = new AppUser
            {
                UserName = Input.Email.Trim(),
                Email = Input.Email.Trim(),
                LockoutEnabled = true,
                OIB = Input.OIB.Trim(),
                JMBG = Input.JMBG.Trim()
            };

            var createResult = await _userManager.CreateAsync(user);
            if (createResult.Succeeded)
            {
                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (addLoginResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, IdentitySeed.BasicRole);
                    await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                    return LocalRedirect(ReturnUrl);
                }

                foreach (var error in addLoginResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            else
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }

        private string ResolveReturnUrl(string? returnUrl)
        {
            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : Url.Content("~/");
        }
    }
}
