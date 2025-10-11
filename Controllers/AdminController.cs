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
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Admin,User")]
public class AdminController : Controller
{
    private static Dictionary<string, List<DateTime>> _loginAttempts = new();
    private static readonly object _rateLimitLock = new();

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
    private readonly IFileProvider _fileProvider;
    private readonly IResxResourceService _resxService;
    private readonly IBlogResxService _blogResxService;
    private readonly IConfiguration _configuration;

    public AdminController(UserManager<User> userManager, SignInManager<User> signInManager, RoleManager<IdentityRole> roleManager,
                           ICategoryRepository categoryRepository, IProductRepository productRepository,
                           AliveResourceService dynamicResourceService,
                           IBlogRepository blogRepository, IImageRepository imageRepository, IEmailSender emailSender,
                           IFileProvider fileProvider, ICarouselRepository carouselRepository,
                           IWebHostEnvironment env, ShopContext dbContext, ILogger<AdminController> logger,
                           IBlogResxService blogResxService,
                           IResxResourceService resxService, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
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
        _env = env;
        _carouselRepository = carouselRepository;
        _resxService = resxService;
        _blogResxService = blogResxService;
    }

    #region Caraousel
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
    public async Task<IActionResult> CarouselCreate(CarouselViewModel model, [FromServices] IResxResourceService resourceService)
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



    [HttpGet]
    public async Task<IActionResult> CarouselEdit(int id)
    {
        Console.WriteLine($"[INFO] Loading edit view for Carousel ID: {id}");

        // Fetch carousel from the database
        var carousel = await _carouselRepository.GetByIdAsync(id);
        if (carousel == null)
        {
            Console.WriteLine($"[WARN] Carousel with ID {id} not found.");
            return NotFound();
        }

        // Fetch translations with null checks and default values
        string GetTranslation(string key, string culture, string defaultValue)
        {
            var value = _resxService.Read(key, culture);
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
            CarouselImage600wPath = carousel.CarouselImage600w,

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

            // AR Translations
            CarouselTitleAR = GetTranslation($"Carousel_{id}_Title", "ar-SA", carousel.CarouselTitle),
            CarouselDescriptionAR = GetTranslation($"Carousel_{id}_Description", "ar-SA", carousel.CarouselDescription),
            CarouselLinkTextAR = GetTranslation($"Carousel_{id}_LinkText", "ar-SA", carousel.CarouselLinkText),
        };

        Console.WriteLine("[INFO] Loaded carousel and translations successfully.");
        return View(model);
    }




    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CarouselEdit(CarouselEditModel model)
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

        // Update carousel properties
        carousel.CarouselTitle = model.CarouselTitle;
        carousel.CarouselDescription = model.CarouselDescription;
        carousel.CarouselLink = model.CarouselLink;
        carousel.CarouselLinkText = model.CarouselLinkText;

        // Image validation and update
        carousel.CarouselImage = await ValidateAndSaveImage(model.CarouselImage, carousel.CarouselImage, "CarouselImage");
        carousel.CarouselImage1200w = await ValidateAndSaveImage(model.CarouselImage1200w, carousel.CarouselImage1200w, "CarouselImage1200w");
        carousel.CarouselImage600w = await ValidateAndSaveImage(model.CarouselImage600w, carousel.CarouselImage600w, "CarouselImage600w");

        // Update translations
        UpdateTranslations(carousel.CarouselId, model);

