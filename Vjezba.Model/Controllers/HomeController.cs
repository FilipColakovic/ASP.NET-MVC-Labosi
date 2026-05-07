using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Vjezba.Model.Data;
using Vjezba.Model.Models;

namespace Vjezba.Model.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private const string DefaultSelectedType = "package";

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        private static readonly HashSet<string> OverviewTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "package",
            "courier",
            "warehouse",
            "user",
            "delivery"
        };

        private static readonly IReadOnlyDictionary<string, Func<AppDbContext, int, object?>> DetailSelectors =
            new Dictionary<string, Func<AppDbContext, int, object?>>(StringComparer.OrdinalIgnoreCase)
            {
                ["address"] = (db, id) => db.Addresses.AsNoTracking().FirstOrDefault(x => x.Id == id),
                ["courier"] = (db, id) => db.Couriers.AsNoTracking().FirstOrDefault(x => x.Id == id),
                ["user"] = (db, id) => db.Users.AsNoTracking().FirstOrDefault(x => x.Id == id),
                ["package"] = (db, id) => LoadPackageGraph(db.Packages.AsNoTracking()).FirstOrDefault(x => x.Id == id),
                ["warehouse"] = (db, id) => db.Warehouses.AsNoTracking()
                    .Include(x => x.Address)
                    .Include(x => x.StoredPackages)
                    .FirstOrDefault(x => x.Id == id),
                ["delivery"] = (db, id) => db.Deliveries.AsNoTracking()
                    .Include(x => x.Courier)
                    .Include(x => x.Packages)
                    .FirstOrDefault(x => x.Id == id),
                ["statuslog"] = (db, id) => db.StatusLogs.AsNoTracking()
                    .Include(x => x.Package)
                    .FirstOrDefault(x => x.Id == id)
            };

        private static readonly IReadOnlyDictionary<string, Func<object, int, string>> ReferenceLabelBuilders =
            new Dictionary<string, Func<object, int, string>>(StringComparer.Ordinal)
            {
                ["courier"] = (value, id) => BuildFullName(value) ?? $"{value.GetType().Name} #{id}",
                ["user"] = (value, id) => BuildFullName(value) ?? $"{value.GetType().Name} #{id}",
                ["package"] = (value, id) => GetStringProperty(value, "TrackingNumber") ?? $"Package #{id}",
                ["warehouse"] = (value, id) => GetStringProperty(value, "Name") ?? $"Warehouse #{id}",
                ["address"] = (value, id) => BuildAddressLabel(value) ?? $"Address #{id}",
                ["delivery"] = (_, id) => $"Delivery #{id}",
                ["statuslog"] = (_, id) => $"Status Log #{id}"
            };

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
            var selectedObject = ResolveDetailObject(_context, normalizedType, id);

            if (selectedObject is null)
            {
                return NotFound();
            }

            var details = new Dictionary<string, string>();
            var links = new Dictionary<string, ObjectReferenceLink>();
            FillDetails(selectedObject, string.Empty, details, links, depth: 0, maxDepth: 4);

            var model = new ObjectDetailsViewModel
            {
                ObjectType = normalizedType,
                ObjectId = id,
                Values = details,
                Links = links
            };

            return View(model);
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

        private static string NormalizeType(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }

        private static object? ResolveDetailObject(AppDbContext dbContext, string type, int id)
        {
            if (!DetailSelectors.TryGetValue(type, out var selector))
            {
                return null;
            }

            return selector(dbContext, id);
        }

        private static void FillDetails(
            object value,
            string prefix,
            IDictionary<string, string> output,
            IDictionary<string, ObjectReferenceLink> links,
            int depth,
            int maxDepth)
        {
            if (depth > maxDepth)
            {
                output[GetOutputKey(prefix)] = "...";
                return;
            }

            if (IsSimple(value.GetType()))
            {
                output[GetOutputKey(prefix)] = value.ToString() ?? string.Empty;
                return;
            }

            if (value is IEnumerable enumerable && value is not string)
            {
                var index = 0;
                foreach (var item in enumerable)
                {
                    var itemPrefix = string.IsNullOrWhiteSpace(prefix) ? $"[{index}]" : $"{prefix}[{index}]";
                    if (item is null)
                    {
                        output[itemPrefix] = "null";
                    }
                    else if (item is IEnumerable nestedEnumerable && item is not string)
                    {
                        FillDetails(nestedEnumerable, itemPrefix, output, links, depth + 1, maxDepth);
                    }
                    else if (IsSimple(item.GetType()) || TryCreateObjectReference(item, out _, out _, out _))
                    {
                        WriteValue(item, itemPrefix, output, links);
                    }
                    else
                    {
                        FillDetails(item, itemPrefix, output, links, depth + 1, maxDepth);
                    }

                    index++;
                }

                if (index == 0)
                {
                    output[GetOutputKey(prefix)] = "[]";
                }

                return;
            }

            foreach (var prop in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propPrefix = string.IsNullOrWhiteSpace(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                var propValue = prop.GetValue(value);

                if (propValue is null)
                {
                    output[propPrefix] = "null";
                    continue;
                }

                if (propValue is IEnumerable && propValue is not string)
                {
                    FillDetails(propValue, propPrefix, output, links, depth + 1, maxDepth);
                }
                else
                {
                    WriteValue(propValue, propPrefix, output, links);
                }
            }
        }

        private static bool TryCreateObjectReference(object value, out string type, out int id, out string label)
        {
            type = string.Empty;
            id = 0;
            label = string.Empty;

            var modelType = value.GetType();
            var modelTypeName = modelType.Name.ToLowerInvariant();

            if (!DetailSelectors.ContainsKey(modelTypeName))
            {
                return false;
            }

            type = modelTypeName;

            var idProperty = modelType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
            if (idProperty?.GetValue(value) is not int foundId)
            {
                return false;
            }

            id = foundId;
            label = BuildReferenceLabel(type, value, id);

            return true;
        }

        private static void WriteValue(
            object? value,
            string key,
            IDictionary<string, string> output,
            IDictionary<string, ObjectReferenceLink> links)
        {
            if (value is null)
            {
                output[GetOutputKey(key)] = "null";
                return;
            }

            if (IsSimple(value.GetType()))
            {
                output[GetOutputKey(key)] = value.ToString() ?? string.Empty;
                return;
            }

            if (TryCreateObjectReference(value, out var referenceType, out var referenceId, out var referenceLabel))
            {
                output[key] = referenceLabel;
                links[key] = new ObjectReferenceLink
                {
                    Type = referenceType,
                    Id = referenceId
                };
                return;
            }

            output[GetOutputKey(key)] = value.GetType().Name;
        }

        private static string BuildReferenceLabel(string type, object value, int id)
        {
            if (ReferenceLabelBuilders.TryGetValue(type, out var labelBuilder))
            {
                return labelBuilder(value, id);
            }

            return $"{value.GetType().Name} #{id}";
        }

        private static string GetOutputKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? "(root)" : key;
        }

        private static string? BuildFullName(object value)
        {
            var firstName = GetStringProperty(value, "FirstName");
            var lastName = GetStringProperty(value, "LastName");

            var fullName = string.Join(" ", new[] { firstName, lastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
            return string.IsNullOrWhiteSpace(fullName) ? null : fullName;
        }

        private static string? BuildAddressLabel(object value)
        {
            var street = GetStringProperty(value, "Street");
            var city = GetStringProperty(value, "City");

            var address = string.Join(", ", new[] { street, city }.Where(s => !string.IsNullOrWhiteSpace(s)));
            return string.IsNullOrWhiteSpace(address) ? null : address;
        }

        private static string? GetStringProperty(object value, string propertyName)
        {
            var property = value.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            return property?.GetValue(value) as string;
        }

        private static bool IsSimple(Type type)
        {
            var normalized = Nullable.GetUnderlyingType(type) ?? type;

            return normalized.IsPrimitive
                || normalized.IsEnum
                || normalized == typeof(string)
                || normalized == typeof(decimal)
                || normalized == typeof(DateTime)
                || normalized == typeof(DateTimeOffset)
                || normalized == typeof(TimeSpan)
                || normalized == typeof(Guid);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpGet("errors/app")]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
