using Vjezba.Model.Data;
using Vjezba.Model.Enums;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

var seedData = SeedDataFactory.Create();

var urgentOpen = seedData.Packages
    .Where(p => p.DeliveryPriority == DeliveryPriority.Urgent &&
                p.Status != PackageStatus.Delivered)
    .ToList();

var statusCounts = seedData.Packages
    .GroupBy(p => p.Status)
    .Select(g => new { Status = g.Key, Count = g.Count() })
    .OrderByDescending(x => x.Count)
    .ToList();

var warehouseUtilization = seedData.Warehouses
    .Select(w => new
    {
        w.Name,
        w.Capacity,
        Stored = w.StoredPackages.Count,
        UtilizationPercent = w.Capacity == 0 ? 0m : (decimal)w.StoredPackages.Count / w.Capacity * 100m
    })
    .OrderByDescending(x => x.UtilizationPercent)
    .ToList();

var delayedDeliveries = seedData.Deliveries
    .Where(d => d.IsDelayed)
    .Select(d => new
    {
        d.Id,
        Courier = d.Courier.FirstName + " " + d.Courier.LastName,
        d.CurrentLocation,
        PackageCount = d.Packages.Count
    })
    .ToList();

var topSenders = seedData.Packages
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
    return $"Async check complete. Total packages: {seedData.Packages.Count}";
}

var asyncMessage = await GetPackageCheckMessageAsync();
Console.WriteLine(asyncMessage);

app.Run();
