using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vjezba.Model.Models;

namespace Vjezba.Model.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class ExternalLoginsModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public ExternalLoginsModel(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public IList<UserLoginInfo> CurrentLogins { get; set; } = [];
        public IList<AuthenticationScheme> OtherLogins { get; set; } = [];
        public bool ShowRemoveButton { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return NotFound("Unable to load user.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostRemoveLoginAsync(string loginProvider, string providerKey)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return NotFound("Unable to load user.");
            }

            await LoadAsync(user);
            if (!ShowRemoveButton)
            {
                StatusMessage = "You cannot remove the only sign-in method from this account.";
                return RedirectToPage();
            }

            var result = await _userManager.RemoveLoginAsync(user, loginProvider, providerKey);
            if (!result.Succeeded)
            {
                StatusMessage = "The external login could not be removed.";
                return RedirectToPage();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "The external login was removed.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostLinkLoginAsync(string provider)
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return NotFound("Unable to load user.");
            }

            var redirectUrl = Url.Page("./ExternalLogins", pageHandler: "LinkLoginCallback");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, await _userManager.GetUserIdAsync(user));
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetLinkLoginCallbackAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return NotFound("Unable to load user.");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync(await _userManager.GetUserIdAsync(user));
            if (info is null)
            {
                StatusMessage = "The external login could not be loaded.";
                return RedirectToPage();
            }

            var result = await _userManager.AddLoginAsync(user, info);
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            StatusMessage = result.Succeeded
                ? "The external login was added."
                : "The external login could not be added.";

            return RedirectToPage();
        }

        private async Task LoadAsync(AppUser user)
        {
            CurrentLogins = await _userManager.GetLoginsAsync(user);
            var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
            OtherLogins = schemes
                .Where(scheme => CurrentLogins.All(login => login.LoginProvider != scheme.Name))
                .ToList();

            var hasPassword = await _userManager.HasPasswordAsync(user);
            ShowRemoveButton = hasPassword || CurrentLogins.Count > 1;
        }
    }
}
