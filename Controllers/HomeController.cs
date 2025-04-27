using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Alpha.Models;
using Data.Abstract;
using Alpha.Services;
using Microsoft.AspNetCore.Localization;
using Alpha.Entity;
using Alpha.EmailServices;
using System.Net;
using System.Net.Mail;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Alpha.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private ICategoryRepository _categoryRepository;
    private ICarouselRepository _carouselRepository;
    private IProductRepository _productRepository;
    private IBlogRepository _blogRepository;
    private LanguageService _localization;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;

    public HomeController(ILogger<HomeController> logger
    ,LanguageService localization
    ,ICategoryRepository categoryRepository
    ,IBlogRepository blogRepository
    ,IProductRepository productRepository
    ,ICarouselRepository carouselRepository
    ,IEmailSender emailSender
    ,IConfiguration configuration
    )
    {
        _logger = logger;
        _productRepository = productRepository;
        _localization = localization;
        _blogRepository = blogRepository;
        _categoryRepository = categoryRepository;
        _carouselRepository = carouselRepository;
        _emailSender = emailSender;
        _configuration = configuration;
    }

    #region SetLanguage
    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
        );
        return Redirect(Request.Headers["Referer"].ToString());
    }

    #endregion


#region Blog

    public async Task<IActionResult> Blog(string category, string brand, string searchTerm, int page = 1, int pageSize = 3)
    {
        // 1. Get all blogs (with related ProductBlogs & CategoryBlogs)
        var allBlogs = await _blogRepository.GetAllAsync();

        // 1a. Get ALL categories directly from the Category table
        //     (this ensures we see every possible category, not just those linked to a blog)
        var allCategories = await _categoryRepository.GetAllAsync();

        // 2. Build a distinct list of all category names from the Category table
        var categoriesList = allCategories
            .Select(c => c.Name)
            .Distinct()
            .ToList();

        // 2a. If you also want to fetch ALL brand names from products, you might do that from:
        //     - a dedicated Brand table (if you have one), OR
        //     - your products in the DB. 
        //     If you only have brand info inside blogs → products, you can do it like this:
        var brandsList = allBlogs
            .SelectMany(b => b.ProductBlogs)
            .Select(pb => pb.Product.Brand)
            .Distinct()
            .ToList();

        // 3. Now apply filters on blogs
        var filteredBlogs = await _blogRepository.SearchWithFiltersAsync(searchTerm, category, brand);


        // 4. Apply pagination to the filtered blogs
        var paginatedBlogs = filteredBlogs
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // 3) Get current language code (like "en", "tr", "de", "fr"...) 
        var currentCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        // 4) For each blog in the final list, fetch translations from .resx
        foreach (var blog in paginatedBlogs)
        {
            // Build your resource keys
            var titleKey = $"Title_{blog.BlogId}_{blog.Url}_{currentCulture}";
            var contentKey = $"Content_{blog.BlogId}_{blog.Url}_{currentCulture}";

            // Attempt to retrieve translation from .resx
            var translatedTitle = _localization.GetKey(titleKey)?.Value;    // or .GetValue() depending on your service
            var translatedContent = _localization.GetKey(contentKey)?.Value;

            // If found, override the default
            if (!string.IsNullOrEmpty(translatedTitle))
                blog.Title = translatedTitle;
            if (!string.IsNullOrEmpty(translatedContent))
                blog.Content = translatedContent;
        }
            // Fallback to some default if the localization is missing:
            var blogListTitle = _localization.GetKey("BlogList_Title")?.Value ?? "Our Blog";
            var blogListDescription = _localization.GetKey("BlogList_Description")?.Value ?? "Explore the latest news...";
            var blogListSearchLabel = _localization.GetKey("BlogList_SearchLabel")?.Value ?? "Search";
            var blogListCategoryLabel = _localization.GetKey("BlogList_CategoryLabel")?.Value ?? "Category";
            var blogListAllCategories = _localization.GetKey("BlogList_AllCategories")?.Value ?? "All Categories";
            var blogListBrandLabel = _localization.GetKey("BlogList_BrandLabel")?.Value ?? "Brand";
            var blogListBrandPlaceholder = _localization.GetKey("BlogList_BrandPlaceholder")?.Value ?? "Enter brand name";
            var blogListApplyFiltersButton = _localization.GetKey("BlogList_ApplyFiltersButton")?.Value ?? "Apply Filters";
            var blogListNoPostsMessage = _localization.GetKey("BlogList_NoPostsMessage")?.Value ?? "No blog posts available at this time. Check back soon!";
            var bloglistreadmore = _localization.GetKey("BlogList_ReadMoreButton")?.Value ??"Read More!";

        // 5. Construct your ViewModel
        var model = new BlogFilterViewModel
        {
            Blogs = paginatedBlogs,

            // Fill the dropdown with ALL categories (not just the ones in use)
            Categories = categoriesList,
            Brands = brandsList,

            Category = category,
            Brand = brand,
            SearchTerm = searchTerm,

            // Pagination
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling((double)filteredBlogs.Count / pageSize),
                // Localized strings
            BlogList_Title = blogListTitle,
            BlogList_Description = blogListDescription,
            BlogList_SearchLabel = blogListSearchLabel,
            BlogList_CategoryLabel = blogListCategoryLabel,
            BlogList_AllCategories = blogListAllCategories,
            BlogList_BrandLabel = blogListBrandLabel,
            BlogList_BrandPlaceholder = blogListBrandPlaceholder,
            BlogList_ApplyFiltersButton = blogListApplyFiltersButton,
            BlogList_NoPostsMessage = blogListNoPostsMessage,
            BlogList_ReadMore = bloglistreadmore
        };

        // 6. Return the View with the model
        return View(model);
    }



    public async Task<IActionResult> BlogDetails(int id, string type)
    {
        if (type == "product")
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View("ProductDetail", product);
        }
        // 3) Get current language code (like "en", "tr", "de", "fr"...) 
        var currentCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        // 1) Fetch the blog record
        var blog = await _blogRepository.GetByIdAsync(id);
        if (blog == null) return NotFound();
        var titleKey = $"Title_{blog.BlogId}_{blog.Url}_{currentCulture}";
        var contentKey = $"Content_{blog.BlogId}_{blog.Url}_{currentCulture}";
        // Attempt to retrieve translation from .resx
        var translatedTitle = _localization.GetKey(titleKey)?.Value; 
        var translatedContent = _localization.GetKey(contentKey)?.Value;
        // If found, override the default
        if (!string.IsNullOrEmpty(translatedTitle))
            blog.Title = translatedTitle;
        if (!string.IsNullOrEmpty(translatedContent))
            blog.Content = translatedContent;
        // 2) Also fetch categories specifically linked to this blog
        //    (assuming blog.CategoryBlogs is loaded via includes)
        var relatedCategories = blog.CategoryBlogs?.Select(cb => cb.Category).ToList();

        // 3) Also fetch products linked to this blog
        //    (assuming blog.ProductBlogs is loaded)
        var relatedProducts = blog.ProductBlogs?.Select(pb => pb.Product).ToList();

        // 4) Localized strings from .resx (you can rename keys as needed)
        var detailPublishedOn  = _localization.GetKey("BlogDetail_PublishedOn")?.Value  ?? "Published on";
        var detailByAuthor     = _localization.GetKey("BlogDetail_ByAuthor")?.Value     ?? "by";
        var detailRecentPosts  = _localization.GetKey("BlogDetail_RecentPosts")?.Value  ?? "Recent Posts";
        var detailCategories   = _localization.GetKey("BlogDetail_CategoriesTitle")?.Value ?? "Categories";
        var detailNextPost     = _localization.GetKey("BlogDetail_NextPost")?.Value     ?? "Next Post";
        var detailPreviousPost = _localization.GetKey("BlogDetail_PreviousPost")?.Value ?? "Previous Post";
        var detailRelCategories= _localization.GetKey("BlogDetail_RelatedCategories")?.Value ?? "Related Categories";
        var detailRelProducts  = _localization.GetKey("BlogDetail_RelatedProducts")?.Value ?? "Related Products";
        var detailEmptyVideo   = _localization.GetKey("BlogDetail_EmptyVideoMsg")?.Value ?? "No video available.";
        var detailEmptyMap     = _localization.GetKey("BlogDetail_EmptyMapMsg")?.Value   ?? "No map available.";
        var emptycategory = _localization.GetKey("BlogDetail_EmptyCategory")?.Value ?? "Commen Use";
        var viewbutton = _localization.GetKey("ViewButton")?.Value ?? "Commen Use";

        // 5) Build the ViewModel
        var model = new BlogDetailsViewModel
        {
            Blog = blog,

            RelatedCategories = relatedCategories,
            RelatedProducts = relatedProducts,

            BlogDetail_PublishedOn = detailPublishedOn,
            BlogDetail_ByAuthor = detailByAuthor,
            BlogDetail_RecentPosts = detailRecentPosts,
            BlogDetail_CategoriesTitle = detailCategories,
            BlogDetail_NextPost = detailNextPost,
            BlogDetail_PreviousPost = detailPreviousPost,
            BlogDetail_RelatedCategories = detailRelCategories,
            BlogDetail_RelatedProducts = detailRelProducts,
            BlogDetail_EmptyVideoMsg = detailEmptyVideo,
            BlogDetail_EmptyMapMsg = detailEmptyMap,
            BlogDetail_EmptyCategory = emptycategory,
            ViewButton = viewbutton
        };

        // 6) Recent blogs for sidebar
        ViewBag.RecentBlogs = await _blogRepository.GetRecentBlogsAsync();

        // 7) All categories (for sidebar)
        ViewBag.Categories = blog.CategoryBlogs?.Select(cb => cb.Category).ToList();


        // 8) Return the strongly-typed view
        return View("BlogDetail", model);
    }


    // Dynamic Load More Blogs (AJAX)
    [HttpGet]
    public async Task<IActionResult> LoadMoreBlogs(string category, string brand, string searchTerm, int page = 1, int pageSize = 3)
    {
        // Use the repository method for filtering
        var filteredBlogs = await _blogRepository.SearchWithFiltersAsync(searchTerm, category, brand);

        // Paginate dynamically
        var paginatedBlogs = filteredBlogs.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return PartialView("_BlogPartial", paginatedBlogs);
    }

