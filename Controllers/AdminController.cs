using Alpha.EmailServices;
using Alpha.Entity;
using Alpha.Extensions;
using Alpha.Identity;
using Alpha.Models;
using Alpha.Services;
using Data.Abstract;
using Data.Concrete.EfCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.FileProviders;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Resources.NetStandard;
using System.Text.RegularExpressions;
using System.Xml.Linq;

[Authorize(Roles = "Admin,User")]
public class AdminController : Controller
{
    private readonly ILogger<AdminController> _logger;
    private readonly ShopContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ICarouselRepository _carouselRepository;
    private readonly SignInManager<User> _signInManager;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly IBlogRepository _blogRepository;
    private readonly IImageRepository _imageRepository;
    private readonly IEmailSender _emailSender;
    private readonly IWebHostEnvironment _env;
    private readonly AliveResourceService _dynamicResourceService;
    private readonly IManageResourceService _manageResourceService;
    private readonly IFileProvider _fileProvider;

    public AdminController(UserManager<User> userManager, SignInManager<User> signInManager,RoleManager<IdentityRole> roleManager,
                           ICategoryRepository categoryRepository, IProductRepository productRepository,
                           AliveResourceService dynamicResourceService,IManageResourceService manageResourceService,
                           IBlogRepository blogRepository, IImageRepository imageRepository,IEmailSender emailSender,
                           IFileProvider fileProvider,ICarouselRepository carouselRepository,
                           IWebHostEnvironment env,ShopContext dbContext,ILogger<AdminController> logger)
                        {
                            _logger = logger;
                            _dbContext = dbContext;
                            _userManager = userManager;
                            _roleManager = roleManager;
                            _signInManager = signInManager;
                            _categoryRepository = categoryRepository;
                            _productRepository = productRepository;
                            _blogRepository = blogRepository;
                            _imageRepository = imageRepository;
                            _emailSender = emailSender;
                            _fileProvider = fileProvider;
                            _dynamicResourceService = dynamicResourceService;
                            _manageResourceService = manageResourceService;
                            _env = env;
                            _carouselRepository = carouselRepository;
                        }
    
    #region Caraousel
     // List All Carousels
    [HttpGet("Admin/Carousels")]
    public async Task<IActionResult> Carousels()
    {
        var carousels = await _carouselRepository.GetAllAsync();
        return View(carousels);
    }

    [HttpGet("Admin/CarouselCreate")]
    public IActionResult CarouselCreate()
    {
        return View(new CarouselViewModel());
    }


    [HttpPost("Admin/CarouselCreate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CarouselCreate(CarouselViewModel model, [FromServices] IManageResourceService resourceService)
    {
        if (!ModelState.IsValid)
        {
            Console.WriteLine("[WARN] Model validation failed.");
            return View(model);
        }

        var carousel = new Carousel
        {
            CarouselTitle = model.CarouselTitle,
            CarouselDescription = model.CarouselDescription,
            CarouselLink = model.CarouselLink,
            CarouselLinkText = model.CarouselLinkText,
            DateAdded = DateTime.UtcNow,
        };

        try
        {
            // Handle image uploads
            carousel.CarouselImage = model.CarouselImage != null ? await SaveFile(model.CarouselImage) : string.Empty;
            carousel.CarouselImage600w = model.CarouselImage600w != null ? await SaveFile(model.CarouselImage600w) : string.Empty;
            carousel.CarouselImage1200w = model.CarouselImage1200w != null ? await SaveFile(model.CarouselImage1200w) : string.Empty;

            // Save to database and retrieve entity with assigned ID
            carousel = await _carouselRepository.CreateAndReturn(carousel);

            // Ensure the ID is valid
            int carouselId = carousel.CarouselId; // Correct ID from DB
            if (carouselId <= 0)
            {
                Console.WriteLine("[ERROR] Failed to generate a valid ID for carousel.");
                TempData["ErrorMessage"] = "An error occurred while creating the carousel.";
                return View(model);
            }

            Console.WriteLine($"[INFO] Carousel created successfully with ID: {carouselId}");

            // Save translations using the valid ID
            SaveAllTranslations(resourceService, carouselId, model);

            TempData["SuccessMessage"] = "Carousel created successfully!";
            return RedirectToAction("Carousels");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to create carousel. Exception: {ex.Message}");
            TempData["ErrorMessage"] = "An error occurred while creating the carousel.";
            return View(model);
        }
    }


        private void SaveAllTranslations(IManageResourceService resourceService, int baseKey, CarouselViewModel model)
        {
            // Validate baseKey
            if (baseKey <= 0)
            {
                Console.WriteLine("[ERROR] Invalid baseKey provided for translations.");
                return;
            }

            // Prepare translations
            var translations = new Dictionary<string, (string Title, string Description, string LinkText)>
            {
                { "en-US", (model.TranslationsUS.Title, model.TranslationsUS.Description, model.TranslationsUS.LinkText) },
                { "tr-TR", (model.TranslationsTR.Title, model.TranslationsTR.Description, model.TranslationsTR.LinkText) },
                { "de-DE", (model.TranslationsDE.Title, model.TranslationsDE.Description, model.TranslationsDE.LinkText) },
                { "fr-FR", (model.TranslationsFR.Title, model.TranslationsFR.Description, model.TranslationsFR.LinkText) },
            };

            foreach (var translation in translations)
            {
                var culture = translation.Key;
                var (title, description, linkText) = translation.Value;

                // Unique keys with carousel prefix
                string keyPrefix = $"Carousel_{baseKey}";

                if (!string.IsNullOrEmpty(title))
                    SaveTranslation(resourceService, $"{keyPrefix}_Title", title, culture);

                if (!string.IsNullOrEmpty(description))
                    SaveTranslation(resourceService, $"{keyPrefix}_Description", description, culture);

                if (!string.IsNullOrEmpty(linkText))
                    SaveTranslation(resourceService, $"{keyPrefix}_LinkText", linkText, culture);

                Console.WriteLine($"[INFO] Saved translations for culture '{culture}' with key prefix '{keyPrefix}'");
            }
        }



        private void SaveTranslation(IManageResourceService resourceService, string key, string value, string culture)
        {
            Console.WriteLine($"[DEBUG] Saving translation: Key='{key}', Value='{value}', Culture='{culture}'");

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value) || string.IsNullOrEmpty(culture))
            {
                Console.WriteLine($"[ERROR] Invalid translation data. Key: '{key}', Value: '{value}', Culture: '{culture}'");
                return;
            }

            resourceService.AddOrUpdateResource(key, value, culture); // Save key-value
        }



    [HttpGet]
    public async Task<IActionResult> CarouselEdit(int id, [FromServices] IManageResourceService manageResourceService)
    {
        Console.WriteLine($"[INFO] Loading edit view for Carousel ID: {id}");

        // Fetch carousel from database
        var carousel = await _carouselRepository.GetByIdAsync(id);
        if (carousel == null)
        {
            Console.WriteLine($"[WARN] Carousel with ID {id} not found.");
            return NotFound();
        }

        // Fetch translations with null checks and default values
        string GetTranslation(string key, string culture, string defaultValue)
        {
            var value = manageResourceService.ReadResourceValue(key, culture);
            if (string.IsNullOrEmpty(value))
            {
                Console.WriteLine($"[WARN] Resource key '{key}' not found in '{culture}' resource file.");
                return defaultValue; // Provide a fallback value if the key is missing
            }
            return value;
        }

        // Create model
        var model = new CarouselEditModel
        {
            CarouselId = carousel.CarouselId,
            CarouselTitle = carousel.CarouselTitle,
            CarouselDescription = carousel.CarouselDescription,
            CarouselLink = carousel.CarouselLink,
            CarouselLinkText = carousel.CarouselLinkText,
            DateAdded = carousel.DateAdded,
            CarouselImagePath = carousel.CarouselImage,
            CarouselImage1200wPath = carousel.CarouselImage1200w,
            CarouselImage600wPath= carousel.CarouselImage600w,

            // US Translations
            CarouselTitleUS = GetTranslation($"Carousel_{carousel.CarouselId}_Title", "en-US", carousel.CarouselTitle),
            CarouselDescriptionUS = GetTranslation($"Carousel_{carousel.CarouselId}_Description", "en-US", carousel.CarouselDescription),
            CarouselLinkTextUS = GetTranslation($"Carousel_{carousel.CarouselId}_LinkText", "en-US", carousel.CarouselLinkText),

            // TR Translations
            CarouselTitleTR = GetTranslation($"Carousel_{carousel.CarouselId}_Title", "tr-TR", carousel.CarouselTitle),
            CarouselDescriptionTR = GetTranslation($"Carousel_{carousel.CarouselId}_Description", "tr-TR", carousel.CarouselDescription),
            CarouselLinkTextTR = GetTranslation($"Carousel_{carousel.CarouselId}_LinkText", "tr-TR", carousel.CarouselLinkText),

            // DE Translations
            CarouselTitleDE = GetTranslation($"Carousel_{carousel.CarouselId}_Title", "de-DE", carousel.CarouselTitle),
            CarouselDescriptionDE = GetTranslation($"Carousel_{carousel.CarouselId}_Description", "de-DE", carousel.CarouselDescription),
            CarouselLinkTextDE = GetTranslation($"Carousel_{carousel.CarouselId}_LinkText", "de-DE", carousel.CarouselLinkText),

            // FR Translations
            CarouselTitleFR = GetTranslation($"Carousel_{carousel.CarouselId}_Title", "fr-FR", carousel.CarouselTitle),
            CarouselDescriptionFR = GetTranslation($"Carousel_{carousel.CarouselId}_Description", "fr-FR", carousel.CarouselDescription),
            CarouselLinkTextFR = GetTranslation($"Carousel_{carousel.CarouselId}_LinkText", "fr-FR", carousel.CarouselLinkText),
        };

        Console.WriteLine("[INFO] Loaded carousel and translations successfully.");
        return View(model);
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CarouselEdit(CarouselEditModel model, [FromServices] IManageResourceService manageResourceService)
    {
        Console.WriteLine($"[INFO] Processing update for Carousel ID: {model.CarouselId}");

        if (!ModelState.IsValid)
        {
            Console.WriteLine("[WARN] Model validation failed.");
            return View(model);
        }

        var carousel = await _carouselRepository.GetByIdAsync(model.CarouselId);
        if (carousel == null)
        {
            Console.WriteLine($"[WARN] Carousel with ID {model.CarouselId} not found.");
            return NotFound();
        }

        // Update properties
        carousel.CarouselTitle = model.CarouselTitle;
        carousel.CarouselDescription = model.CarouselDescription;
        carousel.CarouselLink = model.CarouselLink;
        carousel.CarouselLinkText = model.CarouselLinkText;

        // Image validation and update
        if (model.CarouselImage != null)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(model.CarouselImage.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension) || model.CarouselImage.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError("CarouselImage", "Invalid image format or size.");
                return View(model);
            }

            string newImagePath = await SaveFile(model.CarouselImage);
            if (!string.IsNullOrEmpty(carousel.CarouselImage))
            {
                var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", carousel.CarouselImage);
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                    Console.WriteLine($"[INFO] Deleted old image: {oldImagePath}");
                }
            }
            carousel.CarouselImage = newImagePath;
            Console.WriteLine($"[INFO] Updated image path: {newImagePath}");
        }

            // Image validation and update CarouselImage1200w
        if (model.CarouselImage1200w != null)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(model.CarouselImage1200w.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension) || model.CarouselImage1200w.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError("CarouselImage1200w", "Invalid image format or size.");
                return View(model);
            }

            string newImagePath1200w = await SaveFile(model.CarouselImage1200w);
            if (!string.IsNullOrEmpty(carousel.CarouselImage1200w))
            {
                var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", carousel.CarouselImage1200w);
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                    Console.WriteLine($"[INFO] Deleted old image: {oldImagePath}");
                }
            }
            carousel.CarouselImage1200w = newImagePath1200w;
            Console.WriteLine($"[INFO] Updated image path: {newImagePath1200w}");
        }

        // Image validation and update CarouselImage600w
        if (model.CarouselImage600w != null)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(model.CarouselImage600w.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension) || model.CarouselImage600w.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError("CarouselImage600w", "Invalid image format or size.");
                return View(model);
            }

            string newImagePath600w = await SaveFile(model.CarouselImage600w);
            if (!string.IsNullOrEmpty(carousel.CarouselImage600w))
            {
                var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", carousel.CarouselImage600w);
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                    Console.WriteLine($"[INFO] Deleted old image: {oldImagePath}");
                }
            }
            carousel.CarouselImage600w = newImagePath600w;
            Console.WriteLine($"[INFO] Updated image path: {newImagePath600w}");
        }

        // Update translations
        UpdateTranslations(manageResourceService, carousel.CarouselId, model);

        // Save the carousel
        await _carouselRepository.UpdateAsync(carousel);
        Console.WriteLine($"[INFO] Carousel with ID {model.CarouselId} updated successfully!");
        TempData["SuccessMessage"] = "Carousel updated successfully!";
        return RedirectToAction("Carousels");
    }
