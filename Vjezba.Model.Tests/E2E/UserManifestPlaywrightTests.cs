using Microsoft.Playwright;
using Vjezba.Model.Tests.Infrastructure;

namespace Vjezba.Model.Tests.E2E
{
    public sealed class UserManifestPlaywrightTests : IClassFixture<PlaywrightWebAppFixture>
    {
        private readonly PlaywrightWebAppFixture _app;

        public UserManifestPlaywrightTests(PlaywrightWebAppFixture app)
        {
            _app = app;
        }

        [Fact]
        [Trait("Category", "Playwright")]
        public async Task Admin_can_complete_user_manifest_crud_flow()
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await LaunchBrowserAsync(playwright);
            await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                BaseURL = _app.BaseUrl,
                ViewportSize = new ViewportSize
                {
                    Width = 1440,
                    Height = 900
                }
            });

            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(15_000);

            var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var firstName = $"Playwright{unique % 100000}";
            var lastName = "Tester";
            var email = $"playwright.{unique}@local.test";
            var phone = $"+385 91 {unique % 1000000:000000}";
            var updatedPhone = $"+385 92 {unique % 1000000:000000}";

            // 1-2. Open login and authenticate as seeded admin.
            await page.GotoAsync("/Identity/Account/Login?returnUrl=%2Fmanifest%2Fuser");
            await page.Locator("input[name='Input.Email']").FillAsync("admin@local.test");
            await page.Locator("input[name='Input.Password']").FillAsync("Admin123!");
            await page.Locator("button[type='submit']").ClickAsync();
            await page.WaitForURLAsync("**/manifest/user");

            // 3-6. Create a new user through the manifest UI and verify the row appears.
            await Assertions.Expect(page.Locator("[data-create-open='user']")).ToBeVisibleAsync();
            await page.Locator("[data-create-open='user']").ClickAsync();

            var createModal = page.Locator("[data-create-modal='user']");
            await Assertions.Expect(createModal).ToBeVisibleAsync();
            await createModal.Locator("input[name='FirstName']").FillAsync(firstName);
            await createModal.Locator("input[name='LastName']").FillAsync(lastName);
            await createModal.Locator("input[name='Email']").FillAsync(email);
            await createModal.Locator("input[name='PhoneNumber']").FillAsync(phone);
            await createModal.Locator("button[type='submit']").ClickAsync();

            var createdRow = page.Locator($"tr[data-row-id]:has-text(\"{email}\")");
            await Assertions.Expect(createdRow).ToBeVisibleAsync();

            // 7. Search for the newly created user.
            await page.Locator("#analog-manifest-search").FillAsync(email);
            await Assertions.Expect(createdRow).ToBeVisibleAsync();

            // 8. Open details and return to the manifest.
            await createdRow.Locator("a").First.ClickAsync();
            await Assertions.Expect(page.GetByText(email)).ToBeVisibleAsync();
            await page.Locator("a:has-text('Back')").ClickAsync();
            await page.WaitForURLAsync("**/manifest/user");

            // 9. Edit the same user and verify the changed value.
            createdRow = page.Locator($"tr[data-row-id]:has-text(\"{email}\")");
            await createdRow.Locator("[data-edit-open='user']").ClickAsync();

            var editModal = page.Locator("[data-edit-modal='user']");
            await Assertions.Expect(editModal).ToBeVisibleAsync();
            await editModal.Locator("input[name='PhoneNumber']").FillAsync(updatedPhone);
            await editModal.Locator("button[type='submit']").ClickAsync();

            var updatedRow = page.Locator($"tr[data-row-id]:has-text(\"{updatedPhone}\")");
            await Assertions.Expect(updatedRow).ToBeVisibleAsync();

            // 10. Delete the user and verify search no longer returns it.
            await updatedRow.Locator("form button[type='submit']").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await page.Locator("#analog-manifest-search").FillAsync(email);

            await Assertions.Expect(page.Locator($"tr[data-row-id]:has-text(\"{email}\")")).ToHaveCountAsync(0);
            await Assertions.Expect(page.Locator("#manifest-search-empty")).ToBeVisibleAsync();
        }

        private static async Task<IBrowser> LaunchBrowserAsync(IPlaywright playwright)
        {
            try
            {
                return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Channel = "msedge",
                    Headless = true
                });
            }
            catch (PlaywrightException)
            {
                return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true
                });
            }
        }
    }
}
