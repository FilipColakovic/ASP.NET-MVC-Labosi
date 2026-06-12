using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vjezba.Model.Models;
using Vjezba.Model.Data;

namespace Vjezba.Model.Areas.Identity.Pages.Account.Manage
{
    [Authorize(Roles = IdentitySeed.AdminRole + "," + IdentitySeed.ManagerRole)]
    public class PersonalDataModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;

        public PersonalDataModel(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public Dictionary<string, string?> PersonalData { get; set; } = new();
        public IList<string> Roles { get; set; } = [];

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

        public async Task<IActionResult> OnPostDownloadAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return NotFound("Unable to load user.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var data = BuildPersonalData(user, roles);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            return File(bytes, "application/json", "personal-data.json");
        }

        private async Task LoadAsync(AppUser user)
        {
            Roles = await _userManager.GetRolesAsync(user);
            PersonalData = BuildPersonalData(user, Roles);
        }

        private static Dictionary<string, string?> BuildPersonalData(AppUser user, IEnumerable<string> roles)
        {
            return new Dictionary<string, string?>
            {
                ["Id"] = user.Id,
                ["UserName"] = user.UserName,
                ["Email"] = user.Email,
                ["PhoneNumber"] = user.PhoneNumber,
                ["OIB"] = user.OIB,
                ["JMBG"] = user.JMBG,
                ["Roles"] = string.Join(", ", roles),
                ["EmailConfirmed"] = user.EmailConfirmed.ToString(),
                ["TwoFactorEnabled"] = user.TwoFactorEnabled.ToString(),
                ["LockoutEnabled"] = user.LockoutEnabled.ToString(),
                ["LockoutEnd"] = user.LockoutEnd?.ToString("u")
            };
        }
    }
}