        // Save carousel
        await _carouselRepository.UpdateAsync(carousel);
        Console.WriteLine($"[INFO] Carousel with ID {model.CarouselId} updated successfully!");
        TempData["SuccessMessage"] = "Carousel updated successfully!";
        return RedirectToAction("Carousels");
    }


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

            // Step 3: Delete translations using IResxResourceService
            var baseKey = $"Carousel_{carousel.CarouselId}";

            // Languages
            string[] cultures = { "en-US", "tr-TR", "de-DE", "fr-FR", "ar-SA" };
            string[] keys = { "Title", "Description", "LinkText" };

            foreach (var culture in cultures)
            {
                foreach (var key in keys)
                {
                    string resourceKey = $"{baseKey}_{key}";
                    var success = _resxService.Delete(resourceKey, culture); // Using IResxResourceService for deletion
                    if (success)
                    {
                        Console.WriteLine($"[INFO] Successfully deleted translation: {resourceKey} in {culture}");
                    }
                    else
                    {
                        Console.WriteLine($"[WARN] Failed to delete translation: {resourceKey} in {culture}");
                    }
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

    #endregion

    #region File Management
    // Helper method for image validation and saving
    private async Task<string> ValidateAndSaveImage(IFormFile image, string existingImagePath, string imageType)
    {
        if (image == null) return existingImagePath;  // No new image uploaded, return the existing one.

        // Validate image format and size
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var fileExtension = Path.GetExtension(image.FileName).ToLower();

        if (!allowedExtensions.Contains(fileExtension) || image.Length > 2 * 1024 * 1024)
        {
            ModelState.AddModelError(imageType, "Invalid image format or size.");
            return existingImagePath;
        }

        // Save new image
        string newImagePath = await SaveFile(image);

        // Delete old image if it exists
        if (!string.IsNullOrEmpty(existingImagePath))
        {
            var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", existingImagePath);
            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
                Console.WriteLine($"[INFO] Deleted old {imageType} image: {oldImagePath}");
            }
        }

        Console.WriteLine($"[INFO] Updated {imageType} image path: {newImagePath}");
        return newImagePath;
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

    #endregion


    #region Products

    [HttpGet("Admin/Products")]
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
        var availableLanguages = await Task.FromResult(new List<string> { "en-US", "de-DE", "fr-FR", "tr-TR", "ar-SA" });
        ViewBag.AvailableLanguages = availableLanguages;
        return View(new ProductCreateModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductCreate(ProductCreateModel e)
    {
        Console.WriteLine($"[INFO] ProductCreate POST called for product: {e.Name}");
        
        if (!ModelState.IsValid)
        {
            Console.WriteLine("[WARN] ModelState is invalid:");
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine($"  - {error.ErrorMessage}");
            }
            
            ViewBag.Categories = (await _categoryRepository.GetAllAsync())
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                }).ToList();
            ViewBag.AvailableLanguages = new List<string> { "en-US", "de-DE", "fr-FR", "tr-TR", "ar-SA" };
            return View(e);
        }

        // 1) build your entity
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

        // 2) save *and* get back the populated ID
        try
        {
            product = await _productRepository.CreateAsync(product);
            Console.WriteLine($"[INFO] Product saved to database with ID: {product.ProductId}");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to save product: {ex.Message}");
            TempData["AlertMessage"] = new AlertMessage
            {
                Title = "Error",
                Message = $"Failed to create product: {ex.Message}",
                AlertType = "danger",
                icon = "fas fa-bug",
                icon2 = "fas fa-times"
            };
            ViewBag.Categories = (await _categoryRepository.GetAllAsync())
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                }).ToList();
            ViewBag.AvailableLanguages = new List<string> { "en-US", "de-DE", "fr-FR", "tr-TR", "ar-SA" };
            return View(e);
        }

        if (product.ProductId <= 0)
        {
            Console.WriteLine("[ERROR] Product ID was not generated after save");
            TempData["AlertMessage"] = new AlertMessage
            {
                Title = "Error",
                Message = "Failed to create product. Product ID was not generated.",
                AlertType = "danger",
                icon = "fas fa-bug",
                icon2 = "fas fa-times"
            };
            ViewBag.Categories = (await _categoryRepository.GetAllAsync())
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                }).ToList();
            ViewBag.AvailableLanguages = new List<string> { "en-US", "de-DE", "fr-FR", "tr-TR", "ar-SA" };
            return View(e);
        }

        // 3) now that ProductId is rock-solid, write all translations
        try
        {
            Console.WriteLine($"[INFO] Adding translations for product ID: {product.ProductId}");
            AddProductTranslations(product.ProductId, e);
            Console.WriteLine("[INFO] Translations added successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to add translations: {ex.Message}");
            // Continue anyway - product is saved, translations can be added later
        }

        Console.WriteLine($"[INFO] Product created successfully with ID: {product.ProductId}. Redirecting to Products page.");
        TempData["SuccessMessage"] = "Product and translations created successfully!";
        return RedirectToAction("Products", "Admin");
    }




    [HttpGet]
    [Route("Admin/ProductEdit/{id:int}")]
    public async Task<IActionResult> ProductEdit(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);

        if (product == null)
        {
            TempData["ErrorMessage"] = "Product not found.";
            return RedirectToAction("Products");
        }

        var cultures = new Dictionary<string, string>
            {
                { "fr-FR", "FR" }, { "en-US", "US" },
                { "de-DE", "DE" }, { "tr-TR", "TR" },
                { "ar-SA", "AR" }
            };

        var fields = new[] { "Description", "Upper", "Lining", "Protection", "Midsole", "Insole", "Sole" };
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

            CategoryIds = product.ProductCategories?.Select(pc => pc.CategoryId).ToList() ?? new(),
            ImageIds = product.ProductImages?.Select(pi => pi.ImageId).ToList() ?? new(),
            BlogIds = product.ProductBlogs?.Select(pb => pb.BlogId).ToList() ?? new(),

            AvailableImages = await _imageRepository.GetAllAsync(),
            AvailableCategories = await _categoryRepository.GetAllAsync(),
            AvailableBlogs = (await _blogRepository.GetAllAsync())
                                .Select(b => new SelectListItem
                                {
                                    Value = b.BlogId.ToString(),
                                    Text = b.Title
                                }).ToList(),

            CurrentImages = product.ProductImages?
                .Where(pi => pi.Image != null)
                .Select(pi => pi.Image)
                .ToList() ?? new()
        };

        // Dynamically set translations using reflection
        foreach (var culture in cultures)
        {
            foreach (var field in fields)
            {
                var prop = model.GetType().GetProperty($"{field}{culture.Value}");
                if (prop != null)
                {
                    // Key WITHOUT culture suffix - culture is in the filename!
                    var translation = _resxService.Read($"Product_{product.ProductId}_{field}", culture.Key);
                    prop.SetValue(model, translation ?? string.Empty);
                }
            }
        }

        return View(model);
    }



    [HttpPost]
    [Route("Admin/ProductEdit")]
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
                .Select(b => new SelectListItem { Value = b.BlogId.ToString(), Text = b.Title })
                .ToList();
            return View(e);
        }

        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var product = await _dbContext.Products
                .Include(p => p.ProductCategories)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductBlogs)
                .FirstOrDefaultAsync(p => p.ProductId == e.ProductId);

            if (product == null)
            {
                Console.WriteLine($"[ERROR] Product with ID {e.ProductId} not found.");
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction("Products");
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
            product.CategoryId = e.CategoryId; // Update primary category

            // Update Categories - Clear and Add
            product.ProductCategories.Clear();
            if (e.CategoryIds != null && e.CategoryIds.Any())
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

            // Update Images - Clear and Add
            product.ProductImages.Clear();
            if (e.ImageIds != null && e.ImageIds.Any())
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

            // Update Blogs - Clear and Add
            product.ProductBlogs.Clear();
            if (e.BlogIds != null && e.BlogIds.Any())
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

            // Save Changes to DbContext first
            await _dbContext.SaveChangesAsync();

            // Commit Transaction
            await transaction.CommitAsync();

            Console.WriteLine($"[SUCCESS] Product ID {e.ProductId} updated successfully.");
            TempData["SuccessMessage"] = "Product updated successfully!";

            return RedirectToAction("Products");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"[ERROR] Failed updating product: {ex.Message}");
            TempData["ErrorMessage"] = "An error occurred during the update.";

            return RedirectToAction("Products");
        }
    }


    [HttpPost]
    public async Task<IActionResult> ProductDelete(int id)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // Step 1: Delete translations
            DeleteProductTranslations(id); // Using IResxResourceService-based method

            // Step 2: Fetch and validate product
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction("Products");
            }

            // Step 3: Clear ALL associations (including ProductBlogs)
            product.ProductCategories?.Clear();
            product.ProductImages?.Clear();
            product.ProductBlogs?.Clear();
            await _productRepository.UpdateAsync(product);

            // Step 4: Delete product
            await _productRepository.DeleteAsync(id);

            // Step 5: Commit
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = "Product and its translations were successfully deleted.";
            return RedirectToAction("Products");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"[ERROR] Failed to delete product {id}: {ex.Message}");

            TempData["ErrorMessage"] = "Failed to delete product. Please try again.";
            return RedirectToAction("Products");
        }
    }

    #endregion



    #region Localization Management
    private void SaveContentToResx(BlogCreateModel model, int id)
    {
        string[] cultures = { "en-US", "tr-TR", "de-DE", "fr-FR", "ar-SA" };
        string[] titles = { model.TitleUS, model.TitleTR, model.TitleDE, model.TitleFR, model.TitleAR };
        string[] contents = { model.ContentUS, model.ContentTR, model.ContentDE, model.ContentFR, model.ContentAR };

        for (int i = 0; i < cultures.Length; i++)
        {
            string culture = cultures[i];
            string title = titles[i];
            string content = contents[i];

            content = ProcessContentImagesForEdit(content);

            if (string.IsNullOrWhiteSpace(model.Url))
            {
                throw new InvalidOperationException("Url cannot be null or empty while saving localization.");
            }
            string urlSlug = model.Url.Trim().ToLowerInvariant();

            // Keys WITHOUT language/culture suffix - culture is in the filename!
            _blogResxService.AddOrUpdate($"Title_{id}_{urlSlug}", title, culture);
            _blogResxService.AddOrUpdate($"Content_{id}_{urlSlug}", content, culture);

            Console.WriteLine($"[DEBUG] Blog translation saved for {culture}: Title_{id}_{urlSlug}");
        }

        Console.WriteLine("[DEBUG] All translations updated successfully.");
    }



    private void SaveAllTranslations(IResxResourceService resourceService, int baseKey, CarouselViewModel model)
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
                { "ar-SA", (model.TranslationsAR.Title,  model.TranslationsAR.Description,  model.TranslationsAR.LinkText) },
            };

        foreach (var translation in translations)
        {
            var culture = translation.Key;
            var (title, description, linkText) = translation.Value;

            // Keys WITHOUT culture suffix (culture is in filename, not key name)
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

    private void SaveTranslation(IResxResourceService resourceService, string key, string value, string culture)
    {
        Console.WriteLine($"[DEBUG] Saving translation: Key='{key}', Value='{value}', Culture='{culture}'");

        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value) || string.IsNullOrEmpty(culture))
        {
            Console.WriteLine($"[ERROR] Invalid translation data. Key: '{key}', Value: '{value}', Culture: '{culture}'");
            return;
        }

        resourceService.AddOrUpdate(key, value, culture); // Save key-value using IResxResourceService
    }

    private void AddProductTranslations(int productId, ProductCreateModel model)
    {
        // Map each culture code to its model-property suffix
        var cultures = new Dictionary<string, string>
            {
                { "fr-FR", "FR" },
                { "en-US", "US" },
                { "de-DE", "DE" },
                { "tr-TR", "TR" },
                { "ar-SA", "AR" }   // ensure Arabic uses “AR”, not “SA”
            };

        // The set of fields we translate
        var fields = new[] { "Description", "Upper", "Lining", "Protection", "Midsole", "Insole", "Sole" };

        foreach (var (culture, suffix) in cultures)
        {
            foreach (var field in fields)
            {
                // e.g. model.DescriptionUS, model.UpperAR, etc.
                var propName = field + suffix;
                var prop = model.GetType().GetProperty(propName);
                var value = prop?.GetValue(model) as string;

                if (!string.IsNullOrWhiteSpace(value))
                {
                    // Key WITHOUT culture suffix - culture is in the filename!
                    var resourceKey = $"Product_{productId}_{field}";
                    _resxService.AddOrUpdate(resourceKey, value, culture);
                }
            }
        }
    }


    private void UpdateTranslations(int id, CarouselEditModel model)
    {
        var baseKey = $"Carousel_{id}";
        var translations = new Dictionary<string, (string Title, string Desc, string LinkText)> {
                { "en-US", (model.CarouselTitleUS, model.CarouselDescriptionUS, model.CarouselLinkTextUS) },
                { "tr-TR", (model.CarouselTitleTR, model.CarouselDescriptionTR, model.CarouselLinkTextTR) },
                { "de-DE", (model.CarouselTitleDE, model.CarouselDescriptionDE, model.CarouselLinkTextDE) },
                { "fr-FR", (model.CarouselTitleFR, model.CarouselDescriptionFR, model.CarouselLinkTextFR) },
                { "ar-SA", (model.CarouselTitleAR, model.CarouselDescriptionAR, model.CarouselLinkTextAR) },
            };

        foreach (var kvp in translations)
        {
            var culture = kvp.Key;
            var (title, desc, linkText) = kvp.Value;

            if (!string.IsNullOrWhiteSpace(title))
                _resxService.AddOrUpdate($"{baseKey}_Title", title, culture);
            if (!string.IsNullOrWhiteSpace(desc))
                _resxService.AddOrUpdate($"{baseKey}_Description", desc, culture);
            if (!string.IsNullOrWhiteSpace(linkText))
                _resxService.AddOrUpdate($"{baseKey}_LinkText", linkText, culture);
        }
    }
    /// <summary>
    /// Updates translations for the product using dynamic properties and resx service.
    /// </summary>
    private void UpdateProductTranslations(ProductEditModel model, int productId)
    {
        var cultures = new Dictionary<string, string>
            {
                { "fr-FR", "FR" },
                { "en-US", "US" },
                { "de-DE", "DE" },
                { "tr-TR", "TR" },
                { "ar-SA", "AR" }
            };

        var fields = new[] { "Description", "Upper", "Lining", "Protection", "Midsole", "Insole", "Sole" };

        foreach (var (culture, suffix) in cultures)
        {
            foreach (var field in fields)
            {
                var propName = field + suffix;
                var prop = model.GetType().GetProperty(propName);

                if (prop == null)
                {
                    Console.WriteLine($"[WARN] Property '{propName}' not found on model.");
                    continue;
                }

                var value = prop.GetValue(model) as string;

                if (!string.IsNullOrWhiteSpace(value))
                {
                    // Key WITHOUT culture suffix - culture is in the filename!
                    var resourceKey = $"Product_{productId}_{field}";
                    _resxService.AddOrUpdate(resourceKey, value, culture);
                    Console.WriteLine($"[INFO] Updated translation: {resourceKey} => {value} in {culture}");
                }
            }
        }
    }


    /// <summary>
    /// Deletes all translations for a given product based on its ID.
    /// </summary>
    /// <param name="productId">The ID of the product whose translations will be deleted.</param>
    private void DeleteProductTranslations(int productId)
    {
        var cultures = new[] { "fr-FR", "en-US", "de-DE", "tr-TR", "ar-SA" };
        var fields = new[] { "Description", "Upper", "Lining", "Protection", "Midsole", "Insole", "Sole" };

        foreach (var culture in cultures)
        {
            foreach (var field in fields)
            {
                // Key WITHOUT culture suffix - culture is in the filename!
                var key = $"Product_{productId}_{field}";
                if (_resxService.Exists(key, culture))
                {
                    try
                    {
                        _resxService.Delete(key, culture);
                        Console.WriteLine($"[INFO] Deleted translation: {key} from {culture}");
                    }
                    catch (IOException ioEx)
                    {
                        Console.WriteLine($"[WARN] Could not delete {key} due to file lock: {ioEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Failed to delete {key}: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"[SKIP] No resource found for {key} in {culture}");
                }
            }
        }
    }


    [HttpGet]
    public IActionResult Localization(string lang = "de-DE")
    {
        var translations = _resxService.LoadAll(lang);
        ViewBag.CurrentLanguage = lang;
        ViewBag.AvailableLanguages = _resxService.GetAvailableLanguages();
        return View(translations);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditTranslation(string name, string value, string comment, string lang)
    {
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            if (string.IsNullOrEmpty(name))
                return Json(new { success = false, message = "Key is missing." });

            var result = _resxService.AddOrUpdate(name, value, lang, comment);
            if (!result)
                return Json(new { success = false, message = "Update failed." });

            return Json(new { success = true, message = "Translation updated successfully." });
        }

        // Eski yol - form gönderimiyse redirect yap
        if (string.IsNullOrEmpty(name))
            return RedirectToAction(nameof(Localization), new { lang, e = "KeyMissing" });

        var success = _resxService.AddOrUpdate(name, value, lang, comment);
        if (!success)
            return RedirectToAction(nameof(Localization), new { lang, e = "UpdateFailed" });

        TempData["Success"] = "Translation updated ✔";
        return RedirectToAction(nameof(Localization), new { lang });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteTranslation(string name, string lang)
    {
        if (string.IsNullOrEmpty(name))
            return RedirectToAction(nameof(Localization), new { lang, e = "KeyMissing" });

        var result = _resxService.Delete(name, lang);
        if (!result)
            return RedirectToAction(nameof(Localization), new { lang, e = "DeleteFailed" });

        TempData["Success"] = "Translation deleted ✔";
        return RedirectToAction(nameof(Localization), new { lang });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddTranslation(string name, string value, string comment, string lang)
    {
        if (string.IsNullOrEmpty(name))
            return RedirectToAction(nameof(Localization), new { lang, e = "KeyMissing" });

        if (_resxService.Exists(name, lang))
            return RedirectToAction(nameof(Localization), new { lang, e = "KeyExists" });

        var result = _resxService.AddOrUpdate(name, value, lang, comment);
        if (!result)
            return RedirectToAction(nameof(Localization), new { lang, e = "AddFailed" });

        TempData["Success"] = "Translation added ✔";
        return RedirectToAction(nameof(Localization), new { lang });
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
    public async Task<IActionResult> Register(string token, string email, string expiration)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(expiration))
        {
            return Forbid(); // Missing required parameters
        }

        if (!DateTime.TryParse(expiration, out var expirationDate) || DateTime.UtcNow > expirationDate)
        {
            return BadRequest("This invitation link has expired.");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        var isValid = await _userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, "InviteUser", token);
        if (!isValid)
        {
            return BadRequest("This invitation token is invalid or has already been used.");
        }

        // Show the registration form
        var model = new RegisterModel
        {
            Email = email
        };
        ViewBag.Token = token;
        ViewBag.Expiration = expiration;

        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterModel model, string token, string expiration)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(expiration))
        {
            return Forbid();
        }

        if (!DateTime.TryParse(expiration, out var expirationDate) || DateTime.UtcNow > expirationDate)
        {
            return BadRequest("This invitation link has expired.");
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        var isValid = await _userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, "InviteUser", token);
        if (!isValid)
        {
            return BadRequest("Invalid or used invitation token.");
        }

        // Set the new password
        var passwordResult = await _userManager.RemovePasswordAsync(user);
        if (!passwordResult.Succeeded)
        {
            ModelState.AddModelError("", "Failed to reset temporary password.");
            return View(model);
        }

        var result = await _userManager.AddPasswordAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);

        TempData["SuccessMessage"] = "Registration completed successfully.";
        return RedirectToAction("Dashboard", "Admin");
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
        ViewBag.TotalUsers = (_userManager.Users.ToList()).Count;
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

    private List<string> ExtractImagesFromTranslations(int blogId, string url)
    {
        List<string> imagePaths = new();
        var cultures = new Dictionary<string, string>
    {
        { "en-US", "en" },
        { "tr-TR", "tr" },
        { "de-DE", "de" },
        { "fr-FR", "fr" },
        { "ar-SA", "ar" }
    };

        foreach (var culture in cultures)
        {
            string contentKey = $"Content_{blogId}_{url}_{culture.Value}";

            // Fetch the translation content using ResxResourceService
            string? translationContent = _resxService.Read(contentKey, culture.Key);

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
            if (string.IsNullOrWhiteSpace(model.Url))
                throw new InvalidOperationException("URL boş olamaz. Blog için benzersiz bir URL gerekli.");

            Console.WriteLine("[ERROR] BlogCreate model validation failed.");

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

        string tempPath = Path.Combine(_env.WebRootPath, "temp");
        string imgPath = Path.Combine(_env.WebRootPath, "blog", "img");
        string gifPath = Path.Combine(_env.WebRootPath, "blog", "gif");
        string coverPath = Path.Combine(_env.WebRootPath, "img");

        Directory.CreateDirectory(tempPath);
        Directory.CreateDirectory(imgPath);
        Directory.CreateDirectory(gifPath);
        Directory.CreateDirectory(coverPath);

        var fileMappings = new Dictionary<string, string>();
        foreach (string file in Directory.GetFiles(tempPath))
        {
            string fileName = Path.GetFileName(file);
            string fileExtension = Path.GetExtension(file).ToLower();

            string destination = fileExtension == ".gif"
                ? Path.Combine(gifPath, fileName)
                : Path.Combine(imgPath, fileName);

            if (!System.IO.File.Exists(destination))
            {
                System.IO.File.Move(file, destination);
                Console.WriteLine($"[DEBUG] Moved content image: {fileName}");
            }

            string tempUrl = $"/temp/{fileName}";
            string finalUrl = $"/blog/{(fileExtension == ".gif" ? "gif" : "img")}/{fileName}";
            fileMappings[tempUrl] = finalUrl;
        }

        string updatedContent = ProcessContentImages(model.Content, fileMappings);
        string updatedContentUS = ProcessContentImages(model.ContentUS, fileMappings);
        string updatedContentTR = ProcessContentImages(model.ContentTR, fileMappings);
        string updatedContentDE = ProcessContentImages(model.ContentDE, fileMappings);
        string updatedContentFR = ProcessContentImages(model.ContentFR, fileMappings);
        string updatedContentAR = ProcessContentImages(model.ContentAR, fileMappings);

        string coverFileName = string.Empty;
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

        var blog = new Blog
        {
            Title = model.Title,
            Content = updatedContent,
            Url = model.Url,
            Image = coverFileName,
            Date = DateTime.Now,
            Author = model.Author,
            RawYT = model.RawYT,
            RawMaps = model.RawMaps,
            CategoryBlogs = model.SelectedCategoryIds.Select(id => new CategoryBlog { CategoryId = id }).ToList(),
            ProductBlogs = model.SelectedProductIds.Select(id => new ProductBlog { ProductId = id }).ToList()
        };

        var result = await _blogRepository.CreateAsync(blog);

        // Save translations - keys WITHOUT language suffix (culture is in filename)
        string idKey = result.BlogId.ToString();
        string urlSlug = model.Url.Trim().ToLowerInvariant();
        
        _blogResxService.AddOrUpdate($"Title_{idKey}_{urlSlug}", model.TitleUS, "en-US");
        _blogResxService.AddOrUpdate($"Content_{idKey}_{urlSlug}", updatedContentUS, "en-US");
        _blogResxService.AddOrUpdate($"Title_{idKey}_{urlSlug}", model.TitleTR, "tr-TR");
        _blogResxService.AddOrUpdate($"Content_{idKey}_{urlSlug}", updatedContentTR, "tr-TR");
        _blogResxService.AddOrUpdate($"Title_{idKey}_{urlSlug}", model.TitleDE, "de-DE");
        _blogResxService.AddOrUpdate($"Content_{idKey}_{urlSlug}", updatedContentDE, "de-DE");
        _blogResxService.AddOrUpdate($"Title_{idKey}_{urlSlug}", model.TitleFR, "fr-FR");
        _blogResxService.AddOrUpdate($"Content_{idKey}_{urlSlug}", updatedContentFR, "fr-FR");
        _blogResxService.AddOrUpdate($"Title_{idKey}_{urlSlug}", model.TitleAR, "ar-SA");
        _blogResxService.AddOrUpdate($"Content_{idKey}_{urlSlug}", updatedContentAR, "ar-SA");

        Console.WriteLine($"[DEBUG] Blog '{result.Title}' saved successfully with ID {result.BlogId}.");
        foreach (var tempFile in Directory.GetFiles(tempPath))
        {
            try { System.IO.File.Delete(tempFile); }
            catch (Exception ex) { Console.WriteLine($"[WARNING] Could not delete temp file: {tempFile} - {ex.Message}"); }
        }
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

        var blog = await _blogRepository.GetByIdAsync(id);
        if (blog == null)
        {
            Console.WriteLine($"[ERROR] Blog with ID {id} not found.");
            return NotFound($"Blog with ID {id} not found.");
        }

        try
        {
            // STEP 1: Delete cover image
            if (!string.IsNullOrEmpty(blog.Image))
            {
                string coverPath = Path.Combine(_env.WebRootPath, "img", blog.Image);
                if (System.IO.File.Exists(coverPath))
                {
                    System.IO.File.Delete(coverPath);
                    Console.WriteLine($"[DEBUG] Deleted cover image: {coverPath}");
                }
            }

            // STEP 2: Delete translated content images
            var cultures = new Dictionary<string, string>
        {
            { "en-US", "en" }, { "tr-TR", "tr" },
            { "de-DE", "de" }, { "fr-FR", "fr" }, { "ar-SA", "ar" }
        };

            foreach (var (culture, langCode) in cultures)
            {
                // Keys WITHOUT language suffix (culture is in filename)
                string urlSlug = blog.Url.Trim().ToLowerInvariant();
                string titleKey = $"Title_{id}_{urlSlug}";
                string contentKey = $"Content_{id}_{urlSlug}";
                
                string? html = _blogResxService.Read(contentKey, culture);

                if (!string.IsNullOrWhiteSpace(html))
                {
                    var imagePaths = ExtractImagePathsFromContent(html);
                    foreach (var path in imagePaths.Distinct())
                    {
                        string fullPath = Path.Combine(_env.WebRootPath, path.TrimStart('/'));
                        if (System.IO.File.Exists(fullPath))
                        {
                            System.IO.File.Delete(fullPath);
                            Console.WriteLine($"[DEBUG] Deleted embedded image: {fullPath}");
                        }
                    }
                }

                // Delete translation keys WITHOUT language suffix
                _blogResxService.Delete(titleKey, culture);
                _blogResxService.Delete(contentKey, culture);
                Console.WriteLine($"[DEBUG] Deleted translations for {culture}: {titleKey}, {contentKey}");
            }

            // STEP 3: Remove many-to-many links (Categories & Products)
            foreach (var cb in blog.CategoryBlogs.ToList())
                _dbContext.CategoryBlog.Remove(cb);

            foreach (var pb in blog.ProductBlogs.ToList())
                _dbContext.ProductBlog.Remove(pb);

            await _dbContext.SaveChangesAsync();

            // STEP 4: Delete main blog entry
            await _blogRepository.DeleteAsync(id);

            // STEP 5: Cleanup orphaned images
            await DeleteOrphanedImages();

            Console.WriteLine($"[SUCCESS] Blog ID {id} deleted successfully.");
            return RedirectToAction("Blogs");
        }
        catch (Exception ex)
        {
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

        // Fetch all categories and products
        var categories = await _categoryRepository.GetAllAsync();
        var products = await _productRepository.GetAllAsync();

        // Get IDs of selected items
        var selectedCategoryIds = blog.CategoryBlogs.Select(cb => cb.CategoryId).ToList();
        var selectedProductIds = blog.ProductBlogs.Select(pb => pb.ProductId).ToList();

        // Populate ViewBags
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

        // Setup supported cultures and language codes
        var cultures = new Dictionary<string, string>
    {
        { "en-US", "en" },
        { "tr-TR", "tr" },
        { "de-DE", "de" },
        { "fr-FR", "fr" },
        { "ar-SA", "ar" }
    };

        // Create a dictionary to store all translation values
        var translations = new Dictionary<string, (string Title, string Content)>();

        foreach (var culture in cultures)
        {
            string cultureKey = culture.Key;
            string langCode = culture.Value;

            // Keys WITHOUT language suffix (culture is in the resx filename)
            string urlSlug = blog.Url.Trim().ToLowerInvariant();
            string titleKey = $"Title_{id}_{urlSlug}";
            string contentKey = $"Content_{id}_{urlSlug}";

            string title = _blogResxService.Read(titleKey, cultureKey) ?? string.Empty;
            string contentRaw = _blogResxService.Read(contentKey, cultureKey) ?? string.Empty;
            string content = ProcessContentImagesForEdit(contentRaw);

            translations[cultureKey] = (title, content);
        }

        // Build and return the model
        var model = new BlogEditModel
        {
            BlogId = blog.BlogId,
            Title = blog.Title,
            Content = ProcessContentImagesForEdit(blog.Content),
            Url = blog.Url,
            Author = blog.Author,
            RawYT = blog.RawYT,
            RawMaps = blog.RawMaps,
            SelectedCategoryIds = selectedCategoryIds,
            SelectedProductIds = selectedProductIds,
            ExistingImage = blog.Image,

            TitleUS = translations["en-US"].Title,
            ContentUS = translations["en-US"].Content,
            TitleTR = translations["tr-TR"].Title,
            ContentTR = translations["tr-TR"].Content,
            TitleDE = translations["de-DE"].Title,
            ContentDE = translations["de-DE"].Content,
            TitleFR = translations["fr-FR"].Title,
            ContentFR = translations["fr-FR"].Content,
            TitleAR = translations["ar-SA"].Title,
            ContentAR = translations["ar-SA"].Content
        };

        Console.WriteLine($"[DEBUG] BlogEdit loaded for Blog ID: {id}");
        return View(model);
    }



    [HttpPost]
    public async Task<IActionResult> BlogEdit(int id, BlogEditModel model)
    {
        if (!ModelState.IsValid)
        {
            if (string.IsNullOrWhiteSpace(model.Url))
            {
                throw new InvalidOperationException("URL boş olamaz. Blog için benzersiz bir URL gerekli.");
            }

            // Re-populate ViewBag
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

        var blog = await _blogRepository.GetByIdAsync(id);
        if (blog == null)
            return NotFound();

        // STEP 1: Extract old images from main + translation content
        List<string> oldImages = new() { blog.Content };
        var cultures = new Dictionary<string, string>
    {
        { "en-US", "en" }, { "tr-TR", "tr" },
        { "de-DE", "de" }, { "fr-FR", "fr" }, { "ar-SA", "ar" }
    };

        string urlSlug = blog.Url.Trim().ToLowerInvariant();
        foreach (var (culture, langCode) in cultures)
        {
            // Keys WITHOUT language suffix (culture is in filename)
            string contentKey = $"Content_{id}_{urlSlug}";
            string? content = _blogResxService.Read(contentKey, culture);
            if (!string.IsNullOrEmpty(content))
            {
                oldImages.Add(content);
            }
        }

        var allOldImagePaths = oldImages
            .SelectMany(ExtractImagePathsFromContent)
            .Distinct()
            .ToList();

        // STEP 2: Move files and map temp images
        string tempPath = Path.Combine(_env.WebRootPath, "temp");
        string imgPath = "/blog/img/";
        string gifPath = "/blog/gif/";
        var fileMappings = Directory.GetFiles(tempPath)
            .ToDictionary(
                file => $"/temp/{Path.GetFileName(file)}",
                file =>
                {
                    var ext = Path.GetExtension(file).ToLower();
                    return ext == ".gif" ? $"{gifPath}{Path.GetFileName(file)}" : $"{imgPath}{Path.GetFileName(file)}";
                }
            );

        // STEP 3: Process new content
        string updatedContent = ProcessContentImages(model.Content, fileMappings);
        var translations = new Dictionary<string, (string Title, string Content)>
    {
        { "en-US", (model.TitleUS, ProcessContentImages(model.ContentUS, fileMappings)) },
        { "tr-TR", (model.TitleTR, ProcessContentImages(model.ContentTR, fileMappings)) },
        { "de-DE", (model.TitleDE, ProcessContentImages(model.ContentDE, fileMappings)) },
        { "fr-FR", (model.TitleFR, ProcessContentImages(model.ContentFR, fileMappings)) },
        { "ar-SA", (model.TitleAR, ProcessContentImages(model.ContentAR, fileMappings)) }
    };

        // STEP 4: Save blog and translations
        blog.Title = model.Title;
        blog.Content = updatedContent;
        blog.Url = model.Url;
        blog.Author = model.Author;
        blog.RawYT = model.RawYT;
        blog.RawMaps = model.RawMaps;

        if (model.ImageFile != null)
        {
            string oldCoverPath = Path.Combine(_env.WebRootPath, "img", blog.Image);
            if (System.IO.File.Exists(oldCoverPath))
                System.IO.File.Delete(oldCoverPath);

            string newImageName = $"{Guid.NewGuid()}{Path.GetExtension(model.ImageFile.FileName)}";
            string newImagePath = Path.Combine(_env.WebRootPath, "img", newImageName);
            using var stream = new FileStream(newImagePath, FileMode.Create);
            await model.ImageFile.CopyToAsync(stream);
            blog.Image = newImageName;
        }

        await _blogRepository.UpdateAsync(blog);

        // Save translations WITHOUT language suffix (culture is in filename)
        string finalUrlSlug = blog.Url.Trim().ToLowerInvariant();
        foreach (var (culture, langCode) in cultures)
        {
            var (title, content) = translations[culture];
            _blogResxService.AddOrUpdate($"Title_{id}_{finalUrlSlug}", title, culture);
            _blogResxService.AddOrUpdate($"Content_{id}_{finalUrlSlug}", content, culture);
        }

        Console.WriteLine($"[DEBUG] Translations and blog updated for ID {id} with keys: Title_{id}_{finalUrlSlug}, Content_{id}_{finalUrlSlug}");
        TempData["SuccessMessage"] = "Blog updated successfully!";
        foreach (var tempFile in Directory.GetFiles(tempPath))
        {
            try { System.IO.File.Delete(tempFile); }
            catch (Exception ex) { Console.WriteLine($"[WARNING] Could not delete temp file: {tempFile} - {ex.Message}"); }
        }
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

        // ModelState'ten ImageUrl'yi çıkarıyoruz, çünkü formdan ImageUrl gelmemeli (gelse bile dikkate alınmamalı)
        ModelState.Remove(nameof(model.ImageUrl));

        if (!ModelState.IsValid)
        {
            Console.WriteLine("[WARN] Model validation failed.");
            return View(model);
        }

        var imageEntity = await _imageRepository.GetByIdAsync(model.ImageId);
        if (imageEntity == null)
        {
            Console.WriteLine($"[WARN] No image found with ID: {model.ImageId}");
            TempData["ErrorMessage"] = "Image not found.";
            return RedirectToAction("Images");
        }

        // Güncellenen alanlar
        imageEntity.Text = model.Text;
        imageEntity.ViewPhone = model.ViewPhone;

        if (newImageFile != null && newImageFile.Length > 0)
        {
            try
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "img");

                // Eski görsel varsa sil
                if (!string.IsNullOrEmpty(imageEntity.ImageUrl))
                {
                    var oldFileName = Path.GetFileName(imageEntity.ImageUrl); // Güvenli biçimde dosya adını al
                    var oldImagePath = Path.Combine(uploadsFolder, oldFileName);

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                        Console.WriteLine($"[INFO] Deleted old image file: {oldImagePath}");
                    }
                }

                // Yeni dosyayı kaydet
                var newFileName = $"{Guid.NewGuid()}{Path.GetExtension(newImageFile.FileName)}";
                var newImagePath = Path.Combine(uploadsFolder, newFileName);

                using (var stream = new FileStream(newImagePath, FileMode.Create))
                {
                    await newImageFile.CopyToAsync(stream);
                }

                Console.WriteLine($"[INFO] Saved new image file: {newImagePath}");

                // Yeni URL'yi kaydet (ön yüzde gösterilecek olan yol)
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
                // 1. Yükleme klasörü belirle
                var uploadsFolder = Path.Combine(_env.WebRootPath, "img");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // 2. Dosya ismi oluştur
                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(model.File.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // 3. Dosyayı diske kaydet
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.File.CopyToAsync(stream);
                }

                Console.WriteLine($"✅[INFO] Uploaded new image file: {filePath}");

                // 4. Image entity oluştur ve veritabanına kaydet
                var image = new Image
                {
                    ImageUrl = $"/img/{uniqueFileName}", //  Doğru yol formatı
                    Text = model.Text,
                    DateAdded = DateTime.UtcNow,
                    ViewPhone = model.ViewPhone
                };

                await _imageRepository.CreateAsync(image);
                TempData["SuccessMessage"] = "✅Image uploaded successfully!";
                return RedirectToAction("Images");
            }

            TempData["ErrorMessage"] = "File is required.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to upload image. Exception: {ex.Message}");
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

        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // Step 1: Retrieve the image with its relationships
            var image = await _dbContext.Images
                .Include(i => i.ProductImages)
                .FirstOrDefaultAsync(i => i.ImageId == id);

            if (image == null)
            {
                Console.WriteLine($"[WARN] No image found with ID: {id}");
                TempData["ErrorMessage"] = "Image not found.";
                return RedirectToAction("Images");
            }

            Console.WriteLine($"[INFO] Image found: ID={image.ImageId}, ImageUrl={image.ImageUrl}, Text={image.Text}");

            // Step 2: Remove product associations
            if (image.ProductImages != null && image.ProductImages.Any())
            {
                Console.WriteLine($"[INFO] Image is associated with {image.ProductImages.Count} products. Removing associations...");
                _dbContext.Set<ProductImage>().RemoveRange(image.ProductImages);
                await _dbContext.SaveChangesAsync();
                Console.WriteLine("[INFO] Product associations removed.");
            }

            // Step 3: Delete the physical file
            if (!string.IsNullOrEmpty(image.ImageUrl))
            {
                // ImageUrl is stored as "/img/filename.png", so we need to convert it to a proper file path
                // Remove leading slash and convert forward slashes to backslashes for Windows
                var relativePath = image.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var imgPath = Path.Combine(_env.WebRootPath, relativePath);
                Console.WriteLine($"[INFO] Attempting to delete file at: {imgPath}");

                if (System.IO.File.Exists(imgPath))
                {
                    try
                    {
                        System.IO.File.Delete(imgPath);
                        Console.WriteLine($"[SUCCESS] Deleted image file: {imgPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Failed to delete file: {ex.Message}");
                        // Continue with database deletion even if file delete fails
                    }
                }
                else
                {
                    Console.WriteLine($"[WARN] File not found at: {imgPath}");
                }
            }

            // Step 4: Delete the image record from database
            Console.WriteLine($"[INFO] Deleting image record with ID: {id}");
            _dbContext.Images.Remove(image);
            await _dbContext.SaveChangesAsync();

            // Step 5: Commit transaction
            await transaction.CommitAsync();

            Console.WriteLine($"[SUCCESS] Image with ID {id} deleted successfully.");
            TempData["SuccessMessage"] = "Image and its file were successfully deleted.";
            return RedirectToAction("Images");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"[ERROR] Failed to delete image: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            TempData["ErrorMessage"] = $"An error occurred while deleting the image: {ex.Message}";
            return RedirectToAction("Images");
        }
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
            // View'a gerekli veriler
            async Task PopulateModel(LoginModel m)
            {
                m.Categories = await _categoryRepository.GetAllAsync();
                m.Products = await _productRepository.GetRecentProductsAsync();
                m.RecaptchaSiteKey = _configuration["reCAPTCHA:GoogleSiteKey"];
            }
            if (!string.IsNullOrEmpty(model.Honey))
            {
                ModelState.AddModelError("", "Bot activity detected.");
                return View(model);
            }
            // IP Adresini Al
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            bool isLimited = false;

            // RATE LIMIT: Son 5 dakika içinde 5+ giriş denemesi yapanları engelle
            lock (_rateLimitLock)
            {
                if (!_loginAttempts.ContainsKey(userIp))
                    _loginAttempts[userIp] = new List<DateTime>();

                // Sadece son 5 dakikadaki denemeleri tut
                _loginAttempts[userIp] = _loginAttempts[userIp]
                    .Where(dt => (DateTime.UtcNow - dt).TotalMinutes < 5)
                    .ToList();

                if (_loginAttempts[userIp].Count >= 5)
                    isLimited = true;
                else
                    _loginAttempts[userIp].Add(DateTime.UtcNow);
            }

            if (isLimited)
            {
                ViewBag.ErrorMessage = "Çok sık deneme yaptınız. Lütfen 5 dakika bekleyin.";
                await PopulateModel(model);
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                await PopulateModel(model);
                return View(model);
            }

            // reCAPTCHA kontrolü
            if (!Request.Form.TryGetValue("g-recaptcha-response", out var token)
                || string.IsNullOrWhiteSpace(token.ToString())
                || !await VerifyCaptchaAsync(token.ToString()))
            {
                ViewBag.ErrorMessage = "Lütfen robot olmadığınızı doğrulayın.";
                await PopulateModel(model);
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, false, true);

                if (result.IsLockedOut)
                {
                    ViewBag.ErrorMessage = "Çok fazla başarısız deneme. Lütfen daha sonra tekrar deneyiniz.";
                    await PopulateModel(model);
                    return View(model);
                }
                if (result.Succeeded)
                {
                    return Redirect(model.ReturnUrl ?? "/Admin/Dashboard");
                }
            }

            ViewBag.ErrorMessage = "Email veya şifre hatalı ya da yetkiniz bulunmuyor.";
            await PopulateModel(model);
            return View(model);
        }
    
    private async Task<bool> VerifyCaptchaAsync(string token)
    {
        var _recaptchaSecret = _configuration["reCAPTCHA:SecretKey"];

        using var client = new HttpClient();
        var response = await client.PostAsync(
            $"https://www.google.com/recaptcha/api/siteverify?secret={_recaptchaSecret}&response={token}",
            null);
        var json = await response.Content.ReadAsStringAsync();

        dynamic? result = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
        bool success = result?.success == true;
        return success;
    }

    #endregion
}
