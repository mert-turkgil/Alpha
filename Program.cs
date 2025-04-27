using Data.Concrete.EfCore;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Alpha.Services;
using Alpha.Identity;
using Microsoft.AspNetCore.Identity;
using Alpha.EmailServices;
using Data.Abstract;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Alpha.Extensions;
using Alpha.Data;

var builder = WebApplication.CreateBuilder(args);
#region değişken
// Load configuration
var config = builder.Configuration;

// Configure EmailSender settings
var emailSection = config.GetSection("EmailSender");
var host     = emailSection.GetValue<string>("SMTPMail") 
               ?? throw new InvalidOperationException("SMTP host missing");
var port     = emailSection.GetValue<int>("Port");
var username = emailSection.GetValue<string>("Username") 
               ?? throw new InvalidOperationException("SMTP username missing");
var password = emailSection.GetValue<string>("Password") 
               ?? throw new InvalidOperationException("SMTP password missing");
var enablessl = emailSection.GetValue<bool>("enablessl");
#endregion
// Add connection strings
var shopConnection = builder.Configuration.GetConnectionString("ShopContext")
    ?? throw new InvalidOperationException("Connection string 'ShopContext' not found.");

var identityConnection = builder.Configuration.GetConnectionString("ApplicationContext")
    ?? throw new InvalidOperationException("Connection string 'ApplicationContext' not found.");

builder.Services.AddDbContext<ShopContext>(options => options.UseSqlServer(shopConnection));
builder.Services.AddDbContext<ApplicationContext>(options => options.UseSqlServer(identityConnection));
#region giriş
// Identity configuration
builder.Services.AddIdentity<User, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;

    // Password policy
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;

    // Lockout policy
    options.Lockout.MaxFailedAccessAttempts = 4;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(4);
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = 
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = true;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationContext>()
.AddDefaultTokenProviders();

// Configure cookies
builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = "/admin/login";
    options.LogoutPath = "/admin/logout";
    options.AccessDeniedPath = "/accessdenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.Cookie = new CookieBuilder {
        HttpOnly = true,
        Name = ".Alpha.Security.Cookie",
        SameSite = SameSiteMode.None,
        SecurePolicy = CookieSecurePolicy.Always
    };
});

#endregion
#region güvenlik
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = ".Alpha.AntiForgery";
});
#endregion
#region dil
// Localization
builder.Services.AddSingleton<LanguageService>();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddMvc()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        var assemblyInfo = typeof(SharedResource).GetTypeInfo().Assembly;

        if (assemblyInfo == null)
            throw new InvalidOperationException("Assembly information for SharedResource is missing.");

        var assemblyName = new AssemblyName(assemblyInfo.FullName ?? throw new InvalidOperationException("Assembly full name cannot be null."));
        
        if (string.IsNullOrEmpty(assemblyName.Name))
            throw new InvalidOperationException("Assembly name cannot be null or empty.");

        options.DataAnnotationLocalizerProvider = (type, factory) =>
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory), "Localizer factory cannot be null.");
            
            // Ensure location is not null
            var location = assemblyName.Name ?? throw new ArgumentNullException(nameof(assemblyName.Name), "Location cannot be null.");
            return factory.Create(nameof(SharedResource), location);
        };
    });


builder.Services.Configure<RequestLocalizationOptions>(options => {
    var supportedCultures = new List<CultureInfo> {
        new CultureInfo("fr-FR"),
        new CultureInfo("de-DE"),
        new CultureInfo("en-US"),
        new CultureInfo("tr-TR")
    };
    options.DefaultRequestCulture = new RequestCulture("tr-TR", "tr-TR");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
});
#endregion
#region Services
// Dependency injection for services
builder.Services.AddSingleton<AliveResourceService>(p =>
    new AliveResourceService(Path.Combine(Directory.GetCurrentDirectory(), "Resources")));
    
builder.Services.AddSingleton<IManageResourceService>(sp =>
    new ManageResourceService(Path.Combine(Directory.GetCurrentDirectory(), "Resources")));
builder.Services.AddScoped<INavbarService, NavbarService>();
builder.Services.AddScoped<IImageRepository, EfCoreImageRepository>();
builder.Services.AddScoped<IFooterService, FooterService>();
builder.Services.AddScoped<ICarouselRepository, EfCoreCarouselRepository>();
builder.Services.AddScoped<IBlogRepository, EfCoreBlogRepository>();
builder.Services.AddScoped<IProductRepository, EfCoreProductRepository>();
builder.Services.AddScoped<ICategoryRepository, EfCoreCategoryRepository>();
    if (string.IsNullOrEmpty(host))
        throw new ArgumentNullException(nameof(host), "SMTP host cannot be null or empty.");
    if (port <= 0)
        throw new ArgumentOutOfRangeException(nameof(port), "SMTP port must be a positive number.");
    if (string.IsNullOrEmpty(username))
        throw new ArgumentNullException(nameof(username), "SMTP username cannot be null or empty.");
    if (string.IsNullOrEmpty(password))
        throw new ArgumentNullException(nameof(password), "SMTP password cannot be null or empty.");

    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>(i =>
        new SmtpEmailSender(host, port, enablessl, username, password));

builder.Services.AddScoped<UserManager<User>>();

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Static file provider
string resourcesPath = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
builder.Services.AddSingleton<IFileProvider>(new PhysicalFileProvider(resourcesPath));
#endregion
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson()
    .AddViewLocalization()
        .AddDataAnnotationsLocalization()
            .AddRazorRuntimeCompilation();

var app = builder.Build();

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
            Console.WriteLine("⚠️ Root email is not provided in configuration. Skipping seeding.");
            return;
        }

        var rootUser = await userManager.FindByEmailAsync(rootEmail);
        if (rootUser != null)
        {
            Console.WriteLine($"✅ Root user '{rootEmail}' already exists. Skipping seeding.");
        }
        else
        {
            Console.WriteLine($"🔨 Root user '{rootEmail}' does not exist. Starting seeding...");
            await SeedIdentity.Seed(userManager, roleManager, configuration);
            Console.WriteLine("✅ Seeding completed.");
        }
    }
    else
    {
        Console.WriteLine("⚠️ No root user definition found in appsettings.json (Data:Users).");
    }
}




app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{action=Index}/{id?}",
    defaults: new { controller = "Admin", action = "Index" });

app.MapControllerRoute(
    name: "productDetail",
    pattern: "Product/Detail/{id}",
    defaults: new { controller = "Home", action = "ProductDetail" });



app.Run();
