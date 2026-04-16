using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection;
using Vjezba.Model.Data;
using Vjezba.Model.Models;

namespace Vjezba.Model.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index(string? selectedType)
        {
            var normalized = (selectedType ?? "package").ToLowerInvariant();
            var allowed = new HashSet<string> { "package", "courier", "warehouse", "user", "delivery" };

            if (!allowed.Contains(normalized))
            {
                normalized = "package";
            }

            ViewData["SelectedType"] = normalized;
            return View(BuildOverviewModel());
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Details(string type, int id)
        {
            var seedData = SeedDataFactory.Create();

            object? selectedObject = type?.ToLowerInvariant() switch
            {
                "address" => seedData.Addresses.FirstOrDefault(x => x.Id == id),
                "courier" => seedData.Couriers.FirstOrDefault(x => x.Id == id),
                "user" => seedData.Users.FirstOrDefault(x => x.Id == id),
                "package" => seedData.Packages.FirstOrDefault(x => x.Id == id),
                "warehouse" => seedData.Warehouses.FirstOrDefault(x => x.Id == id),
                "delivery" => seedData.Deliveries.FirstOrDefault(x => x.Id == id),
                _ => null
            };

            if (selectedObject is null)
            {
                return NotFound();
            }

            var details = new Dictionary<string, string>();
            var links = new Dictionary<string, ObjectReferenceLink>();
            FillDetails(selectedObject, string.Empty, details, links, depth: 0, maxDepth: 4);

            var model = new ObjectDetailsViewModel
            {
                ObjectType = type ?? string.Empty,
                ObjectId = id,
                Values = details,
                Links = links
            };

            return View(model);
        }

        private static ObjectOverviewViewModel BuildOverviewModel()
        {
            var seedData = SeedDataFactory.Create();

            return new ObjectOverviewViewModel
            {
                Addresses = seedData.Addresses,
                Couriers = seedData.Couriers,
                Users = seedData.Users,
                Packages = seedData.Packages,
                Warehouses = seedData.Warehouses,
                Deliveries = seedData.Deliveries
            };
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
                output[prefix] = "...";
                return;
            }

            if (IsSimple(value.GetType()))
            {
                output[prefix] = value.ToString() ?? string.Empty;
                return;
            }

            if (value is System.Collections.IEnumerable enumerable && value is not string)
            {
                var index = 0;
                foreach (var item in enumerable)
                {
                    var itemPrefix = string.IsNullOrWhiteSpace(prefix) ? $"[{index}]" : $"{prefix}[{index}]";

                    if (item is null)
                    {
                        output[itemPrefix] = "null";
                    }
                    else if (IsSimple(item.GetType()))
                    {
                        output[itemPrefix] = item.ToString() ?? string.Empty;
                    }
                    else if (TryCreateObjectReference(item, out var referenceType, out var referenceId, out var referenceLabel))
                    {
                        output[itemPrefix] = referenceLabel;
                        links[itemPrefix] = new ObjectReferenceLink
                        {
                            Type = referenceType,
                            Id = referenceId
                        };
                    }
                    else
                    {
                        output[itemPrefix] = item.GetType().Name;
                    }

                    index++;
                }

                if (index == 0)
                {
                    output[prefix] = "[]";
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

                if (IsSimple(prop.PropertyType))
                {
                    output[propPrefix] = propValue.ToString() ?? string.Empty;
                    continue;
                }

                if (TryCreateObjectReference(propValue, out var nestedType, out var nestedId, out var nestedLabel))
                {
                    output[propPrefix] = nestedLabel;
                    links[propPrefix] = new ObjectReferenceLink
                    {
                        Type = nestedType,
                        Id = nestedId
                    };
                }
                else if (propValue is System.Collections.IEnumerable && propValue is not string)
                {
                    FillDetails(propValue, propPrefix, output, links, depth + 1, maxDepth);
                }
                else
                {
                    output[propPrefix] = propValue.GetType().Name;
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

            type = modelTypeName switch
            {
                "address" => "address",
                "courier" => "courier",
                "user" => "user",
                "package" => "package",
                "warehouse" => "warehouse",
                "delivery" => "delivery",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(type))
            {
                return false;
            }

            var idProperty = modelType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
            if (idProperty?.GetValue(value) is not int foundId)
            {
                return false;
            }

            id = foundId;

            label = type switch
            {
                "courier" or "user" => BuildFullName(value) ?? $"{modelType.Name} #{id}",
                "package" => GetStringProperty(value, "TrackingNumber") ?? $"Package #{id}",
                "warehouse" => GetStringProperty(value, "Name") ?? $"Warehouse #{id}",
                "address" => BuildAddressLabel(value) ?? $"Address #{id}",
                "delivery" => $"Delivery #{id}",
                _ => $"{modelType.Name} #{id}"
            };

            return true;
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
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
