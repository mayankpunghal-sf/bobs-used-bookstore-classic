using Amazon.Rekognition;
using Amazon.S3;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using BobsBookstoreClassic.Data;
using Bookstore.Common;
using Bookstore.Data;
using Bookstore.Data.FileServices;
using Bookstore.Data.ImageResizeService;
using Bookstore.Data.ImageValidationServices;
using Bookstore.Data.Repositories;
using Bookstore.Domain;
using Bookstore.Domain.Addresses;
using Bookstore.Domain.Books;
using Bookstore.Domain.Carts;
using Bookstore.Domain.Customers;
using Bookstore.Domain.Offers;
using Bookstore.Domain.Orders;
using Bookstore.Domain.ReferenceData;
using Bookstore.Web.Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;

// ── Bootstrap NLog early so any startup errors are captured ──────────────────
var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── NLog provider ─────────────────────────────────────────────────────────
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // ── Initialise BookstoreConfiguration from IConfiguration ─────────────────
    BookstoreConfiguration.Initialize(builder.Configuration);

    // ── Load SSM overrides (if configured) ────────────────────────────────────
    LoadSsmConfiguration(builder.Configuration);

    // ── Entity Framework Core ─────────────────────────────────────────────────
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(BookstoreConfiguration.GetConnectionString("BookstoreDatabaseConnection")));

    // ── Domain services ───────────────────────────────────────────────────────
    builder.Services.AddScoped<IBookService, BookService>();
    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddScoped<IReferenceDataService, ReferenceDataService>();
    builder.Services.AddScoped<IOfferService, OfferService>();
    builder.Services.AddScoped<ICustomerService, CustomerService>();
    builder.Services.AddScoped<IAddressService, AddressService>();
    builder.Services.AddScoped<IShoppingCartService, ShoppingCartService>();
    builder.Services.AddScoped<IImageResizeService, ImageResizeService>();

    // ── Repositories ─────────────────────────────────────────────────────────
    builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
    builder.Services.AddScoped<IAddressRepository, AddressRepository>();
    builder.Services.AddScoped<IBookRepository, BookRepository>();
    builder.Services.AddScoped<IOfferRepository, OfferRepository>();
    builder.Services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
    builder.Services.AddScoped<IOrderRepository, OrderRepository>();
    builder.Services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();
    builder.Services.AddScoped(typeof(IPaginatedList<>), typeof(PaginatedList<>));

    // ── File service ──────────────────────────────────────────────────────────
    if (BookstoreConfiguration.GetSetting("Services/FileService") == "aws")
    {
        builder.Services.AddSingleton<IAmazonS3, AmazonS3Client>();
        builder.Services.AddScoped<IFileService, S3FileService>();
    }
    else
    {
        var contentPath = Path.Combine(builder.Environment.ContentRootPath, "Content");
        builder.Services.AddSingleton<IFileService>(_ => new LocalFileService(contentPath));
    }

    // ── Image validation service ──────────────────────────────────────────────
    if (BookstoreConfiguration.GetSetting("Services/ImageValidationService") == "aws")
    {
        builder.Services.AddSingleton<IAmazonRekognition, AmazonRekognitionClient>();
        builder.Services.AddScoped<IImageValidationService, RekognitionImageValidationService>();
    }
    else
    {
        builder.Services.AddScoped<IImageValidationService, LocalImageValidationService>();
    }

    // ── Authentication ────────────────────────────────────────────────────────
    if (BookstoreConfiguration.GetSetting("Services/Authentication") == "aws")
    {
        ConfigureCognitoAuthentication(builder.Services, builder.Configuration);
    }
    else
    {
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = "/Authentication/Login";
            });
        builder.Services.AddScoped<LocalAuthenticationMiddleware>();
    }

    // ── MVC with global Authorize filter ─────────────────────────────────────
    builder.Services.AddControllersWithViews(options =>
    {
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
        options.Filters.Add(new AuthorizeFilter(policy));
    });

    var app = builder.Build();

    // ── Seed database ─────────────────────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await BookstoreDbInitializer.SeedAsync(db);
    }

    // ── Middleware pipeline ───────────────────────────────────────────────────
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }
    else
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseHttpsRedirection();

    // Serve files from the project-root Content and Scripts directories
    // (mirrors the legacy virtual paths used by the Razor views / layout)
    app.UseStaticFiles();

    var contentFolder = Path.Combine(app.Environment.ContentRootPath, "Content");
    if (Directory.Exists(contentFolder))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(contentFolder),
            RequestPath = "/Content"
        });
    }

    var scriptsFolder = Path.Combine(app.Environment.ContentRootPath, "Scripts");
    if (Directory.Exists(scriptsFolder))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(scriptsFolder),
            RequestPath = "/Scripts"
        });
    }

    app.UseRouting();

    if (BookstoreConfiguration.GetSetting("Services/Authentication") != "aws")
    {
        app.UseMiddleware<LocalAuthenticationMiddleware>();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    // Areas (Admin) must be mapped before the default route
    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Application stopped due to an unhandled exception.");
    throw;
}
finally
{
    LogManager.Shutdown();
}

