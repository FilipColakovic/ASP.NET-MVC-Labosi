using Microsoft.AspNetCore.Identity;
using Vjezba.Model.Models;

namespace Vjezba.Model.Data
{
    public static class IdentitySeed
    {
        public const string AdminRole = "Admin";
        public const string ManagerRole = "Manager";
        public const string BasicRole = "Basic";

        public static readonly string[] Roles = [AdminRole, ManagerRole, BasicRole];

        public static async Task SeedRolesAndUsersAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();

            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            await EnsureUserAsync(
                userManager,
                email: "admin@local.test",
                password: "Admin123!",
                role: "Admin",
                oib: "12345678901",
                jmbg: "1234567890123");

            await EnsureUserAsync(
                userManager,
                email: "manager@local.test",
                password: "Manager123!",
                role: "Manager",
                oib: "10987654321",
                jmbg: "1098765432109");
        }

        private static async Task EnsureUserAsync(
            UserManager<AppUser> userManager,
            string email,
            string password,
            string role,
            string oib,
            string jmbg)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    LockoutEnabled = true,
                    OIB = oib,
                    JMBG = jmbg
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Cannot create seed user '{email}': {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                var shouldUpdate = false;
                if (user.OIB != oib)
                {
                    user.OIB = oib;
                    shouldUpdate = true;
                }

                if (user.JMBG != jmbg)
                {
                    user.JMBG = jmbg;
                    shouldUpdate = true;
                }

                if (!user.LockoutEnabled)
                {
                    user.LockoutEnabled = true;
                    shouldUpdate = true;
                }

                if (shouldUpdate)
                {
                    var updateResult = await userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            $"Cannot update seed user '{email}': {string.Join("; ", updateResult.Errors.Select(e => e.Description))}");
                    }
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                var addToRoleResult = await userManager.AddToRoleAsync(user, role);
                if (!addToRoleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Cannot assign role '{role}' to '{email}': {string.Join("; ", addToRoleResult.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}
