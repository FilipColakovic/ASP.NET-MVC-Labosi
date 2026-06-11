using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vjezba.Model.Data;
using Vjezba.Model.Models;

namespace Vjezba.Model.Controllers
{
    [Authorize(Roles = IdentitySeed.AdminRole)]
    [Route("admin/users")]
    public class AdminUsersController : Controller
    {
        private static readonly DateTimeOffset DeactivatedUntil = DateTimeOffset.MaxValue;

        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminUsersController(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var model = await BuildViewModelAsync();
            model.StatusMessage = TempData["StatusMessage"] as string;
            model.ErrorMessage = TempData["ErrorMessage"] as string;

            return View(model);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([Bind(Prefix = "CreateUser")] AdminCreateUserViewModel createUser)
        {
            createUser.Email = createUser.Email.Trim();
            createUser.OIB = createUser.OIB.Trim();
            createUser.JMBG = createUser.JMBG.Trim();

            if (!IdentitySeed.Roles.Contains(createUser.Role))
            {
                ModelState.AddModelError(
                    $"{nameof(AdminUserManagementViewModel.CreateUser)}.{nameof(AdminCreateUserViewModel.Role)}",
                    "Unknown role selected.");
            }

            if (!string.IsNullOrWhiteSpace(createUser.Email)
                && await _userManager.FindByEmailAsync(createUser.Email) is not null)
            {
                ModelState.AddModelError(
                    $"{nameof(AdminUserManagementViewModel.CreateUser)}.{nameof(AdminCreateUserViewModel.Email)}",
                    "A user with this email already exists.");
            }

            if (!ModelState.IsValid)
            {
                return await IndexWithCreateErrorAsync(
                    createUser,
                    "Could not create the user. Check the highlighted fields.");
            }

            await EnsureManagedRolesExistAsync();

            var user = new AppUser
            {
                UserName = createUser.Email,
                Email = createUser.Email,
                EmailConfirmed = true,
                LockoutEnabled = true,
                LockoutEnd = createUser.IsActive ? null : DeactivatedUntil,
                OIB = createUser.OIB,
                JMBG = createUser.JMBG
            };

            var createResult = await _userManager.CreateAsync(user, createUser.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return await IndexWithCreateErrorAsync(
                    createUser,
                    "Could not create the user. Identity rejected the submitted data.");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, createUser.Role);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return await IndexWithCreateErrorAsync(
                    createUser,
                    "User was not created because the role could not be assigned.");
            }

            TempData["StatusMessage"] = $"{DisplayName(user)} was created as {createUser.Role}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("role")]
        public async Task<IActionResult> SetRole(string userId, string role)
        {
            if (!IdentitySeed.Roles.Contains(role))
            {
                TempData["ErrorMessage"] = "Unknown role selected.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                TempData["ErrorMessage"] = "User was not found.";
                return RedirectToAction(nameof(Index));
            }

            if (IsCurrentUser(user))
            {
                TempData["ErrorMessage"] = "You cannot change your own role from this panel.";
                return RedirectToAction(nameof(Index));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Contains(IdentitySeed.AdminRole) && role != IdentitySeed.AdminRole)
            {
                var activeAdminCount = await CountActiveAdminsAsync(excludingUserId: user.Id);
                if (activeAdminCount == 0)
                {
                    TempData["ErrorMessage"] = "At least one active Admin must remain.";
                    return RedirectToAction(nameof(Index));
                }
            }

            await EnsureManagedRolesExistAsync();

            var managedRoles = currentRoles
                .Where(IdentitySeed.Roles.Contains)
                .ToList();

            if (managedRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, managedRoles);
                if (!removeResult.Succeeded)
                {
                    TempData["ErrorMessage"] = BuildIdentityError("Could not remove previous roles", removeResult);
                    return RedirectToAction(nameof(Index));
                }
            }

            var addResult = await _userManager.AddToRoleAsync(user, role);
            if (!addResult.Succeeded)
            {
                TempData["ErrorMessage"] = BuildIdentityError("Could not assign role", addResult);
                return RedirectToAction(nameof(Index));
            }

            await _userManager.UpdateSecurityStampAsync(user);

            TempData["StatusMessage"] = $"{DisplayName(user)} is now {role}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("status")]
        public async Task<IActionResult> SetStatus(string userId, bool isActive)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                TempData["ErrorMessage"] = "User was not found.";
                return RedirectToAction(nameof(Index));
            }

