# Sitemap

## Attribute Routes

- / -> HomeController.Index -> Views/Home/Index.cshtml
- /dashboard/{selectedType?} -> HomeController.Index -> Views/Home/Index.cshtml
- /hub/{selectedType?} -> HomeController.Index -> Views/Home/Index.cshtml
- /policy/privacy -> HomeController.Privacy -> Views/Home/Privacy.cshtml
- /objects/{type}/{id:int} -> HomeController.Details -> Views/Home/Details.cshtml
- /details/{type}/{id:int} -> HomeController.Details -> Views/Home/Details.cshtml
- /errors/app -> HomeController.Error -> Views/Shared/Error.cshtml

## Conventional Route

- /{controller=Home}/{action=Index}/{id?}
  - /Home/Index -> Views/Home/Index.cshtml
  - /Home/Privacy -> Views/Home/Privacy.cshtml
  - /Home/Details/{type}/{id} -> Views/Home/Details.cshtml
  - /Home/Error -> Views/Shared/Error.cshtml
