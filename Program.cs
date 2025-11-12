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

var fromEmail = emailSection.GetValue<string>("FromEmail") ?? username;
var fromName = emailSection.GetValue<string>("FromName") ?? "Alpha Safety Shoes";

builder.Services.AddScoped<IEmailSender, SmtpEmailSender>(i =>
    new SmtpEmailSender(host, port, enableSsl, username, password, fromEmail, fromName));

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
// Use custom middleware for culture detection and redirection
app.UseMiddleware<Alpha.Middleware.CultureRedirectMiddleware>();
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

// Product routes - keep URL slugs as-is (they're already in DB)
app.MapControllerRoute(
    name: "localized_product_tr",
    pattern: "tr/urun/{id}/{slug?}",
    defaults: new { controller = "Home", action = "ProductDetail", culture = "tr" });

app.MapControllerRoute(
    name: "localized_product_en",
    pattern: "en/product/{id}/{slug?}",
    defaults: new { controller = "Home", action = "ProductDetail", culture = "en" });

app.MapControllerRoute(
    name: "localized_product_de",
    pattern: "de/produkt/{id}/{slug?}",
    defaults: new { controller = "Home", action = "ProductDetail", culture = "de" });

app.MapControllerRoute(
    name: "localized_product_fr",
    pattern: "fr/produit/{id}/{slug?}",
    defaults: new { controller = "Home", action = "ProductDetail", culture = "fr" });

app.MapControllerRoute(
    name: "localized_product_ar",
    pattern: "ar/product/{id}/{slug?}",
    defaults: new { controller = "Home", action = "ProductDetail", culture = "ar" });

// Services routes - translated
app.MapControllerRoute(
    name: "localized_services_tr",
    pattern: "tr/hizmetlerimiz",
    defaults: new { controller = "Home", action = "Services", culture = "tr" });

app.MapControllerRoute(
    name: "localized_services_en",
    pattern: "en/services",
    defaults: new { controller = "Home", action = "Services", culture = "en" });

app.MapControllerRoute(
    name: "localized_services_de",
    pattern: "de/dienstleistungen",
    defaults: new { controller = "Home", action = "Services", culture = "de" });

app.MapControllerRoute(
    name: "localized_services_fr",
    pattern: "fr/services",
    defaults: new { controller = "Home", action = "Services", culture = "fr" });

app.MapControllerRoute(
    name: "localized_services_ar",
    pattern: "ar/services",
    defaults: new { controller = "Home", action = "Services", culture = "ar" });

// Blog detail routes - keep URL slugs as-is
app.MapControllerRoute(
    name: "localized_blog_detail_tr",
    pattern: "tr/blog/{id}/{slug?}",
    defaults: new { controller = "Home", action = "BlogDetails", culture = "tr" });

app.MapControllerRoute(
    name: "localized_blog_detail_en",
    pattern: "en/blog/{id}/{slug?}",
    defaults: new { controller = "Home", action = "BlogDetails", culture = "en" });

app.MapControllerRoute(
    name: "localized_blog_detail_de",
    pattern: "de/blog/{id}/{slug?}",
    defaults: new { controller = "Home", action = "BlogDetails", culture = "de" });

app.MapControllerRoute(
    name: "localized_blog_detail_fr",
    pattern: "fr/blog/{id}/{slug?}",
    defaults: new { controller = "Home", action = "BlogDetails", culture = "fr" });

app.MapControllerRoute(
    name: "localized_blog_detail_ar",
    pattern: "ar/blog/{id}/{slug?}",
    defaults: new { controller = "Home", action = "BlogDetails", culture = "ar" });

// Blog list routes
app.MapControllerRoute(
    name: "localized_blog_tr",
    pattern: "tr/blog",
    defaults: new { controller = "Home", action = "Blog", culture = "tr" });

app.MapControllerRoute(
    name: "localized_blog_en",
    pattern: "en/blog",
    defaults: new { controller = "Home", action = "Blog", culture = "en" });

app.MapControllerRoute(
    name: "localized_blog_de",
    pattern: "de/blog",
    defaults: new { controller = "Home", action = "Blog", culture = "de" });

app.MapControllerRoute(
    name: "localized_blog_fr",
    pattern: "fr/blog",
    defaults: new { controller = "Home", action = "Blog", culture = "fr" });

app.MapControllerRoute(
    name: "localized_blog_ar",
    pattern: "ar/blog",
    defaults: new { controller = "Home", action = "Blog", culture = "ar" });

// Contact routes - translated
app.MapControllerRoute(
    name: "localized_contact_tr",
    pattern: "tr/iletisim",
    defaults: new { controller = "Home", action = "Contact", culture = "tr" });

app.MapControllerRoute(
    name: "localized_contact_en",
    pattern: "en/contact",
    defaults: new { controller = "Home", action = "Contact", culture = "en" });

app.MapControllerRoute(
    name: "localized_contact_de",
    pattern: "de/kontakt",
    defaults: new { controller = "Home", action = "Contact", culture = "de" });

