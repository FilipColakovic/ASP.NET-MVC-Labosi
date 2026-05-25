using Microsoft.EntityFrameworkCore;
using Vjezba.Model.Enums;
using Vjezba.Model.Models;

namespace Vjezba.Model.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Address> Addresses => Set<Address>();
        public DbSet<Courier> Couriers => Set<Courier>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Package> Packages => Set<Package>();
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<Delivery> Deliveries => Set<Delivery>();
        public DbSet<StatusLog> StatusLogs => Set<StatusLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Courier>(entity =>
            {
                entity.HasIndex(x => x.Email).IsUnique();
                entity.HasIndex(x => x.LicensePlate).IsUnique();
                entity.HasQueryFilter(x => x.DeletedAt == null);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(x => x.Email).IsUnique();
                entity.HasQueryFilter(x => x.DeletedAt == null);
            });

            modelBuilder.Entity<Address>(entity =>
            {
                entity.HasQueryFilter(x => x.DeletedAt == null);
            });

            modelBuilder.Entity<Package>(entity =>
            {
                entity.HasIndex(x => x.TrackingNumber).IsUnique();
                entity.HasQueryFilter(x => x.DeletedAt == null
                    && x.Courier.DeletedAt == null
                    && x.SenderUser.DeletedAt == null
                    && x.RecipientUser.DeletedAt == null
                    && x.SenderAddress.DeletedAt == null
                    && x.RecipientAddress.DeletedAt == null);
                entity.HasOne(x => x.Courier)
                    .WithMany(x => x.Packages)
                    .HasForeignKey(x => x.CourierId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.SenderUser)
                    .WithMany(x => x.SentPackages)
                    .HasForeignKey(x => x.SenderUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.RecipientUser)
                    .WithMany(x => x.ReceivedPackages)
                    .HasForeignKey(x => x.RecipientUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.SenderAddress)
                    .WithMany(x => x.SentPackages)
                    .HasForeignKey(x => x.SenderAddressId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.RecipientAddress)
                    .WithMany(x => x.ReceivedPackages)
                    .HasForeignKey(x => x.RecipientAddressId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(x => x.StatusHistory)
                    .WithOne(x => x.Package)
                    .HasForeignKey(x => x.PackageId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(x => x.Warehouses)
                    .WithMany(x => x.StoredPackages);
                entity.HasMany(x => x.Deliveries)
                    .WithMany(x => x.Packages);
            });

            modelBuilder.Entity<Warehouse>(entity =>
            {
                entity.HasQueryFilter(x => x.DeletedAt == null && x.Address.DeletedAt == null);
                entity.HasOne(x => x.Address)
                    .WithMany(x => x.Warehouses)
                    .HasForeignKey(x => x.AddressId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(x => x.StoredPackages)
                    .WithMany(x => x.Warehouses);
            });

            modelBuilder.Entity<Delivery>(entity =>
            {
                entity.HasQueryFilter(x => x.DeletedAt == null && x.Courier.DeletedAt == null);
                entity.HasOne(x => x.Courier)
                    .WithMany(x => x.Deliveries)
                    .HasForeignKey(x => x.CourierId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(x => x.Packages)
                    .WithMany(x => x.Deliveries);
            });

            modelBuilder.Entity<StatusLog>(entity =>
            {
                entity.HasQueryFilter(x => x.DeletedAt == null && x.Package.DeletedAt == null);
                entity.HasOne(x => x.Package)
                    .WithMany(x => x.StatusHistory)
                    .HasForeignKey(x => x.PackageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        public static async Task SeedAsync(AppDbContext context)
        {
            if (!await context.Couriers.AnyAsync())
            {
                var seedData = SeedDataFactory.Create();

                context.AddRange(seedData.Couriers);
                context.AddRange(seedData.Users);
                context.AddRange(seedData.Addresses);
                context.AddRange(seedData.Packages);
                context.AddRange(seedData.Warehouses);
                context.AddRange(seedData.Deliveries);

                await context.SaveChangesAsync();
            }

            await EnsureAnalyticsDemoDataAsync(context);
        }

        private static async Task EnsureAnalyticsDemoDataAsync(AppDbContext context)
        {
            var couriers = await context.Couriers.OrderBy(x => x.Id).ToListAsync();
            var users = await context.Users.OrderBy(x => x.Id).ToListAsync();
            var addresses = await context.Addresses.OrderBy(x => x.Id).ToListAsync();
            var warehouses = await context.Warehouses
                .Include(x => x.StoredPackages)
                .OrderBy(x => x.Id)
                .ToListAsync();

            if (!couriers.Any() || users.Count < 2 || addresses.Count < 2)
            {
                return;
            }

            var nowUtc = DateTime.UtcNow;
            var recentDayCount = await context.Packages.CountAsync(p => p.CreatedAt >= nowUtc.AddHours(-24));
            var recentWeekCount = await context.Packages.CountAsync(p => p.CreatedAt >= nowUtc.AddDays(-7));

            const int targetDailyPackages = 18;
            const int targetWeeklyPackages = 34;
            var neededForDay = Math.Max(0, targetDailyPackages - recentDayCount);
            var neededForWeek = Math.Max(0, targetWeeklyPackages - recentWeekCount);
            var totalToAdd = Math.Max(neededForDay, neededForWeek);

            var newPackages = new List<Package>();
            if (totalToAdd > 0)
            {
                var existingTrackingNumbers = new HashSet<string>(
                    await context.Packages.Select(p => p.TrackingNumber).ToListAsync(),
                    StringComparer.OrdinalIgnoreCase);

                var statuses = new[]
                {
                    PackageStatus.InTransit,
                    PackageStatus.OutForDelivery,
                    PackageStatus.Delivered,
                    PackageStatus.PendingPickup,
                    PackageStatus.PickedUp,
                    PackageStatus.InTransit,
                    PackageStatus.Created,
                    PackageStatus.DeliveryFailed
                };
                var priorities = new[]
                {
                    DeliveryPriority.Urgent,
                    DeliveryPriority.High,
                    DeliveryPriority.Normal,
                    DeliveryPriority.Low
                };
                var descriptions = new[]
                {
                    "Demo shipment: electronics",
                    "Demo shipment: documents",
                    "Demo shipment: spare parts",
                    "Demo shipment: medical supplies",
                    "Demo shipment: home goods"
                };

                for (var index = 0; index < totalToAdd; index++)
                {
                    var trackingNumber = BuildUniqueDemoTrackingNumber(existingTrackingNumbers, nowUtc, index + 1);
                    var createdAt = index < neededForDay
                        ? nowUtc.AddHours(-(index + 1)).AddMinutes(-((index * 7) % 50))
                        : nowUtc.AddHours(-(24 + ((index - neededForDay) * 4) % 120))
                            .AddMinutes(-((index * 11) % 50));

                    var status = statuses[index % statuses.Length];
                    var sender = users[index % users.Count];
                    var recipient = users[(index + 1) % users.Count];
                    var senderAddress = addresses[index % addresses.Count];
                    var recipientAddress = addresses[(index + 2) % addresses.Count];
                    var package = new Package
                    {
                        TrackingNumber = trackingNumber,
                        WeightKg = 1.2m + (index % 8) * 0.7m,
                        DeliveryPriority = priorities[index % priorities.Length],
                        CourierId = couriers[index % couriers.Count].Id,
                        SenderUserId = sender.Id,
                        RecipientUserId = recipient.Id,
                        SenderAddressId = senderAddress.Id,
                        RecipientAddressId = recipientAddress.Id,
                        Status = status,
                        Description = descriptions[index % descriptions.Length],
                        CreatedAt = createdAt,
                        DeliveredAt = status == PackageStatus.Delivered ? createdAt.AddHours(12) : null
                    };

                    package.StatusHistory.Add(new StatusLog
                    {
                        TimeChanged = createdAt,
                        Location = senderAddress.City,
                        Description = "Package created in analytics demo stream",
                        PreviousStatus = PackageStatus.Created,
                        NewStatus = PackageStatus.PendingPickup
                    });

                    package.StatusHistory.Add(new StatusLog
                    {
                        TimeChanged = createdAt.AddHours(1),
                        Location = recipientAddress.City,
                        Description = "Package status updated in analytics demo stream",
                        PreviousStatus = PackageStatus.PendingPickup,
                        NewStatus = status
                    });

                    newPackages.Add(package);
                    context.Packages.Add(package);
                }

                await context.SaveChangesAsync();

                if (warehouses.Any())
                {
                    for (var index = 0; index < newPackages.Count; index++)
                    {
                        var warehouse = warehouses[index % warehouses.Count];
                        warehouse.StoredPackages.Add(newPackages[index]);
                    }

                    await context.SaveChangesAsync();
                }
            }

            var existingTimedDeliveries = await context.Deliveries.CountAsync(d => d.ArrivalDate > d.DepartureDate);
            if (existingTimedDeliveries > 0)
            {
                return;
            }

            var deliveryPackages = newPackages.Any()
                ? newPackages
                : await context.Packages
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(16)
                    .ToListAsync();

            if (!deliveryPackages.Any())
            {
                return;
            }

            var locations = new[] { "Zagreb Hub", "Rijeka Link", "Split Relay", "Osijek Belt" };
            for (var index = 0; index < 4; index++)
            {
                var slice = deliveryPackages
                    .Where((_, packageIndex) => packageIndex % 4 == index)
                    .Take(4)
                    .ToList();
                if (!slice.Any())
                {
                    continue;
                }

                var departureDate = nowUtc.AddHours(-(18 - (index * 2)));
                var arrivalDate = departureDate.AddHours(8 + (index * 2));
                var delivery = new Delivery
                {
                    DepartureDate = departureDate,
                    ArrivalDate = arrivalDate,
                    CurrentLocation = locations[index % locations.Length],
                    IsDelayed = index == 2,
                    CourierId = slice[0].CourierId,
                    Packages = slice
                };

                context.Deliveries.Add(delivery);
            }

            await context.SaveChangesAsync();
        }

        private static string BuildUniqueDemoTrackingNumber(
            HashSet<string> existingTrackingNumbers,
            DateTime nowUtc,
            int serialSeed)
        {
            var serial = serialSeed;
            while (true)
            {
                var trackingNumber = $"ANLDB-{nowUtc:yyMMdd}-{serial:000}";
                if (existingTrackingNumbers.Add(trackingNumber))
                {
                    return trackingNumber;
                }

                serial++;
            }
        }
    }
}
