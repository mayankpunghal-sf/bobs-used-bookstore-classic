# Migration Summary: .NET Framework 4.8 → .NET 8

## Build Status

`dotnet build BobsBookstoreClassic.sln` — **Build succeeded — 0 Error(s), 2 Warning(s)**

The 2 remaining warnings are `NU1901: Amazon.CDK.Lib 2.188.0 low severity vulnerability` in the **Bookstore.Cdk** project, which was present before the migration and is unrelated to the .NET 8 upgrade.

---

## Changes Made

### Project Files

| File | Change |
|------|--------|
| `Bookstore.Web/Bookstore.Web.csproj` | Replaced legacy verbose XML with SDK-style `Microsoft.NET.Sdk.Web`, `net8.0`. Removed all explicit `<Reference>`, `<Compile>`, `<Content>` items, `packages.config` imports, and OWIN/EF6/System.Web packages. Added `NLog.Web.AspNetCore`, `Microsoft.AspNetCore.Authentication.OpenIdConnect` and AWS SDK references. |
| `Bookstore.Data/Bookstore.Data.csproj` | Changed `TargetFramework` from `netstandard2.0` to `net8.0` (required by EF Core 8). Replaced `EntityFramework 6.5.1` with `Microsoft.EntityFrameworkCore.SqlServer 8.0.0`. Upgraded `Magick.NET-Q8-AnyCPU` from 14.6.0 → 14.16.0 (resolves NU1901/NU1902/NU1903 vulnerability advisories). |

### Application Startup

| File | Change |
|------|--------|
| `Bookstore.Web/Program.cs` *(new)* | Top-level statements file that replaces `Global.asax`, `Startup.cs`, and all `App_Start/` files. Configures NLog, `BookstoreConfiguration` (loading SSM overrides at startup), EF Core with SQL Server, all services/repositories via built-in DI, file/image validation service selection, cookie/OpenIdConnect auth, and ASP.NET Core MVC route mapping including areas. |
| `Bookstore.Web/appsettings.json` *(new)* | Replaces `Web.config` settings — connection string, service toggles, Cognito/S3 placeholders, logging config. |
| `Bookstore.Web/appsettings.Development.json` *(new)* | Dev-only logging overrides. |

### Deleted (Legacy)

- `Global.asax` / `Global.asax.cs`
- `Startup.cs` (OWIN startup class)
- `App_Start/AuthenticationSetup.cs`
- `App_Start/BundleConfig.cs`
- `App_Start/ConfigurationSetup.cs`
- `App_Start/DependencyInjectionSetup.cs`
- `App_Start/FilterConfig.cs`
- `App_Start/LoggingSetup.cs`
- `App_Start/RouteConfig.cs`
- `Areas/Admin/AdminAreaRegistration.cs`
- `Web.config`, `Web.Debug.config`, `Web.Release.config`
- `Views/Web.config`, `Areas/Admin/Views/web.config`
- `packages.config`
- `Properties/AssemblyInfo.cs` (Web and Domain — SDK auto-generates)
- `Bookstore.Data/App.config`, `Bookstore.Data/Properties/AssemblyInfo.cs`

### Configuration

| File | Change |
|------|--------|
| `BookstoreConfiguration.cs` | Rewrote to use `IConfiguration` instead of `ConfigurationManager`. Retains the static singleton API (`GetSetting`, `GetConnectionString`, `AddSetting`, `AddConnectionString`) so all callers are unaffected. Keys are normalised from IConfiguration `:` separator to `/` separator to match existing call sites. SSM path `ConnectionStrings/BookstoreDatabaseConnection` is automatically routed to the connection strings store. |

### Data Layer

| File | Change |
|------|--------|
| `ApplicationDbContext.cs` | Migrated from EF6 (`System.Data.Entity.DbContext`) to EF Core 8 (`Microsoft.EntityFrameworkCore.DbContext`). Constructor now uses `DbContextOptions<ApplicationDbContext>`. `OnModelCreating` rewritten: `PluralizingTableNameConvention.Remove` → explicit `ToTable("X")` calls, `HasRequired().WithMany().WillCascadeOnDelete(false)` → `HasOne().WithMany().OnDelete(DeleteBehavior.NoAction)`, composite key `HasDatabaseGeneratedOption(Identity)` → `ValueGeneratedOnAdd()`. |
| `BookstoreDbInitializer.cs` | Replaced EF6 `DropCreateDatabaseIfModelChanges<T>` initialiser with a static `SeedAsync(ApplicationDbContext)` helper. Called from `Program.cs` at startup via `EnsureCreatedAsync`. |
| `PaginatedList.cs` | `using System.Data.Entity` → `using Microsoft.EntityFrameworkCore` (same API surface). |
| All `Repositories/*.cs` | `using System.Data.Entity` → `using Microsoft.EntityFrameworkCore`. EF6 nested `Include(x => x.Collection.Select(y => y.Nav))` syntax replaced with EF Core `Include(...).ThenInclude(...)` chains. `Task.Run(() => dbContext.X.Add(y))` → `await dbContext.X.AddAsync(y)`. |

### Web Layer