// ── Helper: load settings from AWS Systems Manager Parameter Store ─────────
static void LoadSsmConfiguration(IConfiguration configuration)
{
    var rootPath = "/" + Constants.AppName;
    const string databasePath = "/Database";
    const string authenticationPath = "/Authentication";
    const string fileServicePath = "/Files";

    if (BookstoreConfiguration.GetSetting("Services/Database") == "aws")
    {
        try
        {
            using var client = new AmazonSimpleSystemsManagementClient();
            var request = new GetParameterRequest
            {
                Name = $"{rootPath}{databasePath}/ConnectionStrings/BookstoreDatabaseConnection"
            };
            var response = client.GetParameterAsync(request).GetAwaiter().GetResult();
            BookstoreConfiguration.AddConnectionString(
                "BookstoreDatabaseConnection", response.Parameter.Value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: failed to load database connection string from SSM: {ex.Message}");
        }
    }

    if (BookstoreConfiguration.GetSetting("Services/Authentication") == "aws")
    {
        try
        {
            using var client = new AmazonSimpleSystemsManagementClient();
            var request = new GetParametersByPathRequest
            {
                Path = $"{rootPath}{authenticationPath}/",
                Recursive = true
            };
            var response = client.GetParametersByPathAsync(request).GetAwaiter().GetResult();
            foreach (var parameter in response.Parameters)
            {
                var key = parameter.Name.Replace($"{rootPath}/", string.Empty).Replace("/", ":");
                BookstoreConfiguration.AddSetting(key.Replace(":", "/"), parameter.Value);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: failed to load authentication settings from SSM: {ex.Message}");
        }
    }

    if (BookstoreConfiguration.GetSetting("Services/FileService") == "aws")
    {
        try
        {
            using var client = new AmazonSimpleSystemsManagementClient();
            var request = new GetParametersByPathRequest
            {
                Path = $"{rootPath}{fileServicePath}/",
                Recursive = true
            };
            var response = client.GetParametersByPathAsync(request).GetAwaiter().GetResult();
            foreach (var parameter in response.Parameters)
            {
                var key = parameter.Name.Replace($"{rootPath}/", string.Empty);
                BookstoreConfiguration.AddSetting(key, parameter.Value);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: failed to load file service settings from SSM: {ex.Message}");
        }
    }
}

// ── Helper: configure Cognito / OpenIdConnect authentication ──────────────
static void ConfigureCognitoAuthentication(IServiceCollection services, IConfiguration configuration)
{
    services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = BookstoreConfiguration.GetSetting("Authentication/Cognito/LocalClientId");
        options.MetadataAddress = BookstoreConfiguration.GetSetting("Authentication/Cognito/MetadataAddress");
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.SaveTokens = true;
        options.UseTokenLifetime = false;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "cognito:username",
            RoleClaimType = "cognito:groups"
        };
        options.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = context =>
            {
                var redirectUri = $"{context.Request.Scheme}://{context.Request.Host}/signin-oidc";
                context.ProtocolMessage.RedirectUri = redirectUri;
                return Task.CompletedTask;
            },
            OnAuthorizationCodeReceived = context =>
            {
                var redirectUri = $"{context.Request.Scheme}://{context.Request.Host}/signin-oidc";
                context.TokenEndpointRequest!.RedirectUri = redirectUri;
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var service = context.HttpContext.RequestServices.GetRequiredService<ICustomerService>();
                var identity = context.Principal!.Identity as System.Security.Claims.ClaimsIdentity;
                if (identity == null) return;

                var dto = new CreateOrUpdateCustomerDto(
                    identity.GetSub()!,
                    identity.Name ?? string.Empty,
                    identity.FindFirst(c => c.Type.Contains("givenname"))?.Value ?? string.Empty,
                    identity.FindFirst(c => c.Type.Contains("surname"))?.Value ?? string.Empty);

                await service.CreateOrUpdateCustomerAsync(dto);
            }
        };
    });
}
