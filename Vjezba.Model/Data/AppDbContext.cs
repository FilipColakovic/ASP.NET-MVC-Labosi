using Microsoft.EntityFrameworkCore;
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
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(x => x.Email).IsUnique();
            });

            modelBuilder.Entity<Package>(entity =>
            {
                entity.HasIndex(x => x.TrackingNumber).IsUnique();
                entity.HasOne(x => x.Courier)
                    .WithMany()
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.SenderUser)
                    .WithMany()
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.RecipientUser)
                    .WithMany()
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.SenderAddress)
                    .WithMany()
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.RecipientAddress)
                    .WithMany()
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(x => x.StatusHistory)
                    .WithOne(x => x.Package)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Warehouse>(entity =>
            {
                entity.HasOne(x => x.Address)
                    .WithMany()
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(x => x.StoredPackages)
                    .WithMany();
            });

            modelBuilder.Entity<Delivery>(entity =>
            {
                entity.HasOne(x => x.Courier)
                    .WithMany()
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(x => x.Packages)
                    .WithMany();
            });

            modelBuilder.Entity<StatusLog>(entity =>
            {
                entity.HasOne(x => x.Package)
                    .WithMany(x => x.StatusHistory)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        public static async Task SeedAsync(AppDbContext context)
        {
            if (await context.Couriers.AnyAsync())
            {
                return;
            }

            var seedData = SeedDataFactory.Create();

            context.AddRange(seedData.Couriers);
            context.AddRange(seedData.Users);
            context.AddRange(seedData.Addresses);
            context.AddRange(seedData.Packages);
            context.AddRange(seedData.Warehouses);
            context.AddRange(seedData.Deliveries);

            await context.SaveChangesAsync();
        }
    }
}