private void UpdateTranslations(IManageResourceService manageResourceService, int id, CarouselEditModel model)
{
    var baseKey = $"Carousel_{id}";

    // US Translations
    manageResourceService.AddOrUpdateResource($"{baseKey}_Title", model.CarouselTitleUS, "en-US");
    manageResourceService.AddOrUpdateResource($"{baseKey}_Description", model.CarouselDescriptionUS, "en-US");
    manageResourceService.AddOrUpdateResource($"{baseKey}_LinkText", model.CarouselLinkTextUS, "en-US");

    // TR Translations
    manageResourceService.AddOrUpdateResource($"{baseKey}_Title", model.CarouselTitleTR, "tr-TR");
    manageResourceService.AddOrUpdateResource($"{baseKey}_Description", model.CarouselDescriptionTR, "tr-TR");
    manageResourceService.AddOrUpdateResource($"{baseKey}_LinkText", model.CarouselLinkTextTR, "tr-TR");

    // DE Translations
    manageResourceService.AddOrUpdateResource($"{baseKey}_Title", model.CarouselTitleDE, "de-DE");
    manageResourceService.AddOrUpdateResource($"{baseKey}_Description", model.CarouselDescriptionDE, "de-DE");
    manageResourceService.AddOrUpdateResource($"{baseKey}_LinkText", model.CarouselLinkTextDE, "de-DE");

    // FR Translations
    manageResourceService.AddOrUpdateResource($"{baseKey}_Title", model.CarouselTitleFR, "fr-FR");
    manageResourceService.AddOrUpdateResource($"{baseKey}_Description", model.CarouselDescriptionFR, "fr-FR");
    manageResourceService.AddOrUpdateResource($"{baseKey}_LinkText", model.CarouselLinkTextFR, "fr-FR");

    Console.WriteLine($"[INFO] Updated translations for Carousel_{id}.");
}


        // POST: Delete Carousel
        [HttpPost]
        public async Task<IActionResult> CarouselDelete(int id)
        {
            Console.WriteLine($"[INFO] Starting deletion process for Carousel ID: {id}");

            // Step 1: Retrieve the carousel
            var carousel = await _carouselRepository.GetByIdAsync(id);
            if (carousel == null)
            {
                Console.WriteLine($"[WARN] Carousel with ID {id} not found.");
                TempData["ErrorMessage"] = "Carousel not found.";
                return RedirectToAction("Carousels");
            }

            Console.WriteLine($"[INFO] Found Carousel - Title: {carousel.CarouselTitle}");

            try
            {
                // Step 2: Delete associated files
                DeleteFile(carousel.CarouselImage, "CarouselImage");
                DeleteFile(carousel.CarouselImage600w, "CarouselImage600w");
                DeleteFile(carousel.CarouselImage1200w, "CarouselImage1200w");

                // Step 3: Delete translations
                var baseKey = $"Carousel_{carousel.CarouselId}";

                // Languages
                string[] cultures = { "en-US", "tr-TR", "de-DE", "fr-FR" };
                string[] keys = { "Title", "Description", "LinkText" };

                foreach (var culture in cultures)
                {
                    foreach (var key in keys)
                    {
                        string resourceKey = $"{baseKey}_{key}";
                        _manageResourceService.DeleteResource(resourceKey, culture);
                        Console.WriteLine($"[INFO] Deleted translation: {resourceKey} in {culture}");
                    }
                }

                // Step 4: Delete the carousel from the database
                await _carouselRepository.DeleteAsync(id);
                Console.WriteLine($"[INFO] Carousel with ID {id} deleted successfully!");

                TempData["SuccessMessage"] = "Carousel deleted successfully!";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to delete carousel with ID {id}. Exception: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while deleting the carousel.";
            }

            // Step 5: Redirect to the carousels page
            Console.WriteLine("[INFO] Redirecting to Carousels page...");
            return RedirectToAction("Carousels");
        }

        private void DeleteFile(string filePath, string fileType)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                var fullPath = Path.Combine(_env.WebRootPath, "img", filePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    try
                    {
                        System.IO.File.Delete(fullPath);
                        Console.WriteLine($"[INFO] Deleted {fileType} file: {fullPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Failed to delete {fileType} file: {fullPath}. Exception: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"[WARN] {fileType} file not found at: {fullPath}");
                }
            }
            else
            {
                Console.WriteLine($"[INFO] No {fileType} file associated.");
            }
        }



    // Helper to save files
    private async Task<string> SaveFile(IFormFile file)
    {
        var uploadsFolder = Path.Combine(_env.WebRootPath, "img");
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        Directory.CreateDirectory(uploadsFolder);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"{fileName}";
    }

    private void DeleteFile(string fileUrl)
    {
        if (!string.IsNullOrEmpty(fileUrl))
        {
            var filePath = Path.Combine(_env.WebRootPath, fileUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }



    #endregion

    #region Products

    public async Task<IActionResult> Products()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }

    [HttpGet]
    public async Task<IActionResult> ProductCreate()
    {
        var categories = await _categoryRepository.GetAllAsync();
        ViewBag.Categories = categories.Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                }).ToList();
        var availableLanguages = await Task.FromResult(new List<string> { "en-US", "de-DE", "fr-FR", "tr-TR" });
        ViewBag.AvailableLanguages = availableLanguages;
        return View(new ProductCreateModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductCreate(ProductCreateModel e)
    {
        if (ModelState.IsValid)
        {
            // Step 1: Save the Product
            var product = new Product
            {
                Name = e.Name,
                BodyNo = e.BodyNo,
                Url = e.Url,
                Upper = e.Upper,
                Lining = e.Lining,
                Protection = e.Protection,
                Brand = e.Brand,
                Standard = e.Standard,
                Midsole = e.Midsole,
                Insole = e.Insole,
                Certificate = e.Certificate,
                Size = e.Size,
                Model = e.Model,
                Sole = e.Sole,
                Description = e.Description,
                DateAdded = DateTime.UtcNow,
                CategoryId = e.CategoryId
            };

            // Save product and ensure ProductId is available
            await _productRepository.CreateAsync(product);

            if (product.ProductId == 0) // Ensure the ProductId was generated
            {
                TempData["AlertMessage"] = new AlertMessage
                {
                    Title = "Error",
                    Message = "Failed to create product. Product ID was not generated.",
                    AlertType = "danger",
                    icon = "fas fa-bug",
                    icon2 = "fas fa-times"
                };
                return View(e);
            }
            else
            {
                // Add translations
                AddProductTranslations(product.ProductId, e);
            }

            TempData["SuccessMessage"] = "Product and translations created successfully!";
            return RedirectToAction("Products");
        }

        ViewBag.Categories = (await _categoryRepository.GetAllAsync())
            .Select(c => new SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.Name
            }).ToList();

        ViewBag.AvailableLanguages = new List<string> { "en-US", "de-DE", "fr-FR", "tr-TR" };

        return View(e);
    }

    private void AddProductTranslations(int productId, ProductCreateModel model)
    {
        // Supported Languages and Fields
        var cultures = new[] { "fr-FR", "en-US", "de-DE", "tr-TR" };
        var fields = new Dictionary<string, string>
        {
            { "Description", model.Description },
            { "Upper", model.Upper },
            { "Lining", model.Lining },
            { "Protection", model.Protection },
            { "Midsole", model.Midsole },
            { "Insole", model.Insole },
            { "Sole", model.Sole }
        };

        // Process each culture and field
        foreach (var culture in cultures)
        {
            foreach (var field in fields)
            {
                var resourceKey = $"Product_{productId}_{field.Key}_{culture}";

                // Dynamically get the translation property value
                var value = GetPropertyValue(model, $"{field.Key}{culture.Split('-')[1]}");

                if (!string.IsNullOrWhiteSpace(value))
                {
                    try
                    {
                        _manageResourceService.AddOrUpdateResource(resourceKey, value, culture);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to save translation for {resourceKey}: {ex.Message}");
                    }
                }
            }
        }
    }

    // Helper Method for Reflection
    private string GetPropertyValue(object obj, string propName)
    {
        var property = obj.GetType().GetProperty(propName);
        return property?.GetValue(obj)?.ToString() ?? string.Empty; // Ensure non-null return
    }




[HttpGet]
public async Task<IActionResult> ProductEdit(int id)
{
    // Step 1: Fetch the product by ID with related data (Images, Categories)
    var product = await _productRepository.GetByIdAsync(id);
    if (product == null) return NotFound("Product not found.");

    // Step 2: Populate the ProductEditModel with product fields
    var model = new ProductEditModel
    {
        ProductId = product.ProductId,
        Name = product.Name,
        BodyNo = product.BodyNo,
        Url = product.Url,
        Upper = product.Upper,
        Lining = product.Lining,
        Protection = product.Protection,
        Midsole = product.Midsole,
        Insole = product.Insole,
        Sole = product.Sole,
        Description = product.Description,
        CategoryId = product.CategoryId,

        // Step 3: Populate Translations for all supported fields and cultures
        DescriptionFR = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Description_fr-FR", "fr-FR"),
        UpperFR = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Upper_fr-FR", "fr-FR"),
        LiningFR = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Lining_fr-FR", "fr-FR"),
        ProtectionFR = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Protection_fr-FR", "fr-FR"),
        MidsoleFR = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Midsole_fr-FR", "fr-FR"),
        InsoleFR = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Insole_fr-FR", "fr-FR"),
        SoleFR = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Sole_fr-FR", "fr-FR"),

        DescriptionUS = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Description_en-US", "en-US"),
        UpperUS = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Upper_en-US", "en-US"),
        LiningUS = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Lining_en-US", "en-US"),
        ProtectionUS = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Protection_en-US", "en-US"),
        MidsoleUS = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Midsole_en-US", "en-US"),
        InsoleUS = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Insole_en-US", "en-US"),
        SoleUS = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Sole_en-US", "en-US"),

        DescriptionDE = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Description_de-DE", "de-DE"),
        UpperDE = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Upper_de-DE", "de-DE"),
        LiningDE = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Lining_de-DE", "de-DE"),
        ProtectionDE = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Protection_de-DE", "de-DE"),
        MidsoleDE = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Midsole_de-DE", "de-DE"),
        InsoleDE = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Insole_de-DE", "de-DE"),
        SoleDE = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Sole_de-DE", "de-DE"),

        DescriptionTR = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Description_tr-TR", "tr-TR"),
        UpperTR = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Upper_tr-TR", "tr-TR"),
        LiningTR = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Lining_tr-TR", "tr-TR"),
        ProtectionTR = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Protection_tr-TR", "tr-TR"),
        MidsoleTR = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Midsole_tr-TR", "tr-TR"),
        InsoleTR = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Insole_tr-TR", "tr-TR"),
        SoleTR = _manageResourceService.ReadResourceValue($"Product_{product.ProductId}_Sole_tr-TR", "tr-TR"),

        // Step 4: Populate Selected Categories, Images, and Blogs
        CategoryIds = product.ProductCategories?.Select(pc => pc.CategoryId).ToList() ?? new List<int>(),
        ImageIds = product.ProductImages?.Select(pi => pi.ImageId).ToList() ?? new List<int>(),
        BlogIds = product.ProductBlogs?.Select(pb => pb.BlogId).ToList() ?? new List<int>(),

        // Fetch available data
        AvailableImages = await _imageRepository.GetAllAsync(),
        AvailableCategories = await _categoryRepository.GetAllAsync(),
        AvailableBlogs = (await _blogRepository.GetAllAsync())
                            .Select(b => new SelectListItem
                            {
                                Value = b.BlogId.ToString(),
                                Text = b.Title // Replace 'Title' with the correct property name
                            }).ToList(),

        // Step 6: Populate Current Images
        CurrentImages = product.ProductImages?
              .Where(pi => pi.Image != null)
              .Select(pi => pi.Image)
              .ToList() ?? new List<Image>()
    };

    return View(model);
}