| File | Change |
|------|--------|
| All `Controllers/*.cs` | `using System.Web.Mvc` → `using Microsoft.AspNetCore.Mvc`. `ActionResult` → `IActionResult`. `AllowAnonymous`, `Authorize`, `HttpPost` attributes remain unchanged (same names, different namespace). |
| `Areas/Admin/Controllers/*.cs` | Same MVC namespace swap. `[RouteArea("Admin")]` → `[Area("Admin")]`. |
| `Areas/Admin/AdminAreaRegistration.cs` | Deleted — area registration via `AreaRegistration` class is not used in ASP.NET Core. Areas are now configured by `[Area("Admin")]` on controllers and the `{area:exists}/...` route in `Program.cs`. |
| `Helpers/LocalAuthenticationMiddleware.cs` | Rewrote from OWIN `OwinMiddleware` to ASP.NET Core conventional middleware (takes `RequestDelegate` in ctor, `InvokeAsync(HttpContext, ICustomerService)`). Uses `HttpContext.Request.Cookies` / `Response.Cookies.Append` instead of `HttpContext.Current`. |
| `Helpers/IOwinRequestExtensions.cs` | Replaced OWIN `IOwinRequest` extension with ASP.NET Core `HttpRequest.GetReturnUrl()` (kept for any residual use). |
| `Helpers/HttpContextExtensions.cs` | Replaced `HttpContextBase` with `HttpContext`. Uses `Response.Cookies.Append` with `CookieOptions` instead of `HttpCookie`. |
| `Helpers/ControllerExtensions.cs` | Updated `Controller` type from `System.Web.Mvc.Controller` to `Microsoft.AspNetCore.Mvc.Controller`. |
| `Helpers/MvcHelpers.cs` | Changed `HtmlHelper` parameter type to `IHtmlHelper` (ASP.NET Core interface). |
| `Helpers/MaxFileSizeAttribute.cs` | `HttpPostedFileBase` → `IFormFile`, `.ContentLength` → `.Length`. |
| `Helpers/ImageTypesAttribute.cs` | `HttpPostedFileBase` → `IFormFile`. |
| `Areas/Admin/Models/Inventory/InventoryCreateUpdateViewModel.cs` | `HttpPostedFileBase CoverImage` → `IFormFile CoverImage`. |
| `Models/Resale/ResaleCreateViewModel.cs` | `using System.Web.Mvc` → `using Microsoft.AspNetCore.Mvc.Rendering`. |
| `Models/Address/AddressCreateUpdateViewModel.cs` | Same namespace swap for `SelectListItem`. |
| `Areas/Admin/Models/Inventory/InventoryIndexViewModel.cs` | Same namespace swap. |
| `Areas/Admin/Models/Offers/OfferIndexViewModel.cs` | Same namespace swap. |
| `Areas/Admin/Models/ReferenceData/ReferenceDataCreateViewModel.cs` | Same namespace swap. |
| `Views/_ViewImports.cshtml` | Added `@using Bookstore.Web.ViewModel` and `@using Microsoft.AspNetCore.Mvc.Rendering`. |
| `Areas/Admin/Views/_ViewImports.cshtml` *(new)* | Created area-level imports with `Microsoft.AspNetCore.Mvc.Rendering` and TagHelper directives. |
| `Areas/Admin/Views/Offers/Index.cshtml` | Replaced `@Html.EnumDropDownListFor(...)` (does not exist in ASP.NET Core) with `<select asp-for="..." asp-items="@Html.GetEnumSelectList(...)">` tag helper. |
| `Areas/Admin/Views/Orders/Index.cshtml` | Same replacement for `@Html.EnumDropDownListFor`. |
| `Areas/Admin/Controllers/InventoryController.cs` | `model.CoverImage?.InputStream` → `model.CoverImage?.OpenReadStream()` (IFormFile API). |

### Static Files

Static files are now served from the project-root `Content/` and `Scripts/` directories via explicit `StaticFileOptions` mappings in `Program.cs` (mirroring the original virtual paths `/Content` and `/Scripts` used by layouts and views).

---

## Architecture Changes

- **DI**: Autofac + `Autofac.Mvc5` + `Autofac.Owin` replaced by ASP.NET Core built-in DI (`builder.Services.AddScoped/Singleton`).
- **Auth**: OWIN cookie + OpenIdConnect replaced by `AddAuthentication().AddCookie().AddOpenIdConnect(...)`.
- **Bundling**: `System.Web.Optimization` (BundleConfig) removed. CSS/JS files are referenced directly in layouts via CDN and static file paths.
- **Logging**: NLog configuration moved from OWIN startup to `builder.Host.UseNLog()` with `NLog.Web.AspNetCore`.
- **Global error handling**: `HandleErrorAttribute` global filter replaced by `app.UseExceptionHandler("/Home/Error")`.

---

## Next Steps

- **EF Core Migrations**: Run `dotnet ef migrations add InitialMigration` and `dotnet ef database update` to create EF Core migration history for the existing SQL Server database (currently using `EnsureCreatedAsync` which is suitable for development but not for production schema management).
- **Cognito HTTPS requirement**: Cognito's Hosted UI requires HTTPS redirect URIs (except `http://localhost`). When deploying to ECS, configure the load balancer for HTTPS and verify the `signin-oidc` redirect URI.
- **Amazon CDK.Lib vulnerability**: `Amazon.CDK.Lib 2.188.0` has a NU1901 low severity advisory in `Bookstore.Cdk`. This is pre-existing and unrelated to the migration; upgrade to the latest CDK.Lib when convenient.
- **Magick.NET upgraded**: `Magick.NET-Q8-AnyCPU` was upgraded from 14.6.0 to 14.16.0 to resolve known vulnerability advisories. Test image resizing functionality.
- **wwwroot**: Consider moving `Content/` and `Scripts/` to `wwwroot/` (the conventional ASP.NET Core web root) to simplify static file configuration and align with deployment tooling expectations.
