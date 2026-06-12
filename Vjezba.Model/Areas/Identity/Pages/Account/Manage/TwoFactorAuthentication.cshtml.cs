using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vjezba.Model.Models;
using Vjezba.Model.Data;

namespace Vjezba.Model.Areas.Identity.Pages.Account.Manage
{
    [Authorize(Roles = IdentitySeed.AdminRole + "," + IdentitySeed.ManagerRole)]
    public class TwoFactorAuthenticationModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public TwoFactorAuthenticationModel(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public bool Is2faEnabled { get; set; }
        public bool IsMachineRemembered { get; set; }
        public int RecoveryCodesLeft { get; set; }

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

        public async Task<IActionResult> OnPostEnableAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return NotFound("Unable to load user.");
            }

            var result = await _userManager.SetTwoFactorEnabledAsync(user, true);
            StatusMessage = result.Succeeded
                ? "Two-factor authentication was enabled for this account."
                : "Two-factor authentication could not be enabled.";

            await _signInManager.RefreshSignInAsync(user);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDisableAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return NotFound("Unable to load user.");
            }

            var result = await _userManager.SetTwoFactorEnabledAsync(user, false);
            StatusMessage = result.Succeeded
                ? "Two-factor authentication was disabled for this account."
                : "Two-factor authentication could not be disabled.";

            await _signInManager.RefreshSignInAsync(user);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostForgetBrowserAsync()
        {
            await _signInManager.ForgetTwoFactorClientAsync();
            StatusMessage = "The current browser is no longer remembered for 2FA.";
            return RedirectToPage();
        }

        private async Task LoadAsync(AppUser user)
        {
            Is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            IsMachineRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user);
            RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);
        }
    }
}