[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ProductEdit(ProductEditModel e)
{
    Console.WriteLine($"[START] ProductEdit: Processing product ID {e.ProductId}");

    if (e.ProductId <= 0 || !ModelState.IsValid)
    {
        Console.WriteLine("[ERROR] Invalid product ID or invalid model state.");
        e.CurrentImages = await _imageRepository.GetImagesByProductIdAsync(e.ProductId);
        e.AvailableImages = await _imageRepository.GetAllAsync();
        e.AvailableCategories = await _categoryRepository.GetAllAsync();
        e.AvailableBlogs = (await _blogRepository.GetAllAsync())
                            .Select(b => new SelectListItem
                            {
                                Value = b.BlogId.ToString(),
                                Text = b.Title
                            }).ToList();
        return View(e);
    }

    var product = await _productRepository.GetByIdAsync(e.ProductId);
    if (product == null)
    {
        Console.WriteLine($"[ERROR] Product with ID {e.ProductId} not found.");
        return NotFound();
    }

    // Update Product Details
    product.Name = e.Name;
    product.Description = e.Description;
    product.Url = e.Url;
    product.BodyNo = e.BodyNo;
    product.Upper = e.Upper;
    product.Lining = e.Lining;
    product.Protection = e.Protection;
    product.Midsole = e.Midsole;
    product.Insole = e.Insole;
    product.Sole = e.Sole;

    // Update Categories
    product.ProductCategories.Clear();
    if (e.CategoryIds != null)
    {
        foreach (var categoryId in e.CategoryIds)
        {
            product.ProductCategories.Add(new ProductCategory
            {
                ProductId = product.ProductId,
                CategoryId = categoryId
            });
        }
    }

    // Update Images
    product.ProductImages.Clear();
    if (e.ImageIds != null)
    {
        foreach (var imageId in e.ImageIds)
        {
            product.ProductImages.Add(new ProductImage
            {
                ProductId = product.ProductId,
                ImageId = imageId
            });
        }

    }

    // Update Blogs
    product.ProductBlogs.Clear();
    if (e.BlogIds != null)
    {
        foreach (var blogId in e.BlogIds)
        {
            product.ProductBlogs.Add(new ProductBlog
            {
                ProductId = product.ProductId,
                BlogId = blogId
            });
        }

    }

    // Update Translations
    UpdateProductTranslations(e, product.ProductId);

    // Save Changes
    await _productRepository.UpdateAsync(product);
    Console.WriteLine($"[SUCCESS] Product ID {e.ProductId} updated successfully.");

    TempData["SuccessMessage"] = "Product updated successfully!";
    return RedirectToAction("Products");
}

    /// <summary>
    /// Updates translations for the product using dynamic properties.
    /// </summary>
private void UpdateProductTranslations(ProductEditModel model, int productId)
{
    var cultures = new[] { "fr-FR", "en-US", "de-DE", "tr-TR" };
    var fields = new Dictionary<string, string>
    {
        { "Description", model.Description },
        { "Upper", model.Upper },
        { "Lining", model.Lining },
        { "Protection", model.Protection },
        { "Midsole", model.Midsole },
        { "Insole", model.Insole },
        { "Sole", model.Sole }
    };

    foreach (var culture in cultures)
    {
        foreach (var field in fields)
        {
            var resourceKey = $"Product_{productId}_{field.Key}_{culture}";
            var value = GetPropertyValue(model, $"{field.Key}{culture.Split('-')[1]}");

            if (!string.IsNullOrWhiteSpace(value))
            {
                try
                {
                    _manageResourceService.AddOrUpdateResource(resourceKey, value, culture);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to update translation for {resourceKey}: {ex.Message}");
                }
            }
        }
    }
}



[HttpPost]
public async Task<IActionResult> ProductDelete(int id)
{
    using var transaction = await _dbContext.Database.BeginTransactionAsync();

    try
    {
        // Step 1: Delete translations first
        DeleteProductTranslations(id); // Ensure translations are removed before the product

        // Step 2: Delete product categories and images
        var product = await _productRepository.GetByIdAsync(id);

        if (product != null)
        {
            product.ProductCategories.Clear();
            product.ProductImages.Clear();
            await _productRepository.UpdateAsync(product); // Save changes before deleting
        }

        // Step 3: Delete the product
        await _productRepository.DeleteAsync(id);

        // Commit transaction if all operations succeed
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = "Product and its translations were successfully deleted.";
        return RedirectToAction("Products");
    }
    catch (Exception ex)
    {
        // Rollback transaction on failure
        await transaction.RollbackAsync();
        Console.WriteLine($"Failed to delete product: {ex.Message}");

        TempData["ErrorMessage"] = "Failed to delete product. Please try again.";
        return RedirectToAction("Products");
    }
}


        /// <summary>
        /// Deletes all translations for a given product based on its ID.
        /// </summary>
        /// <param name="productId">The ID of the product whose translations will be deleted.</param>
private void DeleteProductTranslations(int productId)
{
    var cultures = new[] { "fr-FR", "en-US", "de-DE", "tr-TR" };
    var fields = new[] { "Upper", "Lining", "Protection", "Midsole", "Insole", "Sole" };

    foreach (var culture in cultures)
    {
        foreach (var field in fields)
        {
            var resourceKey = $"Product_{productId}_{field}_{culture}";
            try
            {
                // Check if the resource exists before attempting deletion
                var value = _manageResourceService.ReadResourceValue(resourceKey, culture);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _manageResourceService.DeleteResource(resourceKey, culture);
                    Console.WriteLine($"Deleted translation: {resourceKey}");
                }
                else
                {
                    Console.WriteLine($"Skipping deletion: Resource {resourceKey} not found.");
                }
            }
            catch (IOException ioEx)
            {
                // Handle file locking issues
                Console.WriteLine($"File is locked for resource {resourceKey}: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete translation for {resourceKey}: {ex.Message}");
            }
        }
    }
}
private void SafeDeleteResource(string key, string culture)
{
    int retries = 3;

    while (retries > 0)
    {
        try
        {
            _manageResourceService.DeleteResource(key, culture);
            Console.WriteLine($"Deleted resource: {key} in {culture}");
            break; // Exit loop if successful
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Retrying deletion for {key}. Attempt {3 - retries + 1}: {ex.Message}");
            Thread.Sleep(500); // Wait before retrying
            retries--;
        }
    }

    if (retries == 0)
    {
        Console.WriteLine($"Failed to delete {key} after 3 attempts.");
    }
}


        #endregion