#endregion


#region About
  public IActionResult About()
    {
        var model = new AboutViewModel
        {
            // Hero Section
            HeroTitle = _localization.GetKey("About_HeroTitle").Value,
            HeroDescription = _localization.GetKey("About_HeroDescription").Value,

            // Who We Are
            WhoWeAreTitle = _localization.GetKey("About_WhoWeAreTitle").Value,
            WhoWeAreDescription1 = _localization.GetKey("About_WhoWeAreDescription1").Value,
            WhoWeAreDescription2 = _localization.GetKey("About_WhoWeAreDescription2").Value,

            // Core Values
            CoreValuesTitle = _localization.GetKey("About_CoreValuesTitle").Value,
            QualityAssuranceTitle = _localization.GetKey("About_QualityAssuranceTitle").Value,
            QualityAssuranceDescription = _localization.GetKey("About_QualityAssuranceDescription").Value,
            InnovationTitle = _localization.GetKey("About_InnovationTitle").Value,
            InnovationDescription = _localization.GetKey("About_InnovationDescription").Value,
            CustomerFocusTitle = _localization.GetKey("About_CustomerFocusTitle").Value,
            CustomerFocusDescription = _localization.GetKey("About_CustomerFocusDescription").Value,
            SustainabilityTitle = _localization.GetKey("About_SustainabilityTitle").Value,
            SustainabilityDescription = _localization.GetKey("About_SustainabilityDescription").Value,

            // Why Choose Us
            WhyChooseUsTitle = _localization.GetKey("About_WhyChooseUsTitle").Value,
            ExperienceTitle = _localization.GetKey("About_ExperienceTitle").Value,
            ExperienceDescription = _localization.GetKey("About_ExperienceDescription").Value,
            FocusTitle = _localization.GetKey("About_FocusTitle").Value,
            FocusDescription = _localization.GetKey("About_FocusDescription").Value,
            TrustTitle = _localization.GetKey("About_TrustTitle").Value,
            TrustDescription = _localization.GetKey("About_TrustDescription").Value,

            // Product Range
            ProductRangeTitle = _localization.GetKey("About_ProductRangeTitle").Value,
            ProductRangeDescription = _localization.GetKey("About_ProductRangeDescription").Value,
            SafetyShoesTitle = _localization.GetKey("About_SafetyShoesTitle").Value,
            SafetyShoesDescription = _localization.GetKey("About_SafetyShoesDescription").Value,
            IndustrialBootsTitle = _localization.GetKey("About_IndustrialBootsTitle").Value,
            IndustrialBootsDescription = _localization.GetKey("About_IndustrialBootsDescription").Value,
            PersonnelShoesTitle = _localization.GetKey("About_PersonnelShoesTitle").Value,
            PersonnelShoesDescription = _localization.GetKey("About_PersonnelShoesDescription").Value
        };

        return View(model);
    }
