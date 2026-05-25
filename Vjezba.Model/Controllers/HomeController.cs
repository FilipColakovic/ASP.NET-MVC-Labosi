using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
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
            "delivery",
            "address",
            "statuslog"
        };

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("/")]
        [HttpGet("track")]
        [AllowAnonymous]
        public IActionResult Index(string? trackingNumber)
        {
            ViewData["SelectedType"] = "tracking";
            return View(BuildTrackingPageModel(trackingNumber));
        }

        [HttpGet("manifest/{selectedType?}")]
        [HttpGet("dashboard/{selectedType?}")]
        [HttpGet("hub/{selectedType?}")]
        [AllowAnonymous]
        public IActionResult Manifest(string? selectedType)
        {
            var normalized = NormalizeType(selectedType);
            if (!OverviewTypes.Contains(normalized))
            {
                normalized = DefaultSelectedType;
            }

            ViewData["SelectedType"] = normalized;
            return View("Manifest", BuildOverviewModel());
        }

        [HttpGet("policy/privacy")]
        [AllowAnonymous]
        public IActionResult Privacy()
        {
            ViewData["SelectedType"] = "privacy";
            return View();
        }

        [HttpGet("analytics")]
        [HttpGet("reports/analytics")]
        [AllowAnonymous]
        public IActionResult Analytics()
        {
            ViewData["SelectedType"] = "analytics";
            return View(BuildOverviewModel());
        }

        [HttpGet("objects/{type}/{id:int}")]
        [HttpGet("details/{type}/{id:int}")]
        [Authorize]
        public IActionResult Details(string type, int id)
        {
            var normalizedType = NormalizeType(type);
            var model = BuildDetailsModel(normalizedType, id);
            if (model is null)
            {
                return NotFound();
            }

            ViewData["SelectedType"] = MapSelectedTypeForSidebar(normalizedType);
            ViewData["Overview"] = BuildOverviewModel();
            return View("Details", model);
        }

        [HttpGet("manifest/search")]
        [AllowAnonymous]
        public IActionResult ManifestSearch(string selectedType, string? q)
        {
            var normalizedType = NormalizeType(selectedType);
            var term = (q ?? string.Empty).Trim();

            var ids = normalizedType switch
            {
                "package" => SearchPackageIds(term),
                "courier" => SearchCourierIds(term),
                "warehouse" => SearchWarehouseIds(term),
                "user" => SearchUserIds(term),
                "delivery" => SearchDeliveryIds(term),
                "address" => SearchAddressIds(term),
                "statuslog" => SearchStatusLogIds(term),
                _ => new List<int>()
            };

            return Json(new { ids });
        }

        [HttpGet("autocomplete/{source}")]
        [AllowAnonymous]
        public IActionResult Autocomplete(string source, string? q, int take = 20)
        {
            var normalizedSource = NormalizeType(source);
            var term = (q ?? string.Empty).Trim();
            var cappedTake = Math.Clamp(take, 1, 50);

            var items = normalizedSource switch
            {
                "couriers" => SearchAutocompleteCouriers(term, cappedTake),
                "users" => SearchAutocompleteUsers(term, cappedTake),
                "addresses" => SearchAutocompleteAddresses(term, cappedTake),
                "packages" => SearchAutocompletePackages(term, cappedTake),
                _ => new List<object>()
            };

            return Json(items);
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
                    .ToList(),
                StatusLogs = _context.StatusLogs.AsNoTracking()
                    .Include(x => x.Package)
                    .OrderByDescending(x => x.TimeChanged)
                    .ToList()
            };
        }

        private PackageTrackingPageViewModel BuildTrackingPageModel(string? trackingNumber)
        {
            var normalizedTrackingNumber = (trackingNumber ?? string.Empty).Trim().ToUpperInvariant();
            var model = new PackageTrackingPageViewModel
            {
                TrackingNumber = normalizedTrackingNumber
            };

            if (string.IsNullOrWhiteSpace(normalizedTrackingNumber))
            {
                return model;
            }

            var package = LoadPackageDetailsGraph(_context.Packages.AsNoTracking())
                .FirstOrDefault(x => EF.Functions.Like(x.TrackingNumber, normalizedTrackingNumber));

            model.LookupAttempted = true;
            if (package is null)
            {
                model.ErrorMessage = $"No package found for tracking number '{normalizedTrackingNumber}'.";
                return model;
            }

            model.Package = package;
            model.StatusHistory = package.StatusHistory
                .OrderByDescending(x => x.TimeChanged)
                .ToList();
            return model;
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

        private List<int> SearchPackageIds(string term)
        {
            var query = _context.Packages.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(term))
            {
                var pattern = $"%{term}%";
                query = query.Where(x =>
                    EF.Functions.Like(x.TrackingNumber, pattern) ||
                    EF.Functions.Like(x.Description, pattern) ||
                    EF.Functions.Like(x.RecipientAddress.City, pattern) ||
                    EF.Functions.Like(x.RecipientAddress.Country, pattern));
            }

            return query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.Id)
                .ToList();
        }

        private List<int> SearchCourierIds(string term)
        {
            var query = _context.Couriers.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(term))
            {
                var pattern = $"%{term}%";
                query = query.Where(x =>
                    EF.Functions.Like(x.FirstName, pattern) ||
                    EF.Functions.Like(x.LastName, pattern) ||
                    EF.Functions.Like(x.Email, pattern) ||
                    EF.Functions.Like(x.PhoneNumber, pattern) ||
                    EF.Functions.Like(x.VehicleType, pattern) ||
                    EF.Functions.Like(x.LicensePlate, pattern));
            }

            return query
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .Select(x => x.Id)
                .ToList();
        }

        private List<int> SearchWarehouseIds(string term)
        {
            var query = _context.Warehouses.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(term))
            {
                var pattern = $"%{term}%";
                query = query.Where(x =>
                    EF.Functions.Like(x.Name, pattern) ||
                    EF.Functions.Like(x.Address.City, pattern) ||
                    EF.Functions.Like(x.Address.Street, pattern));
            }

            return query
                .OrderBy(x => x.Name)
                .Select(x => x.Id)
                .ToList();
        }

        private List<int> SearchUserIds(string term)
        {
            var query = _context.Users.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(term))
            {
                var pattern = $"%{term}%";
                query = query.Where(x =>
                    EF.Functions.Like(x.FirstName, pattern) ||
                    EF.Functions.Like(x.LastName, pattern) ||
                    EF.Functions.Like(x.Email, pattern) ||
                    EF.Functions.Like(x.PhoneNumber, pattern));
            }

            return query
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .Select(x => x.Id)
                .ToList();
        }

        private List<int> SearchDeliveryIds(string term)
        {
            var query = _context.Deliveries.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(term))
            {
                var pattern = $"%{term}%";
                query = query.Where(x =>
                    EF.Functions.Like(x.CurrentLocation, pattern) ||
                    EF.Functions.Like(x.Courier.FirstName, pattern) ||
                    EF.Functions.Like(x.Courier.LastName, pattern));
            }

            return query
                .OrderByDescending(x => x.DepartureDate)
                .Select(x => x.Id)
                .ToList();
        }

        private List<int> SearchAddressIds(string term)
        {
            var query = _context.Addresses.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(term))
            {
                var pattern = $"%{term}%";
                query = query.Where(x =>
                    EF.Functions.Like(x.Street, pattern) ||
                    EF.Functions.Like(x.City, pattern) ||
                    EF.Functions.Like(x.PostalCode, pattern) ||
                    EF.Functions.Like(x.Country, pattern));
            }

            return query
                .OrderBy(x => x.City)
                .ThenBy(x => x.Street)
                .Select(x => x.Id)
                .ToList();
        }

        private List<int> SearchStatusLogIds(string term)
        {
            var query = _context.StatusLogs.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(term))
            {
                var pattern = $"%{term}%";
                query = query.Where(x =>
                    EF.Functions.Like(x.Package.TrackingNumber, pattern) ||
                    EF.Functions.Like(x.Location, pattern) ||
                    EF.Functions.Like(x.Description, pattern));
            }

            return query
                .OrderByDescending(x => x.TimeChanged)
                .Select(x => x.Id)
                .ToList();
        }

        private List<object> SearchAutocompleteCouriers(string term, int take)
        {
            var query = _context.Couriers.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(term))
            {
                var pattern = $"%{term}%";
                query = query.Where(x =>
                    EF.Functions.Like(x.FirstName, pattern) ||
                    EF.Functions.Like(x.LastName, pattern) ||
                    EF.Functions.Like(x.LicensePlate, pattern));
            }

            return query
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .Take(take)
                .Select(x => (object)new
                {
                    id = x.Id,
                    text = x.FirstName + " " + x.LastName + " (" + x.LicensePlate + ")"
                })
                .ToList();
        }

        private List<object> SearchAutocompleteUsers(string term, int take)
        {
            var query = _context.Users.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(term))
            {
                var pattern = $"%{term}%";
                query = query.Where(x =>
                    EF.Functions.Like(x.FirstName, pattern) ||
                    EF.Functions.Like(x.LastName, pattern) ||
                    EF.Functions.Like(x.Email, pattern));
            }

            return query
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .Take(take)
                .Select(x => (object)new
                {
                    id = x.Id,
                    text = x.FirstName + " " + x.LastName + " (" + x.Email + ")"
                })
                .ToList();
        }

        private List<object> SearchAutocompleteAddresses(string term, int take)
        {
            var query = _context.Addresses.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(term))
            {
                var pattern = $"%{term}%";
                query = query.Where(x =>
                    EF.Functions.Like(x.Street, pattern) ||
                    EF.Functions.Like(x.City, pattern) ||
                    EF.Functions.Like(x.PostalCode, pattern));
            }

            return query
                .OrderBy(x => x.City)
                .ThenBy(x => x.Street)
                .Take(take)
                .Select(x => (object)new
                {
                    id = x.Id,
                    text = x.Street + ", " + x.City + " " + x.PostalCode
                })
                .ToList();
        }

        private List<object> SearchAutocompletePackages(string term, int take)
        {
            var query = _context.Packages.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(term))
            {
                var pattern = $"%{term}%";
                query = query.Where(x =>
                    EF.Functions.Like(x.TrackingNumber, pattern) ||
                    EF.Functions.Like(x.Description, pattern));
            }

            return query
                .OrderByDescending(x => x.CreatedAt)
                .Take(take)
                .Select(x => (object)new
                {
                    id = x.Id,
                    text = x.TrackingNumber
                })
                .ToList();
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

            return DefaultSelectedType;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpGet("errors/app")]
        [AllowAnonymous]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