#region Localization Management
        [HttpGet]
        public IActionResult Localization(string lang = "de-DE")
        {
            var resxPath = GetResxPath(lang);
            var translations = LoadTranslations(resxPath);

            ViewBag.CurrentLanguage = lang;
            ViewBag.AvailableLanguages = new List<string> { "de-DE", "en-US", "fr-FR", "tr-TR" };
            return View(translations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditTranslation(string name, string value,
                                            string comment, string lang)
        {
            if (string.IsNullOrEmpty(name))
                return RedirectToAction(nameof(Localization), new { lang, e = "KeyMissing" });

            var resxPath = GetResxPath(lang);
            if (!System.IO.File.Exists(resxPath))
                return RedirectToAction(nameof(Localization), new { lang, e = "FileMissing" });

            var resx = XDocument.Load(resxPath);
            var data = resx.Root?
                    .Elements("data")
                    .FirstOrDefault(x => x.Attribute("name")?.Value == name);

            if (data is null)
                return RedirectToAction(nameof(Localization), new { lang, e = "KeyNotFound" });

            data.SetElementValue("value",  value  ?? string.Empty);
            data.SetElementValue("comment", comment ?? string.Empty);
            resx.Save(resxPath);
            _dynamicResourceService.ReloadResources();
            TempData["Success"] = "Translation updated ✔";     // razor’da gösterin
            return RedirectToAction(nameof(Localization), new { lang });
        }

        private string GetResxPath(string lang)
        {
            var fileName = $"SharedResource.{lang}.resx";
            return Path.Combine(Directory.GetCurrentDirectory(), "Resources", fileName);
        }

        private List<LocalizationModel> LoadTranslations(string resxPath)
        {
            var resxFile = XDocument.Load(resxPath);

            // Check if root is null
            if (resxFile.Root == null)
                return new List<LocalizationModel>();

            return resxFile.Root.Elements("data")
                .Select(x => new LocalizationModel
                {
                    Key = x.Attribute("name")?.Value ?? "N/A",
                    Value = x.Element("value")?.Value ?? "N/A",
                    Comment = x.Element("comment")?.Value ?? "N/A"
                })
                .ToList();
        }
        #endregion


#region Users
// Create User Method
[HttpGet("Admin/UserCreate")]
public IActionResult UserCreate()
{
    return View(new UserCreateModel { AllRoles = _roleManager.Roles.Select(r => r.Name).ToList() });
}

[HttpPost("Admin/UserCreate")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UserCreate(UserCreateModel model)
{
    if (!ModelState.IsValid)
    {
        TempData["ErrorMessage"] = "Invalid input. Please check your details.";
        model.AllRoles = _roleManager.Roles.Select(r => r.Name).ToList();
        return View(model);
    }

    // Check if email already exists
    var existingUser = await _userManager.FindByEmailAsync(model.Email);
    if (existingUser != null)
    {
        TempData["ErrorMessage"] = "This email is already registered.";
        model.AllRoles = _roleManager.Roles.Select(r => r.Name).ToList();
        return View(model);
    }

    // Create new user
    var user = new User
    {
        UserName = model.UserName,
        Email = model.Email,
        FirstName = model.FirstName,
        LastName = model.LastName,
        EmailConfirmed = model.EmailConfirmed
    };

    var result = await _userManager.CreateAsync(user, model.Password);

    if (result.Succeeded)
    {
        // Assign Roles
        await _userManager.AddToRolesAsync(user, model.SelectedRoles);

        TempData["SuccessMessage"] = "User created successfully!";
        return RedirectToAction("Users");
    }

    foreach (var error in result.Errors)
    {
        ModelState.AddModelError("", error.Description);
    }

    model.AllRoles = _roleManager.Roles.Select(r => r.Name).ToList();
    return View(model);
}


// Edit User Method
[HttpGet("Admin/UserEdit/{id}")]
[Authorize(Roles = "Admin,User")]
public async Task<IActionResult> UserEdit(string id)
{
    var user = await _userManager.FindByIdAsync(id);
    if (user == null)
    {
        TempData["ErrorMessage"] = "User not found.";
        return RedirectToAction("Users");
    }

    var currentUser = await _userManager.GetUserAsync(User!);
    var currentRoles = await _userManager.GetRolesAsync(currentUser!);

    // Only Admins can edit Admins
    var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
    var currentUserIsAdmin = currentRoles.Contains("Admin");

    if (isAdmin && !currentUserIsAdmin)
    {
        TempData["ErrorMessage"] = "You do not have permission to edit this user.";
        return RedirectToAction("Users");
    }

    var model = new UserEditModel
    {
        UserId = user.Id,
        UserName = user.UserName,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        EmailConfirmed = user.EmailConfirmed,
        SelectedRoles = (await _userManager.GetRolesAsync(user)).ToList(),
        AllRoles = _roleManager.Roles.Select(r => r.Name).ToList()
    };

    return View(model);
}

[HttpPost("Admin/UserEdit/{id}")]
[Authorize(Roles = "Admin,User")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UserEdit(UserEditModel model)
{
    if (!ModelState.IsValid)
    {
        model.AllRoles = _roleManager.Roles.Select(r => r.Name).ToList();
        return View(model);
    }

    var user = await _userManager.FindByIdAsync(model.UserId);
    if (user == null)
    {
        TempData["ErrorMessage"] = "User not found.";
        return RedirectToAction("Users");
    }

    var currentUser = await _userManager.GetUserAsync(User!);
    var currentRoles = await _userManager.GetRolesAsync(currentUser!);

    // Only Admins can edit Admins
    var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
    var currentUserIsAdmin = currentRoles.Contains("Admin");

    if (isAdmin && !currentUserIsAdmin)
    {
        TempData["ErrorMessage"] = "You do not have permission to edit this user.";
        return RedirectToAction("Users");
    }

    user.UserName = model.UserName;
    user.FirstName = model.FirstName;
    user.LastName = model.LastName;
    user.Email = model.Email;
    user.EmailConfirmed = model.EmailConfirmed;

    var result = await _userManager.UpdateAsync(user);

    if (result.Succeeded)
    {
        // Update Roles
        var currentRolesForUser = await _userManager.GetRolesAsync(user!);
        await _userManager.RemoveFromRolesAsync(user, currentRolesForUser);
        await _userManager.AddToRolesAsync(user, model.SelectedRoles);

        TempData["SuccessMessage"] = "User updated successfully!";
        return RedirectToAction("Users");
    }

    foreach (var error in result.Errors)
    {
        ModelState.AddModelError("", error.Description);
    }

    model.AllRoles = _roleManager.Roles.Select(r => r.Name).ToList();
    return View(model);
}


        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = _userManager.Users.ToList();
            var userRoles = new Dictionary<string, List<string>>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.ToList();
            }

            ViewBag.UserRoles = userRoles;
            return View(users);
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UserDelete(string id)
        {
            // Find the user by ID
            var user = await _userManager.FindByIdAsync(id);

            if (user != null)
            {
                // Check if the user is an Admin
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin"))
                {
                    TempData["ErrorMessage"] = "You cannot delete an Admin user.";
                    return RedirectToAction("Users");
                }

                // Proceed to delete the user
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "User deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error occurred while deleting the user.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "User not found.";
            }

            return RedirectToAction("Users");
        }


#endregion

#region  üyelik şifre değiştirme
[HttpGet]
[AllowAnonymous]
public IActionResult Register(string token, string email, string expiration)
{
    if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(expiration))
    {
        return BadRequest("Invalid invitation link.");
    }

    if (!DateTime.TryParse(expiration, out var expirationDate) || DateTime.UtcNow > expirationDate)
    {
        return BadRequest("This invitation has expired.");
    }

    var model = new RegisterModel { Email = email };
    return View(model);
}

[HttpPost]
[AllowAnonymous]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Register(RegisterModel model)
{
    if (!ModelState.IsValid)
    {
        TempData["ErrorMessage"] = "Invalid form data. Please check your inputs.";
        return View(model);
    }

    // Check for existing user
    var user = await _userManager.FindByEmailAsync(model.Email);
    if (user == null)
    {
        TempData["ErrorMessage"] = "Invalid invitation. User does not exist.";
        return RedirectToAction("Login", "Admin");
    }

    if (user.EmailConfirmed)
    {
        TempData["ErrorMessage"] = "This invitation has already been used.";
        return RedirectToAction("Login", "Admin");
    }

    // Update user details
    user.UserName = model.UserName;
    user.FirstName = model.FirstName;
    user.LastName = model.LastName;

    // Remove any existing password to avoid conflicts
    var removePasswordResult = await _userManager.RemovePasswordAsync(user);
    if (!removePasswordResult.Succeeded)
    {
        AddErrors(removePasswordResult);
        return View(model);
    }

    // Set a new password for the user
    var addPasswordResult = await _userManager.AddPasswordAsync(user, model.Password);
    if (!addPasswordResult.Succeeded)
    {
        AddErrors(addPasswordResult);
        return View(model);
    }

    // Confirm email to finalize registration
    user.EmailConfirmed = true;
    await _userManager.UpdateAsync(user);

    TempData["SuccessMessage"] = "Registration completed successfully! You can now log in.";
    return RedirectToAction("Login", "Admin");
}

private void AddErrors(IdentityResult result)
{
    foreach (var error in result.Errors)
    {
        ModelState.AddModelError(string.Empty, error.Description);
    }
}




    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login");

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Password changed successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Error changing password. Please check your inputs.";
        }

        return RedirectToAction("Dashboard");
    }
    #endregion




#region Dashboard

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var alphaInfo = new Alpha.Models.AlphaInfo
        {
            Name = "Alpha Safety Shoes",
            Version = "1.0.0",
            Date = DateTime.UtcNow,
            Description = "Alpha Safety Shoes are produced with cutting-edge technology to ensure safety and comfort.",
            IsActive = true,
            Message = "Committed to safety and innovation."
        };
        ViewBag.TotalProducts = (await _productRepository.GetAllAsync()).Count;
        ViewBag.TotalCategories = (await _categoryRepository.GetAllAsync()).Count;
        ViewBag.TotalBlogs = (await _blogRepository.GetAllAsync()).Count;
        ViewBag.TotalUsers = ( _userManager.Users.ToList()).Count;
        ViewBag.TotalCarousel = (await _carouselRepository.GetAllAsync()).Count;
        var translations = new Dictionary<string, string>
        {
            { "HomeBlogHead", "Expert tips and essential guidance" },
            { "WelcomeMessage", "Welcome to Alpha Dashboard" }
        };

        ViewBag.Translations = translations;
        var user = await _userManager.GetUserAsync(User);
        return View(alphaInfo);
    }
    

    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Admin");
    }


    #endregion