#endregion
#region oto mail

    private async Task SendBackEmail(ContactFormViewModel model)
    {
        var subject = "Alpha Admin Invitation";
        var message = $@"
            <h2>Welcome to Alpha Admin Panel</h2>
            <p>You have been invited to join the Alpha Admin system.</p>
            <p>Please click the link below to set up your account:</p>
            <p>
                <a href='' 
                style='display: inline-block; background-color: #ff7b00; color: #fff; 
                        padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                    Complete Registration
                </a>
            </p>
            <p>This invitation link will expire in <strong> day(s)</strong>.</p>
            <p>If you did not expect this email, please ignore it.</p>";

        try
        {
            await _emailSender.SendEmailAsync(model.Email, subject, message);
        }
        catch (Exception ex)
        {
            // Handle exceptions for logging or debugging
            Console.WriteLine($"Error sending email: {ex.Message}");
        }
    }

#endregion

#region Contact

[HttpGet]
public IActionResult Contact()
{
    var model = new ContactViewModel
    {
        Contact_Title               = _localization.GetKey("Contact_Title")?.Value ?? "Contact Us",
        Contact_HeroDescription     = _localization.GetKey("Contact_HeroDescription")?.Value ?? "We're here to assist you.",
        Contact_OurAddress          = _localization.GetKey("Contact_OurAddress")?.Value ?? "Our Address",
        Contact_PhoneNumber         = _localization.GetKey("Contact_PhoneNumber")?.Value ?? "Phone Number",
        Contact_EmailUs             = _localization.GetKey("Contact_EmailUs")?.Value ?? "Email Us",
        Contact_SendUsMessage       = _localization.GetKey("Contact_SendUsMessage")?.Value ?? "Send Us a Message",
        Contact_YourName            = _localization.GetKey("Contact_YourName")?.Value ?? "Your Name",
        Contact_YourEmail           = _localization.GetKey("Contact_YourEmail")?.Value ?? "Your Email",
        Contact_Subject             = _localization.GetKey("Contact_Subject")?.Value ?? "Subject",
        Contact_Message             = _localization.GetKey("Contact_Message")?.Value ?? "Message",
        Contact_SendMessageButton   = _localization.GetKey("Contact_SendMessageButton")?.Value ?? "Send",
        Contact_OurLocation         = _localization.GetKey("Contact_OurLocation")?.Value ?? "Our Location",
        Contact_FollowUsSocialMedia = _localization.GetKey("Contact_FollowUsSocialMedia")?.Value ?? "Follow Us",
        Contact_OurAddressValue     = _localization.GetKey("Contact_OurAddressValue")?.Value ?? "123 Alpha Street",
        Contact_PhoneNumberValue    = _localization.GetKey("Contact_PhoneNumberValue")?.Value ?? "+1 (555) 123-4567",
        Contact_EmailAddressValue   = _localization.GetKey("Contact_EmailAddressValue")?.Value ?? "info@alphasafetyshoes.com"
    };

    return View(model);
}