app.MapControllerRoute(
    name: "localized_contact_fr",
    pattern: "fr/contact",
    defaults: new { controller = "Home", action = "Contact", culture = "fr" });

app.MapControllerRoute(
    name: "localized_contact_ar",
    pattern: "ar/contact",
    defaults: new { controller = "Home", action = "Contact", culture = "ar" });

// About routes - translated
app.MapControllerRoute(
    name: "localized_about_tr",
    pattern: "tr/hakkimizda",
    defaults: new { controller = "Home", action = "About", culture = "tr" });

app.MapControllerRoute(
    name: "localized_about_en",
    pattern: "en/about",
    defaults: new { controller = "Home", action = "About", culture = "en" });

app.MapControllerRoute(
    name: "localized_about_de",
    pattern: "de/uber-uns",
    defaults: new { controller = "Home", action = "About", culture = "de" });

app.MapControllerRoute(
    name: "localized_about_fr",
    pattern: "fr/a-propos",
    defaults: new { controller = "Home", action = "About", culture = "fr" });

app.MapControllerRoute(
    name: "localized_about_ar",
    pattern: "ar/about",
    defaults: new { controller = "Home", action = "About", culture = "ar" });

// Privacy routes - translated
app.MapControllerRoute(
    name: "localized_privacy_tr",
    pattern: "tr/gizlilik",
    defaults: new { controller = "Home", action = "Privacy", culture = "tr" });

app.MapControllerRoute(
    name: "localized_privacy_en",
    pattern: "en/privacy",
    defaults: new { controller = "Home", action = "Privacy", culture = "en" });

app.MapControllerRoute(
    name: "localized_privacy_de",
    pattern: "de/datenschutz",
    defaults: new { controller = "Home", action = "Privacy", culture = "de" });

app.MapControllerRoute(
    name: "localized_privacy_fr",
    pattern: "fr/confidentialite",
    defaults: new { controller = "Home", action = "Privacy", culture = "fr" });

app.MapControllerRoute(
    name: "localized_privacy_ar",
    pattern: "ar/privacy",
    defaults: new { controller = "Home", action = "Privacy", culture = "ar" });

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

#region Worker Webhook API

// API endpoint for Cloudflare Worker to trigger user confirmation email
app.MapPost("/api/email/send-confirmation", async (HttpContext context, IEmailSender emailSender, IConfiguration configuration) =>
{
    try
    {
        // Verify request is from Cloudflare Worker (check secret token)
        var authHeader = context.Request.Headers["X-Worker-Secret"].FirstOrDefault();
        var expectedSecret = Environment.GetEnvironmentVariable("WORKER_SECRET") 
                          ?? configuration["Worker:Secret"];
        
        if (authHeader != expectedSecret)
        {
            Console.WriteLine("[API] ❌ Unauthorized webhook attempt");
            return Results.Unauthorized();
        }

        // Read request body
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(body);

        if (data == null || !data.ContainsKey("userEmail") || !data.ContainsKey("culture"))
        {
            Console.WriteLine("[API] ❌ Invalid request data");
            return Results.BadRequest(new { error = "Missing required fields" });
        }

        var userEmail = data["userEmail"];
        var culture = data["culture"];
        var userName = data.GetValueOrDefault("userName", "Customer");

        Console.WriteLine($"[API] ✅ Webhook received for user: {userEmail}, culture: {culture}");

        // Determine which template to use based on culture
        var templateFile = culture.ToLower() switch
        {
            "tr-tr" or "tr" => "UserNotification_tr.html",
            "de-de" or "de" => "UserNotification_de.html",
            "fr-fr" or "fr" => "UserNotification_fr.html",
            "ar-sa" or "ar" => "UserNotification_ar.html",
            _ => "UserNotification_en.html"
        };

        var subject = culture.ToLower() switch
        {
            "tr-tr" or "tr" => "Mesajınız Alındı - Alpha Ayakkabı",
            "de-de" or "de" => "Ihre Nachricht wurde empfangen - Alpha Ayakkabı",
            "fr-fr" or "fr" => "Votre message a été reçu - Alpha Ayakkabı",
            "ar-sa" or "ar" => "تم استلام رسالتك - Alpha Ayakkabı",
            _ => "Your Message Has Been Received - Alpha Ayakkabı"
        };

        // Read email template
        var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", templateFile);
        
        if (!System.IO.File.Exists(templatePath))
        {
            Console.WriteLine($"[API] ⚠️ Template not found: {templatePath}, using default");
            templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "UserNotification_en.html");
        }

        var emailBody = await System.IO.File.ReadAllTextAsync(templatePath);

        // Send confirmation email to user
        await emailSender.SendEmailAsync(userEmail, subject, emailBody);
        
        Console.WriteLine($"[API] ✅ Confirmation email sent to: {userEmail}");
        
        return Results.Ok(new { success = true, message = "Confirmation email sent" });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[API] ❌ Error: {ex.Message}");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

#endregion

app.Run();