#region Category Management
        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return View(categories);
        }


        [HttpGet]
        public IActionResult CategoryCreate() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CategoryCreate(CategoryEditModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Handle Image Upload
                    string? imagePath = null;
                    if (model.ImageFile != null)
                    {
                        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif" };
                        var fileExtension = Path.GetExtension(model.ImageFile.FileName).ToLower();

                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("ImageFile", "Only PNG, JPG, and GIF files are allowed.");
                            return View(model);
                        }

                        if (model.ImageFile.Length > 1024 * 1000) // Limit file size to 100KB
                        {
                            ModelState.AddModelError("ImageFile", "File size must not exceed 100KB.");
                            return View(model);
                        }

                        // Generate unique file name using GUID
                        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                        var uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img");
                        if (!Directory.Exists(uploadDirectory))
                        {
                            Directory.CreateDirectory(uploadDirectory);
                        }

                        var filePath = Path.Combine(uploadDirectory, uniqueFileName);

                        // Save the file
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.ImageFile.CopyToAsync(stream);
                        }

                        // Assign the relative path for database storage
                        imagePath = $"{uniqueFileName}";
                    }

                    // Create the Category
                    var category = new Category
                    {
                        Name = model.Name,
                        Url = model.Url,
                        ImageUrl = imagePath ?? string.Empty
                    };

                    await _categoryRepository.CreateAsync(category);

                    TempData["SuccessMessage"] = "Category created successfully!";
                    return RedirectToAction("Dashboard");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while creating the category. Please try again.");
                    Console.WriteLine(ex.Message);
                }
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CategoryDelete(int id)
        {
            Console.WriteLine($"[INFO] Initiating category deletion process for ID: {id}");

            // Step 1: Retrieve the category
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                Console.WriteLine($"[WARN] No category found with ID: {id}");
                TempData["ErrorMessage"] = "Category not found.";
                return RedirectToAction("Categories");
            }

            Console.WriteLine($"[INFO] Category found: ID={category.CategoryId}, Name={category.Name}, ImageUrl={category.ImageUrl}");

            // Construct the image path
            var imgPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", category.ImageUrl ?? string.Empty);
            Console.WriteLine($"[INFO] Constructed image path: {imgPath}");

            // Step 2: Check if the category has an associated image
            if (!string.IsNullOrEmpty(category.ImageUrl))
            {
                Console.WriteLine("[INFO] Category has an associated image.");

                // Step 3: Delete the image file if it exists
                if (System.IO.File.Exists(imgPath))
                {
                    try
                    {
                        System.IO.File.Delete(imgPath);
                        Console.WriteLine($"[INFO] Successfully deleted image file: {imgPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Failed to delete image file: {imgPath}. Exception: {ex.Message}");
                        TempData["ErrorMessage"] = "Failed to delete associated image file.";
                        return RedirectToAction("Categories");
                    }
                }
                else
                {
                    Console.WriteLine($"[WARN] Image file not found at: {imgPath}. Possibly already deleted or never existed.");
                }
            }
            else
            {
                Console.WriteLine("[INFO] No associated image to delete.");
            }

            // Step 4: Delete the category
            try
            {
                Console.WriteLine($"[INFO] Deleting category with ID: {id}");
                await _categoryRepository.DeleteAsync(id);
                Console.WriteLine($"[INFO] Successfully deleted category with ID: {id}");
                TempData["SuccessMessage"] = "Category and its associated image were successfully deleted.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] An error occurred while deleting the category with ID: {id}. Exception: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while deleting the category.";
            }

            Console.WriteLine("[INFO] Redirecting to Categories view...");
            return RedirectToAction("Categories");
        }




        #region  CategoryEdit
        [HttpGet("Admin/CategoryEdit/{id}")]
        public async Task<IActionResult> CategoryEdit(int id)
        {
            // Retrieve the category from the repository
            var categoryEntity = await _categoryRepository.GetByIdAsync(id);

            // Handle case where category is not found
            if (categoryEntity == null)
            {
                TempData["ErrorMessage"] = "The requested category was not found.";
                return RedirectToAction("Categories");
            }

            // Map the entity to the view model
            var model = new CategoryEditModel
            {
                CategoryId = categoryEntity.CategoryId,
                Name = categoryEntity.Name,
                Url = categoryEntity.Url,
                ImageUrl = categoryEntity.ImageUrl
            };

            // Return the view with the populated model
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CategoryEdit(CategoryEditModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors.";
                return View(model);
            }

            // Retrieve the category
            var categoryEntity = await _categoryRepository.GetByIdAsync(model.CategoryId);
            if (categoryEntity == null)
            {
                TempData["ErrorMessage"] = "Category not found.";
                return RedirectToAction("Categories");
            }

            // Update entity
            categoryEntity.Name = model.Name;
            categoryEntity.Url = model.Url;

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img");

                // Ensure folder exists
                if (!Directory.Exists(imagesFolder))
                {
                    Directory.CreateDirectory(imagesFolder);
                }

                // Delete old image
                if (!string.IsNullOrEmpty(categoryEntity.ImageUrl))
                {
                    var oldImagePath = Path.Combine(imagesFolder, categoryEntity.ImageUrl);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Save new image
                var fileExtension = Path.GetExtension(model.ImageFile.FileName);
                var newFileName = $"{Guid.NewGuid()}{fileExtension}";
                var newImagePath = Path.Combine(imagesFolder, newFileName);

                using (var stream = new FileStream(newImagePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(stream);
                }

                categoryEntity.ImageUrl = newFileName;
            }

            await _categoryRepository.UpdateAsync(categoryEntity);

            TempData["SuccessMessage"] = "Category updated successfully!";
            return RedirectToAction("Categories");
        }
        #endregion



    #endregion




#region Blog Management


private void DeleteUnusedImages(Blog blog, List<string> usedImagePaths)
{
    // Paths for images
    string imgPath = Path.Combine(_env.WebRootPath, "blog", "img");
    string gifPath = Path.Combine(_env.WebRootPath, "blog", "gif");

    // Check for old images not used anymore
    string[] imgFiles = Directory.GetFiles(imgPath);
    string[] gifFiles = Directory.GetFiles(gifPath);

    // Merge files
    var allFiles = imgFiles.Concat(gifFiles).ToList();

    foreach (var file in allFiles)
    {
        string relativePath = file.Replace(_env.WebRootPath, "").Replace("\\", "/").TrimStart('/');
        if (!usedImagePaths.Contains(relativePath))
        {
            System.IO.File.Delete(file);
            Console.WriteLine($"[DEBUG] Deleted unused image: {file}");
        }
    }
}


private string ProcessContentImagesForEdit(string content)
{
    string imgPath = "/blog/img/";
    string gifPath = "/blog/gif/";

    // Replace temporary paths with permanent paths
    content = System.Text.RegularExpressions.Regex.Replace(content, @"/temp/(.*?\.(jpg|jpeg|png))", $"{imgPath}$1");
    content = System.Text.RegularExpressions.Regex.Replace(content, @"/temp/(.*?\.(gif))", $"{gifPath}$1");

    Console.WriteLine("[DEBUG] Fixed image paths in content for edit.");
    return content;
}


// ProcessContentImages Method
private string ProcessContentImages(string content, Dictionary<string, string> fileMappings)
{
    if (string.IsNullOrEmpty(content))
    {
        return content; // Return unchanged if content is null or empty
    }

    string tempPath = Path.Combine(_env.WebRootPath, "temp");
    string imgPath = Path.Combine(_env.WebRootPath, "blog", "img");
    string gifPath = Path.Combine(_env.WebRootPath, "blog", "gif");

    // Ensure directories exist
    Directory.CreateDirectory(imgPath);
    Directory.CreateDirectory(gifPath);

    // Replace mapped URLs first
     foreach (var mapping in fileMappings)
    {
        // Replace temp URLs in the content with final URLs
        content = content.Replace(mapping.Key, mapping.Value);

        // Move the file from the temp folder to the final destination
        string tempFilePath = Path.Combine(_env.WebRootPath, mapping.Key.TrimStart('/'));
        string finalFilePath = Path.Combine(_env.WebRootPath, mapping.Value.TrimStart('/'));
        // Ensure the directory exists before moving the file
        string? directoryPath = Path.GetDirectoryName(finalFilePath);
        if (directoryPath != null) // Check if it's not null
        {
            Directory.CreateDirectory(directoryPath); // Create the directory
        }
        if (System.IO.File.Exists(tempFilePath) && !System.IO.File.Exists(finalFilePath))
        {
            System.IO.File.Move(tempFilePath, finalFilePath); // Move the file
            Console.WriteLine($"[DEBUG] Moved file from {tempFilePath} to {finalFilePath}");
        }
        else
        {
            Console.WriteLine($"[WARNING] File move failed: {tempFilePath} to {finalFilePath}");
        }
    }

    // Find all images in content
    var matches = Regex.Matches(content, @"src=[""'](?<url>/temp/.*?\.(jpg|jpeg|png|gif))[""']");
    foreach (Match match in matches)
    {
        string tempUrl = match.Groups["url"].Value; // Example: /temp/image.png
        string tempFilePath = Path.Combine(_env.WebRootPath, tempUrl.TrimStart('/'));

        if (System.IO.File.Exists(tempFilePath))
        {
            string fileName = Path.GetFileName(tempFilePath);
            string fileExtension = Path.GetExtension(fileName).ToLower();

            // Determine destination path
            string targetPath = fileExtension == ".gif"
                ? Path.Combine(gifPath, fileName)
                : Path.Combine(imgPath, fileName);

            // Move file if it doesn't already exist
            if (!System.IO.File.Exists(targetPath))
            {
                System.IO.File.Move(tempFilePath, targetPath); // Move file to final location
                Console.WriteLine($"[DEBUG] Moved image: {tempFilePath} -> {targetPath}");
            }

            // Update content with the final URL
            string finalUrl = $"/blog/{(fileExtension == ".gif" ? "gif" : "img")}/{fileName}";
            content = content.Replace(tempUrl, finalUrl);
        }
        else
        {
            Console.WriteLine($"[WARNING] Temp image not found: {tempFilePath}");
        }
    }

    return content; // Return updated content
}



[HttpPost]
public async Task<IActionResult> UploadFile(IFormFile upload, string blogId)
{
    if (upload == null || upload.Length == 0)
    {
        return Json(new { uploaded = false, error = "No file uploaded." });
    }

    // Define Temp Path
    string tempPath = Path.Combine(_env.WebRootPath, "temp");
    Directory.CreateDirectory(tempPath); // Ensure temp folder exists

    // Calculate File Hash
    string fileHash;
    using (var md5 = System.Security.Cryptography.MD5.Create())
    {
        using (var stream = upload.OpenReadStream())
        {
            fileHash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
        }
    }

    // Generate Unique File Name Using Hash
    string extension = Path.GetExtension(upload.FileName);
    string fileName = $"{fileHash}{extension}";
    string tempFilePath = Path.Combine(tempPath, fileName);

    // Check if File Already Exists in Temp
    if (!System.IO.File.Exists(tempFilePath))
    {
        using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
        {
            await upload.CopyToAsync(fileStream);
        }
        Console.WriteLine($"[DEBUG] Uploaded new file: {fileName}");
    }
    else
    {
        Console.WriteLine($"[DEBUG] File already exists in temp: {fileName}");
    }

    // Return Temporary URL for CKEditor
    string tempUrl = $"/temp/{fileName}";
    return Json(new { uploaded = true, url = tempUrl });
}

private string MoveImagesAndFixPaths(string content, int blogId)
{
    string tempPath = Path.Combine(_env.WebRootPath, "temp");
    string imgPath = Path.Combine(_env.WebRootPath, "blog", "img");
    string gifPath = Path.Combine(_env.WebRootPath, "blog", "gif");

    // Ensure directories exist
    Directory.CreateDirectory(imgPath);
    Directory.CreateDirectory(gifPath);

    // Find all image src attributes in the content
    var matches = System.Text.RegularExpressions.Regex.Matches(content, @"src=[""'](?<url>/temp/.*?\.(jpg|jpeg|png|gif))[""']");
    foreach (System.Text.RegularExpressions.Match match in matches)
    {
        string tempUrl = match.Groups["url"].Value; // Example: /temp/image123.gif
        string tempFilePath = Path.Combine(_env.WebRootPath, tempUrl.TrimStart('/'));

        // Move file if it exists
        if (System.IO.File.Exists(tempFilePath))
        {
            string fileName = Path.GetFileName(tempFilePath);
            string fileExtension = Path.GetExtension(fileName).ToLower();

            // Determine destination folder based on file extension
            string targetPath = fileExtension == ".gif"
                ? Path.Combine(gifPath, fileName)
                : Path.Combine(imgPath, fileName);

            if (!System.IO.File.Exists(targetPath))
            {
                System.IO.File.Move(tempFilePath, targetPath);
                Console.WriteLine($"[DEBUG] Moved image: {tempFilePath} -> {targetPath}");
            }

            // Replace temp URL with the final URL in the content
            string finalUrl = $"/blog/{(fileExtension == ".gif" ? "gif" : "img")}/{fileName}";
            content = content.Replace(tempUrl, finalUrl);
        }
        else
        {
            Console.WriteLine($"[WARNING] Temp image not found: {tempFilePath}");
        }
    }

    return content; // Return processed content with updated paths
}



private void SaveContentToResx(BlogCreateModel model , int id)
{
    // Supported languages and their codes
    string[] cultures = { "en-US", "tr-TR", "de-DE", "fr-FR" };
    string[] titles = { model.TitleUS, model.TitleTR, model.TitleDE, model.TitleFR };
    string[] contents = { model.ContentUS, model.ContentTR, model.ContentDE, model.ContentFR };

    // Process each translation
    for (int i = 0; i < cultures.Length; i++)
    {
        string culture = cultures[i];  // Language code
        string title = titles[i];      // Language-specific title
        string content = contents[i]; // Already processed content

        // Save translations to .resx
        _manageResourceService.AddOrUpdateResource($"Title_{id}_{model.Url}_{culture.Substring(0, 2).ToLower()}", title, culture);
        _manageResourceService.AddOrUpdateResource($"Content_{id}_{model.Url}_{culture.Substring(0, 2).ToLower()}", content, culture);

        Console.WriteLine($"[DEBUG] Translation saved for {culture} with updated image paths.");
    }

    Console.WriteLine("[DEBUG] All translations updated successfully.");
}


private void SaveContentToResxEdit(BlogResultModel model, string updatedContent, int id)
{
    // Supported languages and their codes
    string[] cultures = { "en-US", "tr-TR", "de-DE", "fr-FR" };
    string[] langCodes = { "en", "tr", "de", "fr" };
    string[] titles = { model.TitleUS, model.TitleTR, model.TitleDE, model.TitleFR };
    string[] contents = { model.ContentUS, model.ContentTR, model.ContentDE, model.ContentFR };

    for (int i = 0; i < cultures.Length; i++)
    {
        string culture = cultures[i];
        string langCode = langCodes[i]; // Language code for key
        string title = titles[i];
        string content = contents[i];

        // Process images in translations
        string processedContent = ProcessContentImagesForEdit(content);

        // Save translations to .resx
        _manageResourceService.AddOrUpdateResource($"Title_{id}_{model.Url}_{langCode}", title, culture);
        _manageResourceService.AddOrUpdateResource($"Content_{id}_{model.Url}_{langCode}", processedContent, culture);

        Console.WriteLine($"[DEBUG] Translation saved for {culture} with updated image paths.");
    }

    Console.WriteLine("[DEBUG] All translations updated successfully.");
}

private List<string> ExtractImagesFromTranslations(int blogId, string url)
{
    List<string> imagePaths = new();
    var cultures = new Dictionary<string, string>
    {
        { "en-US", "en" },
        { "tr-TR", "tr" },
        { "de-DE", "de" },
        { "fr-FR", "fr" }
    };

    foreach (var culture in cultures)
    {
        string contentKey = $"Content_{blogId}_{url}_{culture.Value}";
        string translationContent = _manageResourceService.ReadResourceValue(contentKey, culture.Key);

        if (!string.IsNullOrEmpty(translationContent))
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(
                translationContent,
                @"src=[""'](?<url>/blog/(img|gif)/.*?\.(jpg|jpeg|png|gif))[""']");
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                imagePaths.Add(match.Groups["url"].Value.TrimStart('/'));
            }
        }
    }

    return imagePaths; // Return all extracted image paths
}


private async Task DeleteOrphanedImages()
{
    string imgFolder = Path.Combine(_env.WebRootPath, "blog", "img");
    string gifFolder = Path.Combine(_env.WebRootPath, "blog", "gif");

    // Collect all image paths in the folders
    var allImages = Directory.GetFiles(imgFolder, "*.*", SearchOption.TopDirectoryOnly)
                             .Union(Directory.GetFiles(gifFolder, "*.*", SearchOption.TopDirectoryOnly))
                             .ToList();

    // Extract used images from translations
    var usedImages = new HashSet<string>();
    var blogs = await _blogRepository.GetAllAsync(); // Get all blogs
    foreach (var blog in blogs)
    {
        usedImages.UnionWith(ExtractImagesFromTranslations(blog.BlogId, blog.Url));
    }

    // Compare and delete unused images
    foreach (var image in allImages)
    {
        string relativePath = image.Replace(_env.WebRootPath, "").Replace("\\", "/").TrimStart('/');

        if (!usedImages.Contains(relativePath))
        {
            try
            {
                System.IO.File.Delete(image);
                Console.WriteLine($"[DEBUG] Deleted orphaned image: {image}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to delete orphaned image {image}: {ex.Message}");
            }
        }
    }
}


