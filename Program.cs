using Data.Concrete.EfCore;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using System.Globalization;
using Alpha.Services;
using Alpha.Identity;
using Microsoft.AspNetCore.Identity;
using Alpha.EmailServices;
using Data.Abstract;
using Alpha.Data;
using Alpha.Extensions;
using Alpha;

var builder = WebApplication.CreateBuilder(args);

#region Configuration

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();

var config = builder.Configuration;

var emailSection = config.GetSection("EmailSender");
var host = emailSection.GetValue<string>("SMTPMail")
    ?? throw new InvalidOperationException("SMTP host missing");
var port = emailSection.GetValue<int>("Port");
var username = emailSection.GetValue<string>("Username")
    ?? throw new InvalidOperationException("SMTP username missing");
// Email password'i ortam değişkeninden alalım, gerekiyorsa override yapalım
var password = Environment.GetEnvironmentVariable("EmailSender__Password")
    ?? emailSection.GetValue<string>("Password")
    ?? throw new InvalidOperationException("SMTP password missing");
var enableSsl = emailSection.GetValue<bool?>("EnableSsl") ?? true;

#endregion

#region ConnectionStrings
//"Server=localhost\\SQLEXPRESS;Database=AlphaDb;User Id=sa;Password=*.;TrustServerCertificate=True;"
var shopConnection = Environment.GetEnvironmentVariable("SHOP_DB")
    ?? config.GetConnectionString("ShopContext")
    ?? throw new InvalidOperationException("SHOP_DB not found.");

var identityConnection = Environment.GetEnvironmentVariable("APP_DB")
    ?? config.GetConnectionString("ApplicationContext")
    ?? throw new InvalidOperationException("APP_DB not found.");

builder.Services.AddDbContext<ShopContext>(options =>
    options.UseSqlServer(shopConnection));

builder.Services.AddDbContext<ApplicationContext>(options =>
    options.UseSqlServer(identityConnection));
#endregion

#region IdentityConfiguration

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Lockout.MaxFailedAccessAttempts = 4;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(4);
    options.Lockout.AllowedForNewUsers = true;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/admin/login";
    options.LogoutPath = "/admin/logout";
    options.AccessDeniedPath = "/accessdenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.Cookie = new CookieBuilder
    {
        HttpOnly = true,
        Name = ".Alpha.Security.Cookie",
        SameSite = SameSiteMode.Lax, // Changed from None to Lax for better Plesk compatibility
        SecurePolicy = CookieSecurePolicy.SameAsRequest, // Changed to SameAsRequest for Plesk reverse proxy
        IsEssential = true // Mark as essential for GDPR compliance
    };
});

#endregion

#region Security

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = ".Alpha.AntiForgery";
    options.Cookie.SameSite = SameSiteMode.Lax; // Plesk compatibility
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Support both HTTP/HTTPS
    options.Cookie.IsEssential = true; // Essential for functionality
});

#endregion

#region Localization

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    // Use centralized culture configuration
    var supportedCultures = CultureConfig.GetCultureCodes()
        .Select(c => new CultureInfo(c)).ToList();

    options.DefaultRequestCulture = new RequestCulture(CultureConfig.DefaultCulture);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
});

#endregion

#region Services

string resourcesPath = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
string blogResourcesPath = Path.Combine(Directory.GetCurrentDirectory(), "BlogResources");

builder.Services.AddSingleton<IFileProvider>(new PhysicalFileProvider(resourcesPath));

// Add AliveResourceService for main Resources folder (used by LanguageService)
var mainResourceService = new AliveResourceService(resourcesPath);
builder.Services.AddSingleton<AliveResourceService>(mainResourceService);
Console.WriteLine($"[Program] Main Resources watcher initialized at startup.");

builder.Services.AddSingleton<IResxResourceService, ResxResourceService>();
builder.Services.AddSingleton<IBlogResxService, BlogResxService>();