[HttpPost]
[ActionName("Contact")] // 💡 Bu çok önemli: POST methodu çakışmasın diye!
[ValidateAntiForgeryToken]
public async Task<IActionResult> ContactPost(ContactViewModel model)
{
    if (!ModelState.IsValid)
        return View(model);

    try
    {
        string adminEmail = "owner-email@example.com";
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        string emailRoot = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates");
        string adminTemplatePath = Path.Combine(emailRoot, "AdminNotification.html");

        string userTemplatePath = culture switch
        {
            "en" => Path.Combine(emailRoot, "UserNotification_en.html"),
            "de" => Path.Combine(emailRoot, "UserNotification_de.html"),
            "fr" => Path.Combine(emailRoot, "UserNotification_fr.html"),
            _    => Path.Combine(emailRoot, "UserNotification_tr.html")
        };

        string adminTemplate = await System.IO.File.ReadAllTextAsync(adminTemplatePath);
        string userTemplate = await System.IO.File.ReadAllTextAsync(userTemplatePath);

        string adminBody = adminTemplate
            .Replace("{UserName}", model.Name)
            .Replace("{UserEmail}", model.Email)
            .Replace("{UserMessage}", model.Message);

        string adminSubject = "Yeni İletişim Formu Mesajı";
        string userSubject = culture switch
        {
            "en" => "Thank You for Your Message",
            "de" => "Vielen Dank für Ihre Nachricht",
            "fr" => "Merci pour votre message",
            _    => "Mesajınız İçin Teşekkürler"
        };

        await _emailSender.SendEmailAsync(adminEmail, adminSubject, adminBody);
        await _emailSender.SendEmailAsync(model.Email, userSubject, userTemplate);

        TempData["SuccessMessage"] = _localization.GetKey("ContactSuccessMessage").Value;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Contact form error");
        TempData["ErrorMessage"] = _localization.GetKey("ContactErrorMessage").Value;
    }

    return View(model);
}