private void DeleteImages(Blog blog)
{
    // Paths for image storage
    string imgPath = Path.Combine(_env.WebRootPath, "blog", "img");
    string gifPath = Path.Combine(_env.WebRootPath, "blog", "gif");
    string coverPath = Path.Combine(_env.WebRootPath, "img");

    // 1. Delete Cover Image
    if (!string.IsNullOrEmpty(blog.Image))
    {
        string coverImagePath = Path.Combine(coverPath, blog.Image);
        if (System.IO.File.Exists(coverImagePath))
        {
            System.IO.File.Delete(coverImagePath);
            Console.WriteLine($"[DEBUG] Deleted cover image: {coverImagePath}");
        }
        else
        {
            Console.WriteLine($"[WARNING] Cover image not found: {coverImagePath}");
        }
    }

    // 2. Delete Embedded Images in Content and Translations
    // Supported cultures and keys
    var cultures = new Dictionary<string, string>
    {
        { "en-US", "en" },
        { "tr-TR", "tr" },
        { "de-DE", "de" },
        { "fr-FR", "fr" }
    };

    // Track deleted images to avoid multiple deletions
    HashSet<string> deletedImages = new HashSet<string>();

    // Iterate through each translation and process its images
    foreach (var culture in cultures)
    {
        // Generate translation content key
        string contentKey = $"Content_{blog.BlogId}_{blog.Url}_{culture.Value}";

        // Read the content from .resx
        string translationContent = _manageResourceService.ReadResourceValue(contentKey, culture.Key);

        if (string.IsNullOrEmpty(translationContent))
        {
            Console.WriteLine($"[WARNING] No content found for key {contentKey} in culture {culture.Key}");
            continue;
        }

        // Extract all image URLs using Regex
        var matches = System.Text.RegularExpressions.Regex.Matches(
            translationContent,
            @"src=[""'](?<url>/blog/(img|gif)/.*?\.(jpg|jpeg|png|gif))[""']");

        // Process each image
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            string imageUrl = match.Groups["url"].Value; // e.g., /blog/img/example1.jpg
            string imagePath = Path.Combine(_env.WebRootPath, imageUrl.TrimStart('/')); // Get full path

            // Skip if already deleted
            if (deletedImages.Contains(imagePath))
            {
                continue; // Avoid duplicate deletions
            }

            if (System.IO.File.Exists(imagePath))
            {
                try
                {
                    System.IO.File.Delete(imagePath); // Delete file
                    deletedImages.Add(imagePath);    // Track deleted image
                    Console.WriteLine($"[DEBUG] Deleted embedded image: {imagePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to delete image {imagePath}: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"[WARNING] Embedded image not found: {imagePath}");
            }
        }
    }

    Console.WriteLine("[DEBUG] All images related to the blog and translations deleted.");
}



private List<string> ExtractImagePathsFromContent(string content)
{
    var imagePaths = new List<string>();

    if (!string.IsNullOrEmpty(content))
    {
        var matches = System.Text.RegularExpressions.Regex.Matches(content, @"src=[""'](?<url>/blog/(img|gif)/.*?\.(jpg|jpeg|png|gif))[""']");
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            imagePaths.Add(match.Groups["url"].Value.TrimStart('/')); // Add relative path
        }
    }

    return imagePaths;
}


private void DeleteTranslations(int id, string url)
{
    // Supported languages and their culture codes
    var cultures = new Dictionary<string, string>
    {
        { "en-US", "en" },
        { "tr-TR", "tr" },
        { "de-DE", "de" },
        { "fr-FR", "fr" }
    };

    // Loop through each culture and delete its translations
    foreach (var culture in cultures)
    {
        string cultureCode = culture.Value; // 'en', 'tr', etc.
        string lang = culture.Key;         // 'en-US', 'tr-TR', etc.

        // Construct keys with culture suffix
        string titleKey = $"Title_{id}_{url}_{cultureCode}";
        string contentKey = $"Content_{id}_{url}_{cultureCode}";

        // Delete resources for the current language
        _manageResourceService.DeleteResource(titleKey, lang);
        _manageResourceService.DeleteResource(contentKey, lang);

        Console.WriteLine($"[DEBUG] Deleted translations for Blog ID {id} in culture {lang}");
    }
}

private void DeleteUnusedImages(List<string> unusedImages)
{
    foreach (var imgPath in unusedImages)
    {
        string fullPath = Path.Combine(_env.WebRootPath, imgPath.TrimStart('/'));
        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
            Console.WriteLine($"[DEBUG] Deleted unused image: {fullPath}");
        }
        else
        {
            Console.WriteLine($"[WARNING] File not found: {fullPath}");
        }
    }
}



#endregion

#region Blog
public async Task<IActionResult> Blogs()
{
    var blogs = await _blogRepository.GetAllAsync();
    return View(blogs);
}

[HttpPost]
public async Task<IActionResult> BlogCreate(BlogCreateModel model)
{
    if (!ModelState.IsValid)
    {
        Console.WriteLine("[ERROR] BlogCreate model validation failed.");

        // Re-populate ViewBag in case of validation errors
        var categories = await _categoryRepository.GetAllAsync();
        var products = await _productRepository.GetAllAsync();

        ViewBag.Categories = categories.Select(c => new SelectListItem
        {
            Value = c.CategoryId.ToString(),
            Text = c.Name
        }).ToList();

        ViewBag.Products = products.Select(p => new SelectListItem
        {
            Value = p.ProductId.ToString(),
            Text = p.Name
        }).ToList();

        return View(model);
    }

    // Define paths
    string tempPath = Path.Combine(_env.WebRootPath, "temp");
    string imgPath = Path.Combine(_env.WebRootPath, "blog", "img");
    string gifPath = Path.Combine(_env.WebRootPath, "blog", "gif");
    string coverPath = Path.Combine(_env.WebRootPath, "img");

    // Ensure directories exist
    Directory.CreateDirectory(tempPath);
    Directory.CreateDirectory(imgPath);
    Directory.CreateDirectory(gifPath);
    Directory.CreateDirectory(coverPath);

    // Map all files in temp folder
    var fileMappings = new Dictionary<string, string>();
    foreach (string file in Directory.GetFiles(tempPath))
    {
        string fileName = Path.GetFileName(file);
        string fileExtension = Path.GetExtension(file).ToLower();

        // Determine destination folder
        string destination = fileExtension == ".gif"
            ? Path.Combine(gifPath, fileName)
            : Path.Combine(imgPath, fileName);

        // Move file if it doesn't already exist
        if (!System.IO.File.Exists(destination))
        {
            System.IO.File.Move(file, destination);
            Console.WriteLine($"[DEBUG] Moved content image: {fileName}");
        }

        // Map the file for later replacements
        string tempUrl = $"/temp/{fileName}";
        string finalUrl = $"/blog/{(fileExtension == ".gif" ? "gif" : "img")}/{fileName}";
        fileMappings[tempUrl] = finalUrl;
    }

    // Process main content and translations
    string updatedContent = ProcessContentImages(model.Content, fileMappings);
    string updatedContentUS = ProcessContentImages(model.ContentUS, fileMappings);
    string updatedContentTR = ProcessContentImages(model.ContentTR, fileMappings);
    string updatedContentDE = ProcessContentImages(model.ContentDE, fileMappings);
    string updatedContentFR = ProcessContentImages(model.ContentFR, fileMappings);

    // Handle Cover Image
    string coverFileName = null!;
    if (model.ImageFile != null && model.ImageFile.Length > 0)
    {
        coverFileName = $"{model.Url}_cover{Path.GetExtension(model.ImageFile.FileName)}";
        string coverFilePath = Path.Combine(coverPath, coverFileName);

        using (var stream = new FileStream(coverFilePath, FileMode.Create))
        {
            await model.ImageFile.CopyToAsync(stream);
        }

        Console.WriteLine($"[DEBUG] Saved cover image: {coverFileName}");
    }

    // Create Blog Entity
    var blog = new Blog
    {
        Title = model.Title,
        Content = updatedContent,
        Url = model.Url,
        Image = coverFileName,
        Date = DateTime.Now,
        Author = model.Author,
        RawYT = model.RawYT,
        RawMaps = model.RawMaps
    };

        // Add selected categories
        foreach (var categoryId in model.SelectedCategoryIds)
        {
            blog.CategoryBlogs.Add(new CategoryBlog
            {
                CategoryId = categoryId
            });
        }

        // Add selected products
        foreach (var productId in model.SelectedProductIds)
        {
            blog.ProductBlogs.Add(new ProductBlog
            {
                ProductId = productId
            });
        }


    // Save to database using repository
    var result = await _blogRepository.CreateAsync(blog);

    // Save translations with generated BlogId
    SaveContentToResx(new BlogCreateModel
    {
        TitleUS = model.TitleUS,
        ContentUS = updatedContentUS,
        TitleTR = model.TitleTR,
        ContentTR = updatedContentTR,
        TitleDE = model.TitleDE,
        ContentDE = updatedContentDE,
        TitleFR = model.TitleFR,
        ContentFR = updatedContentFR,
        Url = model.Url
    }, result.BlogId );

    Console.WriteLine($"[DEBUG] Blog '{result.Title}' saved successfully with ID {result.BlogId}.");
    return RedirectToAction("Blogs");
}

[HttpGet]
public async Task<IActionResult> BlogCreate()
{
    // Fetch all categories and products
    var categories = await _categoryRepository.GetAllAsync();
    var products = await _productRepository.GetAllAsync();

    // Populate ViewBag for category and product display
    ViewBag.Categories = categories.Select(c => new SelectListItem
    {
        Value = c.CategoryId.ToString(),
        Text = c.Name
    }).ToList();

    ViewBag.Products = products.Select(p => new SelectListItem
    {
        Value = p.ProductId.ToString(),
        Text = p.Name
    }).ToList();

    // Initialize a new model for the view
    var model = new BlogCreateModel
    {
        SelectedCategoryIds = new List<int>(),
        SelectedProductIds = new List<int>()
    };

    Console.WriteLine("[DEBUG] BlogCreate page loaded.");
    return View(model);
}

[HttpPost]
public async Task<IActionResult> BlogDelete(int id)
{
    Console.WriteLine($"[INFO] Blog deletion requested for ID: {id}");

    // -----------------------------
    // STEP 1: Retrieve the Blog
    // -----------------------------
    var blog = await _blogRepository.GetByIdAsync(id);
    if (blog == null)
    {
        Console.WriteLine($"[ERROR] Blog with ID {id} not found.");
        return NotFound($"Blog with ID {id} not found.");
    }

    try
    {
        // -----------------------------
        // STEP 2: Delete Associated Images
        // -----------------------------
        Console.WriteLine($"[INFO] Deleting associated images for Blog ID {id}...");
        DeleteImages(blog);
        Console.WriteLine($"[DEBUG] Images deleted for Blog ID {id}.");

        // -----------------------------
        // STEP 3: Delete Translations
        // -----------------------------
        Console.WriteLine($"[INFO] Deleting translations for Blog ID {id}...");
        DeleteTranslations(id, blog.Url);
        Console.WriteLine($"[DEBUG] Translations deleted for Blog ID {id}.");

        // -----------------------------
        // STEP 4: Remove Related Entities
        // -----------------------------
        Console.WriteLine($"[INFO] Removing related entities for Blog ID {id}...");
        await _blogRepository.RemoveRelatedEntitiesAsync(id);
        Console.WriteLine($"[DEBUG] Related entities deleted for Blog ID {id}.");
        // Remove related CategoryBlogs
        var relatedCategoryBlogs = blog.CategoryBlogs.ToList();
        foreach (var categoryBlog in relatedCategoryBlogs)
        {
            _dbContext.CategoryBlog.Remove(categoryBlog);
        }

        // Remove related ProductBlogs
        var relatedProductBlogs = blog.ProductBlogs.ToList();
        foreach (var productBlog in relatedProductBlogs)
        {
            _dbContext.ProductBlog.Remove(productBlog);
        }

        await _dbContext.SaveChangesAsync();
        Console.WriteLine($"[DEBUG] Related entities deleted for Blog ID {id}.");

        // -----------------------------
        // STEP 5: Delete the Blog Entry
        // -----------------------------
        Console.WriteLine($"[INFO] Deleting blog entry for Blog ID {id}...");
        await _blogRepository.DeleteAsync(id);
        Console.WriteLine($"[DEBUG] Blog with ID {id} deleted successfully.");

        // -----------------------------
        // STEP 6: Clean Up Orphaned Images
        // -----------------------------
        Console.WriteLine($"[INFO] Cleaning up orphaned images...");
        await DeleteOrphanedImages(); // Ensure this is an async method
        Console.WriteLine($"[DEBUG] Orphaned images cleaned up.");

        // -----------------------------
        // STEP 7: Return Success Response
        // -----------------------------
        Console.WriteLine($"[SUCCESS] Blog ID {id} deleted successfully.");
        return RedirectToAction("Blogs");
    }
    catch (Exception ex)
    {
        // -----------------------------
        // ERROR HANDLING
        // -----------------------------
        Console.WriteLine($"[ERROR] Failed to delete Blog ID {id}. Exception: {ex}");
        return StatusCode(500, $"Internal Server Error. Failed to delete Blog ID {id}.");
    }
}