            if (IsCurrentUser(user))
            {
                TempData["ErrorMessage"] = "You cannot deactivate your own account from this panel.";
                return RedirectToAction(nameof(Index));
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            if (!isActive && userRoles.Contains(IdentitySeed.AdminRole))
            {
                var activeAdminCount = await CountActiveAdminsAsync(excludingUserId: user.Id);
                if (activeAdminCount == 0)
                {
                    TempData["ErrorMessage"] = "At least one active Admin must remain.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var enableLockoutResult = await _userManager.SetLockoutEnabledAsync(user, true);
            if (!enableLockoutResult.Succeeded)
            {
                TempData["ErrorMessage"] = BuildIdentityError("Could not update lockout settings", enableLockoutResult);
                return RedirectToAction(nameof(Index));
            }

            var lockoutResult = await _userManager.SetLockoutEndDateAsync(
                user,
                isActive ? null : DeactivatedUntil);

            if (!lockoutResult.Succeeded)
            {
                TempData["ErrorMessage"] = BuildIdentityError("Could not update account status", lockoutResult);
                return RedirectToAction(nameof(Index));
            }

            await _userManager.UpdateSecurityStampAsync(user);

            TempData["StatusMessage"] = $"{DisplayName(user)} is now {(isActive ? "active" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> IndexWithCreateErrorAsync(
            AdminCreateUserViewModel createUser,
            string message)
        {
            var model = await BuildViewModelAsync();
            model.CreateUser = createUser;
            model.ErrorMessage = message;

            return View("Index", model);
        }

        private async Task<AdminUserManagementViewModel> BuildViewModelAsync()
        {
            var currentUserId = _userManager.GetUserId(User);
            var users = await _userManager.Users
                .OrderBy(x => x.Email)
                .ToListAsync();

            var rows = new List<AdminUserRowViewModel>();
            foreach (var user in users)
            {
                var roles = (await _userManager.GetRolesAsync(user))
                    .OrderBy(x => x)
                    .ToList();

                rows.Add(new AdminUserRowViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    OIB = user.OIB,
                    JMBG = user.JMBG,
                    IsActive = IsActive(user),
                    LockoutEnd = user.LockoutEnd,
                    IsCurrentUser = user.Id == currentUserId,
                    Roles = roles,
                    PrimaryRole = PickPrimaryRole(roles)
                });
            }

            return new AdminUserManagementViewModel
            {
                Users = rows,
                AvailableRoles = IdentitySeed.Roles.ToList()
            };
        }

        private async Task EnsureManagedRolesExistAsync()
        {
            foreach (var requiredRole in IdentitySeed.Roles)
            {
                if (!await _roleManager.RoleExistsAsync(requiredRole))
                {
                    await _roleManager.CreateAsync(new IdentityRole(requiredRole));
                }
            }
        }

        private async Task<int> CountActiveAdminsAsync(string? excludingUserId = null)
        {
            var admins = await _userManager.GetUsersInRoleAsync(IdentitySeed.AdminRole);
            return admins.Count(user =>
                user.Id != excludingUserId
                && IsActive(user));
        }

        private bool IsCurrentUser(AppUser user)
        {
            return user.Id == _userManager.GetUserId(User);
        }

        private static bool IsActive(AppUser user)
        {
            return !user.LockoutEnd.HasValue || user.LockoutEnd.Value <= DateTimeOffset.UtcNow;
        }

        private static string PickPrimaryRole(IReadOnlyCollection<string> roles)
        {
            if (roles.Contains(IdentitySeed.AdminRole))
            {
                return IdentitySeed.AdminRole;
            }

            if (roles.Contains(IdentitySeed.ManagerRole))
            {
                return IdentitySeed.ManagerRole;
            }

            return IdentitySeed.BasicRole;
        }

        private static string DisplayName(AppUser user)
        {
            return user.Email ?? user.UserName ?? "User";
        }

        private static string BuildIdentityError(string prefix, IdentityResult result)
        {
            return $"{prefix}: {string.Join("; ", result.Errors.Select(x => x.Description))}";
        }
    }
}