// Create a separate watcher for BlogResources folder (standalone, not in DI)
// This just watches for file changes but doesn't serve resources via LanguageService
if (Directory.Exists(blogResourcesPath))
{
    var blogWatcher = new AliveResourceService(blogResourcesPath);
    Console.WriteLine($"[Program] BlogResources watcher initialized at startup.");
    // Note: This is a standalone watcher, not added to DI to avoid overriding the main one
    // BlogResxService handles reading from BlogResources directly
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<LanguageService>();
builder.Services.AddScoped<INavbarService, NavbarService>();
builder.Services.AddScoped<IImageRepository, EfCoreImageRepository>();
builder.Services.AddScoped<IFooterService, FooterService>();
builder.Services.AddScoped<ICarouselRepository, EfCoreCarouselRepository>();
builder.Services.AddScoped<IBlogRepository, EfCoreBlogRepository>();
builder.Services.AddScoped<IProductRepository, EfCoreProductRepository>();
builder.Services.AddScoped<ICategoryRepository, EfCoreCategoryRepository>();

builder.Services.AddScoped<IEmailSender, SmtpEmailSender>(i =>
    new SmtpEmailSender(host, port, enableSsl, username, password));

#endregion

#region MVC & Razor

builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization()
    .AddRazorRuntimeCompilation();

builder.Services.AddMvc()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(SharedResource));
    });
#endregion

var app = builder.Build();

#region SeedIdentityData

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    var rootUserSection = configuration.GetSection("Data:Users").GetChildren()
        .FirstOrDefault(u => string.Equals(u.GetValue<string>("username"), "root", StringComparison.OrdinalIgnoreCase));

    if (rootUserSection != null)
    {
        var rootEmail = rootUserSection.GetValue<string>("email");

        if (string.IsNullOrWhiteSpace(rootEmail))
        {
            Console.WriteLine("⚠️ Root email is not provided. Skipping seeding.");
        }
        else
        {
            var rootUser = await userManager.FindByEmailAsync(rootEmail);
            if (rootUser == null)
            {
                Console.WriteLine("🔨 Seeding root user...");
                await SeedIdentity.Seed(userManager, roleManager, configuration);
                Console.WriteLine("✅ Root user seeding completed.");
            }
            else
            {
                Console.WriteLine($"✅ Root user '{rootEmail}' already exists.");
            }
        }
    }
    else
    {
        Console.WriteLine("⚠️ No root user defined in configuration.");
    }
}

#endregion

#region AppMiddleware
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        var defaultCulture = "tr"; // veya "en"
        context.Response.Redirect($"/{defaultCulture}");
        return;
    }

    await next();
});
var ckLicenseKey = builder.Configuration["CKEditor:LicenseKey"];

app.Use(async (context, next) =>
{
    context.Items["CKLicenseKey"] = ckLicenseKey;
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllerRoute(
    name: "sitemap",
    pattern: "sitemap.xml",
    defaults: new { controller = "Sitemap", action = "Index" });
    
app.MapControllerRoute(
    name: "localized_product",
    pattern: "{culture}/urun/{id}/{slug?}",
    defaults: new { controller = "Home", action = "ProductDetail" });

app.MapControllerRoute(
    name: "localized_services",
    pattern: "{culture}/hizmetler",
    defaults: new { controller = "Home", action = "Services" });

app.MapControllerRoute(
    name: "localized_blog_detail",
    pattern: "{culture}/blog/{id}/{slug?}",
    defaults: new { controller = "Home", action = "BlogDetails" });

app.MapControllerRoute(
    name: "localized_blog",
    pattern: "{culture}/blog",
    defaults: new { controller = "Home", action = "Blog" });

app.MapControllerRoute(
    name: "localized_contact",
    pattern: "{culture}/contact",
    defaults: new { controller = "Home", action = "Contact" });

app.MapControllerRoute(
    name: "localized_about",
    pattern: "{culture}/about", 
    defaults: new { controller = "Home", action = "About" });

app.MapControllerRoute(
    name: "localized_privacy",
    pattern: "{culture}/privacy", 
    defaults: new { controller = "Home", action = "Privacy" });

app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{action=Index}/{id?}",
    defaults: new { controller = "Admin" });

app.MapControllerRoute(
    name: "localized_index",
    pattern: "{culture}",
    defaults: new { controller = "Home", action = "Index" });


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

#endregion

app.Run();
