using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vjezba.Model.Data;
using Vjezba.Model.Models;

namespace Vjezba.Model.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private const string DefaultSelectedType = "package";

        private static readonly HashSet<string> OverviewTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "package",
            "courier",
            "warehouse",
            "user",
            "delivery"
        };

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("/")]
        [HttpGet("dashboard/{selectedType?}")]
        [HttpGet("hub/{selectedType?}")]
        public IActionResult Index(string? selectedType)
        {
            var normalized = NormalizeType(selectedType);
            if (!OverviewTypes.Contains(normalized))
            {
                normalized = DefaultSelectedType;
            }

            ViewData["SelectedType"] = normalized;
            return View(BuildOverviewModel());
        }

        [HttpGet("policy/privacy")]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet("objects/{type}/{id:int}")]
        [HttpGet("details/{type}/{id:int}")]
        public IActionResult Details(string type, int id)
        {
            var normalizedType = NormalizeType(type);
            var model = BuildDetailsModel(normalizedType, id);
            if (model is null)
            {
                return NotFound();
            }

            ViewData["SelectedType"] = MapSelectedTypeForSidebar(normalizedType);
            return View("Details", model);
        }

        private ObjectOverviewViewModel BuildOverviewModel()
        {
            return new ObjectOverviewViewModel
            {
                Addresses = _context.Addresses.AsNoTracking().OrderBy(x => x.Id).ToList(),
                Couriers = _context.Couriers.AsNoTracking().OrderBy(x => x.Id).ToList(),
                Users = _context.Users.AsNoTracking().OrderBy(x => x.Id).ToList(),
                Packages = LoadPackageGraph(_context.Packages.AsNoTracking()).OrderBy(x => x.Id).ToList(),
                Warehouses = _context.Warehouses.AsNoTracking()
                    .Include(x => x.Address)
                    .Include(x => x.StoredPackages)
                    .OrderBy(x => x.Id)
                    .ToList(),
                Deliveries = _context.Deliveries.AsNoTracking()
                    .Include(x => x.Courier)
                    .Include(x => x.Packages)
                    .OrderBy(x => x.Id)
                    .ToList()
            };
        }

        private IObjectDetailsPageViewModel? BuildDetailsModel(string normalizedType, int id)
        {
            switch (normalizedType)
            {
                case "package":
                    var package = LoadPackageDetailsGraph(_context.Packages.AsNoTracking()).FirstOrDefault(x => x.Id == id);
                    if (package is null)
                    {
                        return null;
                    }

                    return new PackageDetailsViewModel
                    {
                        Package = package,
                        StatusLogCount = package.StatusHistory.Count,
                        WarehouseCount = package.Warehouses.Count,
                        DeliveryCount = package.Deliveries.Count
                    };

                case "courier":
                    var courier = _context.Couriers.AsNoTracking().FirstOrDefault(x => x.Id == id);
                    if (courier is null)
                    {
                        return null;
                    }

                    return new CourierDetailsViewModel
                    {
                        Courier = courier,
                        PackageCount = _context.Packages.AsNoTracking().Count(x => x.CourierId == id),
                        DeliveryCount = _context.Deliveries.AsNoTracking().Count(x => x.CourierId == id)
                    };

                case "user":
                    var user = _context.Users.AsNoTracking().FirstOrDefault(x => x.Id == id);
                    if (user is null)
                    {
                        return null;
                    }

                    return new UserDetailsViewModel
                    {
                        User = user,
                        SentPackageCount = _context.Packages.AsNoTracking().Count(x => x.SenderUserId == id),
                        ReceivedPackageCount = _context.Packages.AsNoTracking().Count(x => x.RecipientUserId == id)
                    };

                case "warehouse":
                    var warehouse = _context.Warehouses.AsNoTracking()
                        .Include(x => x.Address)
                        .Include(x => x.StoredPackages)
                        .FirstOrDefault(x => x.Id == id);
                    if (warehouse is null)
                    {
                        return null;
                    }

                    return new WarehouseDetailsViewModel
                    {
                        Warehouse = warehouse,
                        StoredPackageCount = warehouse.StoredPackages.Count
                    };

                case "delivery":
                    var delivery = _context.Deliveries.AsNoTracking()
                        .Include(x => x.Courier)
                        .Include(x => x.Packages)
                        .FirstOrDefault(x => x.Id == id);
                    if (delivery is null)
                    {
                        return null;
                    }

                    return new DeliveryDetailsViewModel
                    {
                        Delivery = delivery,
                        PackageCount = delivery.Packages.Count
                    };

                case "address":
                    var address = _context.Addresses.AsNoTracking().FirstOrDefault(x => x.Id == id);
                    if (address is null)
                    {
                        return null;
                    }

                    return new AddressDetailsViewModel
                    {
                        Address = address,
                        SentPackageCount = _context.Packages.AsNoTracking().Count(x => x.SenderAddressId == id),
                        ReceivedPackageCount = _context.Packages.AsNoTracking().Count(x => x.RecipientAddressId == id),
                        WarehouseCount = _context.Warehouses.AsNoTracking().Count(x => x.AddressId == id)
                    };

                case "statuslog":
                    var statusLog = _context.StatusLogs.AsNoTracking()
                        .Include(x => x.Package)
                        .FirstOrDefault(x => x.Id == id);
                    if (statusLog is null)
                    {
                        return null;
                    }

                    return new StatusLogDetailsViewModel
                    {
                        StatusLog = statusLog
                    };

                default:
                    return null;
            }
        }

        private static IQueryable<Package> LoadPackageGraph(IQueryable<Package> query)
        {
            return query
                .Include(x => x.Courier)
                .Include(x => x.SenderUser)
                .Include(x => x.RecipientUser)
                .Include(x => x.SenderAddress)
                .Include(x => x.RecipientAddress)
                .Include(x => x.StatusHistory);
        }

        private static IQueryable<Package> LoadPackageDetailsGraph(IQueryable<Package> query)
        {
            return LoadPackageGraph(query)
                .Include(x => x.Warehouses)
                .Include(x => x.Deliveries);
        }

        private static string NormalizeType(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }

        private static string MapSelectedTypeForSidebar(string normalizedType)
        {
            if (OverviewTypes.Contains(normalizedType))
            {
                return normalizedType;
            }

            return normalizedType switch
            {
                "statuslog" => "delivery",
                "address" => "package",
                _ => DefaultSelectedType
            };
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpGet("errors/app")]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