[HttpGet]
public async Task<IActionResult> BlogEdit(int id)
{
    // Retrieve the blog by ID
    var blog = await _blogRepository.GetByIdAsync(id);

    if (blog == null)
    {
        Console.WriteLine($"[ERROR] Blog with ID {id} not found.");
        return NotFound();
    }

    // Fetch all categories
    var categories = await _categoryRepository.GetAllAsync();
    var products = await _productRepository.GetAllAsync();

    // Get IDs of selected categories (from many-to-many relationship)
    var selectedCategoryIds = blog.CategoryBlogs.Select(cb => cb.CategoryId).ToList();
    var selectedProductIds = blog.ProductBlogs.Select(pb => pb.ProductId).ToList();

    // Populate ViewBag for category display
    ViewBag.Categories = categories.Select(c => new SelectListItem
    {
        Value = c.CategoryId.ToString(),
        Text = c.Name
    }).ToList();

    ViewBag.Products = products.Select(p => new SelectListItem
    {
        Value = p.ProductId.ToString(),
        Text = p.Name
    }).ToList();
    // Create ViewModel with existing data
    var model = new BlogEditModel
    {
        BlogId = blog.BlogId,
        Title = blog.Title,
        Content = ProcessContentImagesForEdit(blog.Content), // Process main content
        Url = blog.Url,
        Author = blog.Author,
        RawYT = blog.RawYT,
        RawMaps = blog.RawMaps,
        SelectedCategoryIds = selectedCategoryIds, // Assign selected category IDs
        SelectedProductIds = selectedProductIds,
        ExistingImage = blog.Image, // Preview current image

        // Load translations (updated format)
        TitleUS = _manageResourceService.ReadResourceValue($"Title_{id}_{blog.Url}_en", "en-US") ?? string.Empty,
        ContentUS = ProcessContentImagesForEdit(
            _manageResourceService.ReadResourceValue($"Content_{id}_{blog.Url}_en", "en-US")
        ),
        TitleTR = _manageResourceService.ReadResourceValue($"Title_{id}_{blog.Url}_tr", "tr-TR") ?? string.Empty,
        ContentTR = ProcessContentImagesForEdit(
            _manageResourceService.ReadResourceValue($"Content_{id}_{blog.Url}_tr", "tr-TR")
        ),
        TitleDE = _manageResourceService.ReadResourceValue($"Title_{id}_{blog.Url}_de", "de-DE") ?? string.Empty,
        ContentDE = ProcessContentImagesForEdit(
            _manageResourceService.ReadResourceValue($"Content_{id}_{blog.Url}_de", "de-DE")
        ),
        TitleFR = _manageResourceService.ReadResourceValue($"Title_{id}_{blog.Url}_fr", "fr-FR") ?? string.Empty,
        ContentFR = ProcessContentImagesForEdit(
            _manageResourceService.ReadResourceValue($"Content_{id}_{blog.Url}_fr", "fr-FR")
        ),
    };
     Console.WriteLine($"[DEBUG] BlogEdit page loaded for Blog ID: {id} with SelectedCategoryIds: {string.Join(", ", selectedCategoryIds)} and SelectedProductIds: {string.Join(", ", selectedProductIds)}");
    Console.WriteLine($"[DEBUG] BlogEdit page loaded for Blog ID: {id} with SelectedCategoryIds: {string.Join(", ", selectedCategoryIds)}");
    return View(model);
}




// BlogEdit Method
[HttpPost]
public async Task<IActionResult> BlogEdit(int id, BlogEditModel model)
{
    if (!ModelState.IsValid)
    {
        var categories = await _categoryRepository.GetAllAsync();
        var products = await _productRepository.GetAllAsync();
        ViewBag.Categories = categories.Select(c => new SelectListItem
        {
            Value = c.CategoryId.ToString(),
            Text = c.Name
        }).ToList();
        ViewBag.Products = products.Select(p => new SelectListItem
        {
            Value = p.ProductId.ToString(),
            Text = p.Name
        }).ToList();
        return View(model);
    }

    // Retrieve the blog
    var blog = await _blogRepository.GetByIdAsync(id);
    if (blog == null)
    {
        return NotFound();
    }

     // Update categories (Many-to-Many)
    var existingCategoryBlogs = blog.CategoryBlogs.ToList();
    foreach (var categoryBlog in existingCategoryBlogs)
    {
        _dbContext.CategoryBlog.Remove(categoryBlog); // Remove old mappings
    }

    foreach (var categoryId in model.SelectedCategoryIds)
    {
        var newCategoryBlog = new CategoryBlog
        {
            BlogId = blog.BlogId,
            CategoryId = categoryId
        };
        _dbContext.CategoryBlog.Add(newCategoryBlog); // Add new mappings
    }
    // Update products (Many-to-Many)
    var existingProductBlogs = blog.ProductBlogs.ToList();
    foreach (var productBlog in existingProductBlogs)
    {
        _dbContext.ProductBlog.Remove(productBlog);
    }

    foreach (var productId in model.SelectedProductIds)
    {
        var newProductBlog = new ProductBlog
        {
            BlogId = blog.BlogId,
            ProductId = productId
        };
        _dbContext.ProductBlog.Add(newProductBlog);
    }

    // Extract old images from content and translations
    List<string> oldImages = new();
    oldImages.AddRange(ExtractImagePathsFromContent(blog.Content));
    oldImages.AddRange(ExtractImagePathsFromContent(
        _manageResourceService.ReadResourceValue($"Content_{id}_{blog.Url}_en", "en-US")));
    oldImages.AddRange(ExtractImagePathsFromContent(
        _manageResourceService.ReadResourceValue($"Content_{id}_{blog.Url}_tr", "tr-TR")));
    oldImages.AddRange(ExtractImagePathsFromContent(
        _manageResourceService.ReadResourceValue($"Content_{id}_{blog.Url}_de", "de-DE")));
    oldImages.AddRange(ExtractImagePathsFromContent(
        _manageResourceService.ReadResourceValue($"Content_{id}_{blog.Url}_fr", "fr-FR")));
    

    // Generate mappings for temp images
    Dictionary<string, string> fileMappings = new();
    string tempPath = Path.Combine(_env.WebRootPath, "temp");
    string imgPath = "/blog/img/";
    string gifPath = "/blog/gif/";

    foreach (string file in Directory.GetFiles(tempPath))
    {
        string fileName = Path.GetFileName(file);
        string extension = Path.GetExtension(file).ToLower();
        string finalPath = extension == ".gif"
            ? $"{gifPath}{fileName}"
            : $"{imgPath}{fileName}";
        fileMappings[$"/temp/{fileName}"] = finalPath;
    }

    // Process content and translations
    string updatedContent = ProcessContentImages(model.Content, fileMappings);
    string updatedContentUS = ProcessContentImages(model.ContentUS, fileMappings);
    string updatedContentTR = ProcessContentImages(model.ContentTR, fileMappings);
    string updatedContentDE = ProcessContentImages(model.ContentDE, fileMappings);
    string updatedContentFR = ProcessContentImages(model.ContentFR, fileMappings);

    // Update blog details
    blog.Title = model.Title;
    blog.Content = updatedContent;
    blog.Url = model.Url;
    blog.Author = model.Author;
    blog.RawYT = model.RawYT;
    blog.RawMaps = model.RawMaps;

    // Handle cover image
    if (model.ImageFile != null)
    {
        string oldCoverPath = Path.Combine(_env.WebRootPath, "img", blog.Image);
        if (System.IO.File.Exists(oldCoverPath))
        {
            System.IO.File.Delete(oldCoverPath);
        }

        string newImageName = $"{Guid.NewGuid()}{Path.GetExtension(model.ImageFile.FileName)}";
        string newImagePath = Path.Combine(_env.WebRootPath, "img", newImageName);

        using (var stream = new FileStream(newImagePath, FileMode.Create))
        {
            await model.ImageFile.CopyToAsync(stream);
        }

        blog.Image = newImageName;
    }

    var updatedBlog = await _blogRepository.UpdateAsync(blog);

    var resultModel = new BlogResultModel
    {
        BlogId = updatedBlog.BlogId,
        Url = updatedBlog.Url,
        TitleUS = model.TitleUS,
        ContentUS = updatedContentUS,
        TitleTR = model.TitleTR,
        ContentTR = updatedContentTR,
        TitleDE = model.TitleDE,
        ContentDE = updatedContentDE,
        TitleFR = model.TitleFR,
        ContentFR = updatedContentFR
    };

    SaveContentToResxEdit(resultModel, updatedContent, updatedBlog.BlogId);
    // Calculate unused images
    List<string> usedImages = new();
    usedImages.AddRange(ExtractImagePathsFromContent(updatedContent));
    usedImages.AddRange(ExtractImagePathsFromContent(updatedContentUS));
    usedImages.AddRange(ExtractImagePathsFromContent(updatedContentTR));
    usedImages.AddRange(ExtractImagePathsFromContent(updatedContentDE));
    usedImages.AddRange(ExtractImagePathsFromContent(updatedContentFR));

    List<string> unusedImages = oldImages.Except(usedImages).ToList();
    DeleteUnusedImages(unusedImages);

    return RedirectToAction("Blogs");
}




#endregion



