using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Vjezba.Model.Data;
using Vjezba.Model.Enums;
using Vjezba.Model.Models;

var builder = WebApplication.CreateBuilder(args);
var dbDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dbDirectory);
var dbPath = Path.Combine(dbDirectory, "vjezba.db");
var configuredConnection = builder.Configuration.GetConnectionString("DefaultConnection");
var sqliteConnection = string.IsNullOrWhiteSpace(configuredConnection)
    ? $"Data Source={dbPath}"
    : configuredConnection;

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(sqliteConnection));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await AppDbContext.SeedAsync(dbContext);

    var urgentOpen = dbContext.Packages
        .AsNoTracking()
        .Where(p => p.DeliveryPriority == DeliveryPriority.Urgent &&
                    p.Status != PackageStatus.Delivered)
        .ToList();

    var statusCounts = dbContext.Packages
        .AsNoTracking()
        .GroupBy(p => p.Status)
        .Select(g => new { Status = g.Key, Count = g.Count() })
        .OrderByDescending(x => x.Count)
        .ToList();

    var warehouseUtilization = dbContext.Warehouses
        .AsNoTracking()
        .Select(w => new
        {
            w.Name,
            w.Capacity,
            Stored = w.StoredPackages.Count,
            UtilizationPercent = w.Capacity == 0 ? 0m : (decimal)w.StoredPackages.Count / w.Capacity * 100m
        })
        .OrderByDescending(x => x.UtilizationPercent)
        .ToList();

    var delayedDeliveries = dbContext.Deliveries
        .AsNoTracking()
        .Where(d => d.IsDelayed)
        .Select(d => new
        {
            d.Id,
            Courier = d.Courier.FirstName + " " + d.Courier.LastName,
            d.CurrentLocation,
            PackageCount = d.Packages.Count
        })
        .ToList();

    var topSenders = dbContext.Packages
        .AsNoTracking()
        .GroupBy(p => p.SenderUser)
        .Select(g => new
        {
            Sender = g.Key.FirstName + " " + g.Key.LastName,
            SentCount = g.Count()
        })
        .OrderByDescending(x => x.SentCount)
        .ToList();

    Console.WriteLine($"Urgent open packages: {urgentOpen.Count}");
    Console.WriteLine($"Package statuses tracked: {statusCounts.Count}");
    Console.WriteLine($"Warehouse utilization rows: {warehouseUtilization.Count}");
    Console.WriteLine($"Delayed deliveries: {delayedDeliveries.Count}");
    Console.WriteLine($"Top senders tracked: {topSenders.Count}");

    async Task<string> GetPackageCheckMessageAsync()
    {
        await Task.Delay(200);
        return $"Async check complete. Total packages: {dbContext.Packages.Count()}";
    }

    var asyncMessage = await GetPackageCheckMessageAsync();
    Console.WriteLine(asyncMessage);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

var supportedCultures = new[]
{
    new CultureInfo("hr"),
    new CultureInfo("en-US")
};

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("hr"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
