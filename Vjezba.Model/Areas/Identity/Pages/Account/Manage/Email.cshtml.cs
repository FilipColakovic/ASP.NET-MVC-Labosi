using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vjezba.Model.Models;

namespace Vjezba.Model.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class EmailModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public EmailModel(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Email { get; set; } = string.Empty;
        public bool IsEmailConfirmed { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "New email")]
            public string NewEmail { get; set; } = string.Empty;
        }

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

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return NotFound("Unable to load user.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var newEmail = Input.NewEmail.Trim();
            var email = await _userManager.GetEmailAsync(user) ?? string.Empty;
            if (string.Equals(newEmail, email, StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = "Email is unchanged.";
                return RedirectToPage();
            }

            var existingUser = await _userManager.FindByEmailAsync(newEmail);
            if (existingUser is not null && existingUser.Id != user.Id)
            {
                ModelState.AddModelError("Input.NewEmail", "A user with this email already exists.");
                await LoadAsync(user);
                Input.NewEmail = newEmail;
                return Page();
            }

            var setEmailResult = await _userManager.SetEmailAsync(user, newEmail);
            if (!setEmailResult.Succeeded)
            {
                AddErrors(setEmailResult);
                await LoadAsync(user);
                Input.NewEmail = newEmail;
                return Page();
            }

            var setUserNameResult = await _userManager.SetUserNameAsync(user, newEmail);
            if (!setUserNameResult.Succeeded)
            {
                AddErrors(setUserNameResult);
                await LoadAsync(user);
                Input.NewEmail = newEmail;
                return Page();
            }

            user.EmailConfirmed = true;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                AddErrors(updateResult);
                await LoadAsync(user);
                Input.NewEmail = newEmail;
                return Page();
            }

            await _userManager.UpdateSecurityStampAsync(user);
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Email updated. Use the new email for your next login.";
            return RedirectToPage();
        }

        private async Task LoadAsync(AppUser user)
        {
            Email = await _userManager.GetEmailAsync(user) ?? string.Empty;
            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
            Input = new InputModel
            {
                NewEmail = Email
            };
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