#region Image Management
    // GET: Admin/ImageEdit/{id}
    [HttpGet("Admin/ImageEdit/{id}")]
    public async Task<IActionResult> ImageEdit(int id)
    {
        Console.WriteLine($"[INFO] Initiating image edit process for ID: {id}");

        // Step 1: Retrieve the image
        var image = await _imageRepository.GetByIdAsync(id);
        if (image == null)
        {
            Console.WriteLine($"[WARN] No image found with ID: {id}");
            TempData["ErrorMessage"] = "Image not found.";
            return RedirectToAction("Images");
        }

        Console.WriteLine($"[INFO] Image found: ID={image.ImageId}, URL={image.ImageUrl}");

        // Step 2: Map to ImageEditModel
        var model = new ImageEditModel
        {
            ImageId = image.ImageId,
            ImageUrl = image.ImageUrl,
            Text = image.Text,
            DateAdded = image.DateAdded,
            ViewPhone = image.ViewPhone
        };

        return View(model);
    }

    [HttpPost("Admin/ImageEdit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImageEdit(ImageEditModel model, IFormFile newImageFile)
    {
        Console.WriteLine($"[INFO] Processing image edit for ID: {model.ImageId}");

        // Step 1: Validation
        if (!ModelState.IsValid)
        {
            Console.WriteLine("[WARN] Model validation failed.");
            return View(model);
        }

        // Step 2: Retrieve the existing image
        var imageEntity = await _imageRepository.GetByIdAsync(model.ImageId);
        if (imageEntity == null)
        {
            Console.WriteLine($"[WARN] No image found with ID: {model.ImageId}");
            TempData["ErrorMessage"] = "Image not found.";
            return RedirectToAction("Images");
        }

        // Step 3: Update text and ViewPhone properties
        imageEntity.Text = model.Text;
        imageEntity.ViewPhone = model.ViewPhone;

        // Step 4: Handle new image file upload (if provided)
        if (newImageFile != null && newImageFile.Length > 0)
        {
            try
            {
                // Build the image path
                var uploadsFolder = Path.Combine(_env.WebRootPath, "img");

                // Delete old image file
                if (!string.IsNullOrEmpty(imageEntity.ImageUrl))
                {
                    var oldImagePath = Path.Combine(uploadsFolder, imageEntity.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                        Console.WriteLine($"[INFO] Deleted old image file: {oldImagePath}");
                    }
                }

                // Save new file
                var newFileName = $"{Guid.NewGuid()}{Path.GetExtension(newImageFile.FileName)}";
                var newImagePath = Path.Combine(uploadsFolder, newFileName);
                using (var stream = new FileStream(newImagePath, FileMode.Create))
                {
                    await newImageFile.CopyToAsync(stream);
                }
                Console.WriteLine($"[INFO] Saved new image file: {newImagePath}");

                // Update the image URL
                imageEntity.ImageUrl = $"/img/{newFileName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to process new image file. Exception: {ex.Message}");
                TempData["ErrorMessage"] = "Failed to upload new image.";
                return View(model);
            }
        }
        else
        {
            Console.WriteLine("[INFO] No new image file provided. Skipping image update.");
        }

        // Step 5: Update database
        try
        {
            await _imageRepository.UpdateAsync(imageEntity);
            Console.WriteLine($"[INFO] Successfully updated image with ID: {model.ImageId}");
            TempData["SuccessMessage"] = "Image updated successfully!";
            return RedirectToAction("Images");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to update image record. Exception: {ex.Message}");
            TempData["ErrorMessage"] = "An error occurred while updating the image.";
            return View(model);
        }
    }


    [HttpGet("Admin/ImageCreate")]
    public IActionResult ImageCreate()
    {
        return View();
    }

    [HttpPost("Admin/ImageCreate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImageCreate(ImageCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please correct the form errors.";
            return View(model);
        }

        try
        {
            if (model.File != null && model.File.Length > 0)
            {
                // File management
                var uploadsFolder = Path.Combine(_env.WebRootPath, "img");
                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(model.File.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.File.CopyToAsync(stream);
                }

                // Create Image entity
                var image = new Image
                {
                    ImageUrl = uniqueFileName,
                    Text = model.Text,
                    DateAdded = DateTime.UtcNow,
                    ViewPhone = model.ViewPhone
                };

                // Save to database
                await _imageRepository.CreateAsync(image);
                TempData["SuccessMessage"] = "Image uploaded successfully!";
                return RedirectToAction("Images");
            }

            TempData["ErrorMessage"] = "File is required.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {ex.Message}");
            TempData["ErrorMessage"] = "An error occurred while uploading the image.";
        }

        return View(model);
    }


    [HttpGet("Admin/Images")] // Explicit route
    public async Task<IActionResult> Images()
    {
        var images = await _imageRepository.GetAllAsync();

        var model = images.Select(image => new ImageWithProductsViewModel
        {
            ImageId = image.ImageId,
            ImageUrl = image.ImageUrl,
            Text = image.Text,
            DateAdded = image.DateAdded,
            ViewPhone = image.ViewPhone,
            Products = image.ProductImages.Select(pi => new ProductViewModel
            {
                ProductId = pi.Product.ProductId,
                ProductName = pi.Product.Name,
                Description = pi.Product.Description
            }).ToList()
        }).ToList();

        return View(model);
    }


    [HttpPost("Admin/UploadImage")]
    public async Task<IActionResult> UploadImage(IFormFile file, string text, bool viewPhone)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a valid image file.";
                return RedirectToAction("Images");
            }

            try
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "img");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Unique file name
                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Save image to database
                var image = new Image
                {
                    ImageUrl = $"/img/{uniqueFileName}",
                    Text = text ?? string.Empty,
                    DateAdded = DateTime.UtcNow,
                    ViewPhone = viewPhone
                };

                await _imageRepository.CreateAsync(image);
                TempData["SuccessMessage"] = "Image uploaded successfully!";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Upload failed: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while uploading the image.";
            }

            return RedirectToAction("Images");
        }

        [HttpPost("Admin/DeleteImage")]
        public async Task<IActionResult> DeleteImage(int id)
        {
            Console.WriteLine($"[INFO] Initiating image deletion process for ID: {id}");

            // Step 1: Retrieve the image
            var image = await _imageRepository.GetByIdAsync(id);
            if (image == null)
            {
                Console.WriteLine($"[WARN] No image found with ID: {id}");
                TempData["ErrorMessage"] = "Image not found.";
                return RedirectToAction("Images");
            }

            Console.WriteLine($"[INFO] Image found: ID={image.ImageId}, ImageUrl={image.ImageUrl}, Text={image.Text}");

            // Construct the image path
            var imgPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", image.ImageUrl ?? string.Empty);
            Console.WriteLine($"[INFO] Constructed image path: {imgPath}");

            // Step 2: Check if the image file exists
            if (!string.IsNullOrEmpty(image.ImageUrl))
            {
                Console.WriteLine("[INFO] Image has an associated file.");

                // Step 3: Delete the image file if it exists
                if (System.IO.File.Exists(imgPath))
                {
                    try
                    {
                        System.IO.File.Delete(imgPath);
                        Console.WriteLine($"[INFO] Successfully deleted image file: {imgPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Failed to delete image file: {imgPath}. Exception: {ex.Message}");
                        TempData["ErrorMessage"] = "Failed to delete the associated image file.";
                        return RedirectToAction("Images");
                    }
                }
                else
                {
                    Console.WriteLine($"[WARN] Image file not found at: {imgPath}. Possibly already deleted or never existed.");
                }
            }
            else
            {
                Console.WriteLine("[INFO] No associated image file to delete.");
            }

            // Step 4: Delete the image record from the database
            try
            {
                Console.WriteLine($"[INFO] Deleting image record with ID: {id}");
                await _imageRepository.DeleteAsync(id);
                Console.WriteLine($"[INFO] Successfully deleted image record with ID: {id}");
                TempData["SuccessMessage"] = "Image and its associated file were successfully deleted.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] An error occurred while deleting the image record with ID: {id}. Exception: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while deleting the image.";
            }

            Console.WriteLine("[INFO] Redirecting to Images view...");
            return RedirectToAction("Images");
        }

#endregion



#region Invite User
    private async Task SendInvitationEmail(string email, string callbackUrl, int tokenDuration)
    {
        var subject = "Alpha Admin Invitation";
        var message = $@"
            <h2>Welcome to Alpha Admin Panel</h2>
            <p>You have been invited to join the Alpha Admin system.</p>
            <p>Please click the link below to set up your account:</p>
            <p>
                <a href='{callbackUrl}' 
                style='display: inline-block; background-color: #ff7b00; color: #fff; 
                        padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                    Complete Registration
                </a>
            </p>
            <p>This invitation link will expire in <strong>{tokenDuration} day(s)</strong>.</p>
            <p>If you did not expect this email, please ignore it.</p>";

        try
        {
            await _emailSender.SendEmailAsync(email, subject, message);
        }
        catch (Exception ex)
        {
            // Handle exceptions for logging or debugging
            Console.WriteLine($"Error sending email: {ex.Message}");
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(string email, int tokenDuration)
    {
        if (string.IsNullOrEmpty(email))
        {
            TempData.Put("AlertMessage", new AlertMessage
            {
                Title = "Error",
                Message = "Email cannot be empty.",
                AlertType = "danger",
                icon = "fas fa-file",
                icon2 = "fas fa-times"
            });
            return View("Dashboard");
        }

        // Check for valid email format
        if (!new EmailAddressAttribute().IsValid(email))
        {
            TempData.Put("AlertMessage", new AlertMessage
            {
                Title = "Error",
                Message = "Invalid email format.",
                AlertType = "danger",
                icon = "fas fa-file",
                icon2 = "fas fa-times"
            });
            return View("Dashboard");
        }

        // Check if the user already exists
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            TempData.Put("AlertMessage", new AlertMessage
            {
                Title = "Error",
                Message = "This email is already registered.",
                AlertType = "danger",
                icon = "fas fa-file",
                icon2 = "fas fa-times"
            });
            return View("Dashboard");
        }

        // Generate a secure temporary password
        var temporaryPassword = "Temp@12345"; // Adjust to match your password policies
        Console.WriteLine($"Generated temporary password: {temporaryPassword}");

        // Create a user with a temporary username and email
        var user = new User { UserName = email, Email = email };
        var result = await _userManager.CreateAsync(user, temporaryPassword);

        if (!result.Succeeded)
        {
            // Log errors if user creation fails
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"Error: {error.Code} - {error.Description}");
            }
            TempData.Put("AlertMessage", new AlertMessage
            {
                Title = "Error",
                Message = "Failed to create user. Please check the logs.",
                AlertType = "danger",
                icon = "fas fa-bug",
                icon2 = "fas fa-times"
            });
            return View("Dashboard");
        }

        // Generate a one-time user token
        var token = await _userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, "InviteUser");
        var expirationDate = DateTime.UtcNow.AddDays(tokenDuration);

        // Generate the callback URL for the user to register
        var callbackUrl = Url.Action(
            "Register", 
            "Admin", 
            new { token, email, expiration = expirationDate.ToString("o") }, 
            Request.Scheme
        );

        Console.WriteLine($"Generated callback URL: {callbackUrl}");

        if (string.IsNullOrEmpty(callbackUrl))
        {
            TempData.Put("AlertMessage", new AlertMessage
            {
                Title = "Error",
                Message = "Error generating invitation link.",
                AlertType = "danger",
                icon = "fas fa-file",
                icon2 = "fas fa-times"
            });
            return View("Dashboard");
        }

        // Send the invitation email
        try
        {
            Console.WriteLine("Attempting to send invitation email...");
            await SendInvitationEmail(email, callbackUrl, tokenDuration);

            TempData.Put("AlertMessage", new AlertMessage
            {
                Title = "Success",
                Message = $"Invitation sent successfully! Token valid for {tokenDuration} day(s).",
                AlertType = "success",
                icon = "fas fa-check-circle",
                icon2 = "fas fa-times"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending email: {ex.Message}");
            TempData.Put("AlertMessage", new AlertMessage
            {
                Title = "Error",
                Message = $"Failed to send email: {ex.Message}",
                AlertType = "danger",
                icon = "fas fa-file",
                icon2 = "fas fa-times"
            });
            return View("Dashboard");
        }

        return RedirectToAction("Dashboard");
    }


#endregion

    //  Login
#region Login
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Login()
    {
        var model = new LoginModel
        {
            Categories = await _categoryRepository.GetAllAsync(),
            Products = await _productRepository.GetRecentProductsAsync(),
        };
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await _categoryRepository.GetAllAsync();
            model.Products = await _productRepository.GetRecentProductsAsync();
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
        {
            var result = await _signInManager.PasswordSignInAsync(user!.UserName!, model.Password, false, false);

            if (result.Succeeded)
            {
                return Redirect(model.ReturnUrl ?? "/Admin/Dashboard");
            }
        }

        ViewBag.ErrorMessage = "Email veya şifre hatalı ya da yetkiniz bulunmuyor.";
        model.Categories = await _categoryRepository.GetAllAsync();
        model.Products = await _productRepository.GetRecentProductsAsync();
        return View(model);
    }
    #endregion
}
