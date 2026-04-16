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
            FillDetails(selectedObject, string.Empty, details, depth: 0, maxDepth: 4);

            var model = new ObjectDetailsViewModel
            {
                ObjectType = type ?? string.Empty,
                ObjectId = id,
                Values = details
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
                    else
                    {
                        FillDetails(item, itemPrefix, output, depth + 1, maxDepth);
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

                FillDetails(propValue, propPrefix, output, depth + 1, maxDepth);
            }
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