#endregion

#region  Services
    public async Task<IActionResult> Services(string category, string brand, string search)
    {
        // Retrieve all products and categories
        var allProducts = await _productRepository.GetAllAsync();
        var categories = await _categoryRepository.GetAllAsync();
        // 1) Retrieve localized strings from your resource service
        var servicesTitle = _localization.GetKey("Services_Title")?.Value ?? "Our Services";
        var servicesHeroDescription = _localization.GetKey("Services_HeroDescription")?.Value 
            ?? "Discover Alpha Safety Shoes' premium products and services...";
        var servicesCatalogFilterTitle = _localization.GetKey("Services_CatalogFilterTitle")?.Value 
            ?? "Catalog & Filters";
        var servicesViewCatalogButton = _localization.GetKey("Services_ViewCatalogButton")?.Value 
            ?? "View Our Catalog";
        var servicesFilterProductsTitle = _localization.GetKey("Services_FilterProductsTitle")?.Value 
            ?? "Filter Products";
        var servicesCategoryLabel = _localization.GetKey("Services_CategoryLabel")?.Value 
            ?? "Category";
        var servicesBrandLabel = _localization.GetKey("Services_BrandLabel")?.Value 
            ?? "Brand";
        var servicesBrandPlaceholder = _localization.GetKey("Services_BrandPlaceholder")?.Value 
            ?? "Enter brand name";
        var servicesSearchProductLabel = _localization.GetKey("Services_SearchProductLabel")?.Value 
            ?? "Search Product";
        var servicesApplyFiltersButton = _localization.GetKey("Services_ApplyFiltersButton")?.Value 
            ?? "Apply Filters";
        var servicesOurProductsTitle = _localization.GetKey("Services_OurProductsTitle")?.Value 
            ?? "Our Products";
        var servicesNoProductsMessage = _localization.GetKey("Services_NoProductsMessage")?.Value 
            ?? "No products found matching your criteria.";

        var Btn = _localization.GetKey("ViewButton")?.Value 
            ?? "View Details";    

        // Filtering Logic
        if (!string.IsNullOrEmpty(category))
        {
            allProducts = allProducts.Where(p => p.Category.Url == category).ToList();
        }

        if (!string.IsNullOrEmpty(brand))
        {
            allProducts = allProducts.Where(p => p.Brand.Contains(brand, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrEmpty(search))
        {
            allProducts = allProducts.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Set ViewBag for input persistence
        ViewBag.SelectedCategory = category;
        ViewBag.Brand = brand;
        ViewBag.Search = search;

        // Pass data to the view
        var viewModel = new ServicesViewModel
        {
            Categories = categories,
            Products = allProducts,
                // Localized strings
                Services_Title = servicesTitle,
                Services_HeroDescription = servicesHeroDescription,
                Services_CatalogFilterTitle = servicesCatalogFilterTitle,
                Services_ViewCatalogButton = servicesViewCatalogButton,
                Services_FilterProductsTitle = servicesFilterProductsTitle,
                Services_CategoryLabel = servicesCategoryLabel,
                Services_BrandLabel = servicesBrandLabel,
                Services_BrandPlaceholder = servicesBrandPlaceholder,
                Services_SearchProductLabel = servicesSearchProductLabel,
                Services_ApplyFiltersButton = servicesApplyFiltersButton,
                Services_OurProductsTitle = servicesOurProductsTitle,
                Services_NoProductsMessage = servicesNoProductsMessage,
                button = Btn
        };

        return View(viewModel);
    }





[HttpGet]
public async Task<IActionResult> ProductDetail(int id)
{
    if (id <= 0)
        return BadRequest("Invalid product ID.");

    // 1) Fetch the product by ID
    var product = await _productRepository.GetByIdAsync(id);
    if (product == null)
        return NotFound("Product not found.");

    // 2) Fetch translations for non-variable text
    ViewBag.BrandLabel = _localization.GetKey("ProductDetail_BrandLabel")?.Value ?? "Brand";
    ViewBag.DescriptionLabel = _localization.GetKey("ProductDetail_DescriptionLabel")?.Value ?? "Description";
    ViewBag.InsoleLabel = _localization.GetKey("ProductDetail_InsoleLabel")?.Value ?? "Insole";
    ViewBag.UpperLabel = _localization.GetKey("ProductDetail_UpperLabel")?.Value ?? "Upper";
    ViewBag.ImagesLabel = _localization.GetKey("ProductDetail_ImagesLabel")?.Value ?? "Images";
    ViewBag.DateAddedLabel = _localization.GetKey("ProductDetail_DateAddedLabel")?.Value ?? "Date Added";
    ViewBag.CategoryLabel = _localization.GetKey("ProductDetail_CategoryLabel")?.Value ?? "Category";
    ViewBag.BodyNoLabel = _localization.GetKey("ProductDetail_BodyNoLabel")?.Value ?? "Body Number";
    ViewBag.BrandFieldLabel = _localization.GetKey("ProductDetail_BrandFieldLabel")?.Value ?? "Brand";
    ViewBag.ModelLabel = _localization.GetKey("ProductDetail_ModelLabel")?.Value ?? "Model";
    ViewBag.SoleLabel = _localization.GetKey("ProductDetail_SoleLabel")?.Value ?? "Sole";
    ViewBag.LiningLabel = _localization.GetKey("ProductDetail_LiningLabel")?.Value ?? "Lining";
    ViewBag.ProtectionLabel = _localization.GetKey("ProductDetail_ProtectionLabel")?.Value ?? "Protection";
    ViewBag.MidsoleLabel = _localization.GetKey("ProductDetail_MidsoleLabel")?.Value ?? "Midsole";
    ViewBag.SizeLabel = _localization.GetKey("ProductDetail_SizeLabel")?.Value ?? "Size";
    ViewBag.CertificateLabel = _localization.GetKey("ProductDetail_CertificateLabel")?.Value ?? "Certificate";
    ViewBag.StandardLabel = _localization.GetKey("ProductDetail_StandardLabel")?.Value ?? "Standard";
    ViewBag.Message = _localization.GetKey("ProductDetail_CategoryLabel").Value?? "Categories";
    ViewBag.BTN = _localization.GetKey("ViewButton")?.Value ?? "Details";

    // 3) Detect the user’s current language
    var currentLang = CultureInfo.CurrentUICulture.Name;

    // 4) Localize translatable fields dynamically
    var prefix = $"Product_{product.ProductId}_";

    string? TryLocalize(string fieldName)
    {
        var key = $"{prefix}{fieldName}_{currentLang}";
        var val = _localization.GetKey(key)?.Value;
        return string.IsNullOrEmpty(val) ? null : val;
    }

    product.Description = TryLocalize("Description") ?? product.Description;
    product.Upper = TryLocalize("Upper") ?? product.Upper;
    product.Insole = TryLocalize("Insole") ?? product.Insole;
    product.Lining = TryLocalize("Lining") ?? product.Lining;
    product.Protection = TryLocalize("Protection") ?? product.Protection;
    product.Midsole = TryLocalize("Midsole") ?? product.Midsole;
    product.Sole = TryLocalize("Sole") ?? product.Sole;

    // 5) Fetch related blogs, categories, and recent products
    var relatedBlogs = product.ProductBlogs?
        .Select(pb => pb.Blog)
        .Where(b => b != null)
        .ToList() ?? new List<Blog>();
    ViewBag.RelatedBlogs = relatedBlogs;

    var relatedCategories = product.ProductCategories?
        .Select(pc => pc.Category)
        .Where(c => c != null)
        .ToList() ?? new List<Category>();
    ViewBag.RelatedCategories = relatedCategories;
        // 5) Map product to ProductDetailViewModel
    var viewModel = new ProductDetailViewModel
    {
        ProductId = product.ProductId,
        Name = product.Name,
        Description = TryLocalize("Description") ?? product.Description,
        Upper = TryLocalize("Upper") ?? product.Upper,
        Insole = TryLocalize("Insole") ?? product.Insole,
        Lining = TryLocalize("Lining") ?? product.Lining,
        Protection = TryLocalize("Protection") ?? product.Protection,
        Midsole = TryLocalize("Midsole") ?? product.Midsole,
        Sole = TryLocalize("Sole") ?? product.Sole,
        Model = product.Model,
        Standard = product.Standard,
        Certificate = product.Certificate,
        Brand = product.Brand,
        Size = product.Size,
        BodyNo = product.BodyNo,
        DateAdded = product.DateAdded,
        ProductImages = product.ProductImages?.ToList() ?? new List<ProductImage>(),
        RelatedBlogs = product.ProductBlogs?.Select(pb => pb.Blog).Where(b => b != null).ToList() ?? new List<Blog>(),
        RelatedCategories = product.ProductCategories?.Select(pc => pc.Category).Where(c => c != null).ToList() ?? new List<Category>(),
        RecentProducts = await _productRepository.GetRecentProductsAsync() ?? new List<Product>(),
        CategoryName = product.ProductCategories?.FirstOrDefault()?.Category?.Name ?? "Unknown Category"
    };

    var recentProducts = await _productRepository.GetRecentProductsAsync();
    ViewBag.RecentProducts = recentProducts ?? new List<Product>();

    // 6) Return the product to the view
    return View("ProductDetail", viewModel);
}





#endregion



#region Index
    public async Task<IActionResult> Index()
    {
        var heroTitle = _localization.GetKey("HeroTitle").Value;
        var heroDescription = _localization.GetKey("HeroDescription").Value;
        var heroLink = _localization.GetKey("HeroLink").Value;
        var CountryQuote = _localization.GetKey("CountryQuote").Value;
        var country1 = _localization.GetKey("country1").Value;
        var country2 = _localization.GetKey("country2").Value;
        var country3 = _localization.GetKey("country3").Value;
        var country4 = _localization.GetKey("country4").Value;  
        var country5 = _localization.GetKey("country5").Value;  
        var country6 = _localization.GetKey("country6").Value;  
        var country7 = _localization.GetKey("country7").Value;  
        var country8 = _localization.GetKey("country8").Value;          
        var country9 = _localization.GetKey("country9").Value;  
        var country10 = _localization.GetKey("country10").Value;  
        var country11 = _localization.GetKey("country11").Value;  
        var country12 = _localization.GetKey("country12").Value;    
        var Talker1 = _localization.GetKey("Talker1").Value;  
        var Talker2 = _localization.GetKey("Talker2").Value; 
        var Talker3 = _localization.GetKey("Talker3").Value;         
        var carousels = await _carouselRepository.GetAllAsync();
        var viewModel = new IndexViewModel
        {
            Carousels = carousels,
            HeroTitle = heroTitle,
            HeroDescription = heroDescription,
            HeroLink = heroLink,
            CountryQuote = CountryQuote,
            contry1 = country1,
            contry2 = country2,
            contry3 = country3,
            contry4 = country4,
            contry5 = country5,
            contry6 = country6,
            contry7 = country7,
            contry8 = country8,
            contry9 = country9,
            contry10 = country10,
            contry11 = country11,
            contry12 = country12                      
        };

        return View(viewModel);
    }


    public async Task<IActionResult> LoadMoreProducts(int page = 1)
    {
        int pageSize = 4;
        var products = await _productRepository.GetAllAsync();

        var pagedProducts = products
            .OrderByDescending(p => p.DateAdded)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return PartialView("_ProductPartial", pagedProducts);
    }
#endregion

#region Privacy
public IActionResult Privacy()
{
    var model = new PrivacyViewModel
    {
        // Hero Section
        Title = _localization.GetKey("Privacy_Title").Value,
        HeroDescription = _localization.GetKey("Privacy_HeroDescription").Value,

        // Information We Collect
        InfoCollectionTitle = _localization.GetKey("Privacy_InfoCollectionTitle").Value,
        InfoCollectionPersonal = _localization.GetKey("Privacy_InfoCollectionPersonal").Value,
        InfoCollectionPayment = _localization.GetKey("Privacy_InfoCollectionPayment").Value,
        InfoCollectionUsage = _localization.GetKey("Privacy_InfoCollectionUsage").Value,
        InfoCollectionCookies = _localization.GetKey("Privacy_InfoCollectionCookies").Value,

        // How We Use Your Information
        UsageTitle = _localization.GetKey("Privacy_UsageTitle").Value,
        UsageIntro = _localization.GetKey("Privacy_UsageIntro").Value,
        UsagePurpose1 = _localization.GetKey("Privacy_UsagePurpose1").Value,
        UsagePurpose2 = _localization.GetKey("Privacy_UsagePurpose2").Value,
        UsagePurpose3 = _localization.GetKey("Privacy_UsagePurpose3").Value,

        // How We Protect Your Data
        ProtectionTitle = _localization.GetKey("Privacy_ProtectionTitle").Value,
        ProtectionIntro = _localization.GetKey("Privacy_ProtectionIntro").Value,
        ProtectionSSL = _localization.GetKey("Privacy_ProtectionSSL").Value,
        ProtectionFirewalls = _localization.GetKey("Privacy_ProtectionFirewalls").Value,
        ProtectionAccess = _localization.GetKey("Privacy_ProtectionAccess").Value,

        // Sharing Information
        SharingIntro =_localization.GetKey("Privacy_SharingIntro").Value,
        SharingTitle = _localization.GetKey("Privacy_SharingTitle").Value,
        SharingThirdParty = _localization.GetKey("Privacy_SharingThirdParty").Value,
        SharingLegal = _localization.GetKey("Privacy_SharingLegal").Value,

        // Your Rights
        RightsTitle = _localization.GetKey("Privacy_RightsTitle").Value,
        RightsAccess = _localization.GetKey("Privacy_RightsAccess").Value,
        RightsDelete = _localization.GetKey("Privacy_RightsDelete").Value,
        RightsOptOut = _localization.GetKey("Privacy_RightsOptOut").Value,

        // Contact Section
        ContactTitle = _localization.GetKey("Privacy_ContactTitle").Value,
        ContactQuestion = _localization.GetKey("Privacy_ContactQuestion").Value,
        ContactEmail = _localization.GetKey("Privacy_ContactEmail").Value,
        ContactPhone = _localization.GetKey("Privacy_ContactPhone").Value,
        ContactAddress = _localization.GetKey("Privacy_ContactAddress").Value
    };

    return View(model);
}
#endregion


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
