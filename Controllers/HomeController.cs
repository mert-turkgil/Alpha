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
    // Rate limit için (en üste veya class’ın başına):
    private static Dictionary<string, DateTime> _contactRateLimit = new();
    private static readonly object _rateLimitLock = new();

    private readonly ILogger<HomeController> _logger;
    private ICategoryRepository _categoryRepository;
    private ICarouselRepository _carouselRepository;
    private IProductRepository _productRepository;
    private IBlogRepository _blogRepository;
    private LanguageService _localization;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly IBlogResxService _blogResxService;
    private readonly IResxResourceService _resxService;

    public HomeController(ILogger<HomeController> logger
    ,LanguageService localization
    ,ICategoryRepository categoryRepository
    ,IBlogRepository blogRepository
    ,IProductRepository productRepository
    ,ICarouselRepository carouselRepository
    ,IEmailSender emailSender
    ,IConfiguration configuration
    , IBlogResxService blogResxService
    , IResxResourceService resxService
    )
    {
        _configuration = configuration;
        _logger = logger;
        _productRepository = productRepository;
        _localization = localization;
        _blogRepository = blogRepository;
        _categoryRepository = categoryRepository;
        _carouselRepository = carouselRepository;
        _emailSender = emailSender;
        _blogResxService = blogResxService;
        _resxService = resxService;
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
    [HttpGet("{culture}/blog")]
    public async Task<IActionResult> Blog(string culture,string category, string brand, string searchTerm, int page = 1, int pageSize = 3)
    {
        // Map short culture code to full culture code
        var fullCulture = culture.ToLower() switch
        {
            "tr" => "tr-TR",
            "en" => "en-US",
            "de" => "de-DE",
            "fr" => "fr-FR",
            "ar" => "ar-SA",
            _ => "en-US"
        };
        
        // 🌍 Dil ayarını uygula
        CultureInfo.CurrentUICulture = new CultureInfo(fullCulture);
        CultureInfo.CurrentCulture = new CultureInfo(fullCulture);
        ViewBag.Title = _localization.GetKey("SEO_Blog_Title") 
                        ?? "Blog | İş Güvenliği Rehberleri ve Haberler | Alpha Ayakkabı";

        ViewBag.MetaDescription = _localization.GetKey("SEO_Blog_Description") 
                        ?? "İş güvenliği, askeri botlar ve koruyucu ekipmanlar hakkında ipuçları, haberler ve rehber içerikler Alpha Blog’da.";

        ViewBag.MetaKeywords = _localization.GetKey("SEO_Blog_Keywords") 
                        ?? "iş güvenliği blogu, askeri botlar, alpha ayakkabı, güvenlik ayakkabısı, çalışma güvenliği";


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

        // 3) Get current culture name (e.g., "en-US", "tr-TR") 
        var cultureName = CultureInfo.CurrentCulture.Name;

        // 4) For each blog in the final list, fetch translations from BlogResources
        foreach (var blog in paginatedBlogs)
        {
            // Build resource keys WITHOUT culture suffix
            var titleKey = $"Title_{blog.BlogId}_{blog.Url}";
            var contentKey = $"Content_{blog.BlogId}_{blog.Url}";

            // Retrieve translation from BlogResources (culture determined by current culture)
            var translatedTitle = _blogResxService.Read(titleKey, cultureName);
            var translatedContent = _blogResxService.Read(contentKey, cultureName);

            // If found, override the default
            if (!string.IsNullOrEmpty(translatedTitle))
                blog.Title = translatedTitle;
            if (!string.IsNullOrEmpty(translatedContent))
                blog.Content = translatedContent;
        }
            // Fallback to some default if the localization is missing:
            var blogListTitle = _localization.GetKey("BlogList_Title") ?? "Our Blog";
            var blogListDescription = _localization.GetKey("BlogList_Description") ?? "Explore the latest news...";
            var blogListSearchLabel = _localization.GetKey("BlogList_SearchLabel") ?? "Search";
            var blogListCategoryLabel = _localization.GetKey("BlogList_CategoryLabel") ?? "Category";
            var blogListAllCategories = _localization.GetKey("BlogList_AllCategories") ?? "All Categories";
            var blogListBrandLabel = _localization.GetKey("BlogList_BrandLabel") ?? "Brand";
            var blogListBrandPlaceholder = _localization.GetKey("BlogList_BrandPlaceholder") ?? "Enter brand name";
            var blogListApplyFiltersButton = _localization.GetKey("BlogList_ApplyFiltersButton") ?? "Apply Filters";
            var blogListNoPostsMessage = _localization.GetKey("BlogList_NoPostsMessage") ?? "No blog posts available at this time. Check back soon!";
            var bloglistreadmore = _localization.GetKey("BlogList_ReadMoreButton") ?? "Read More!";

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



    [HttpGet("{culture}/blog/{id}/{slug?}")]
    public async Task<IActionResult> BlogDetails(string culture, int id, string? slug)
    {
        // Map short culture code to full culture code
        var fullCulture = culture.ToLower() switch
        {
            "tr" => "tr-TR",
            "en" => "en-US",
            "de" => "de-DE",
            "fr" => "fr-FR",
            "ar" => "ar-SA",
            _ => "en-US"
        };
        
        CultureInfo.CurrentUICulture = new CultureInfo(fullCulture);
        CultureInfo.CurrentCulture = new CultureInfo(fullCulture);

        var blog = await _blogRepository.GetByIdAsync(id);
        if (blog == null)
            return NotFound();

        var expectedSlug = blog.Url?.ToLower() ?? "blog";

        // Slug yanlışsa düzelt
        if (!string.Equals(slug, expectedSlug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("BlogDetails", new { culture = culture, id = blog.BlogId, slug = expectedSlug });
        }

        // Localization - keys WITHOUT culture suffix, use full culture code for file lookup
        var titleKey = $"Title_{blog.BlogId}_{blog.Url}";
        var contentKey = $"Content_{blog.BlogId}_{blog.Url}";

        var translatedTitle = _blogResxService.Read(titleKey, fullCulture);
        var translatedContent = _blogResxService.Read(contentKey, fullCulture);
        
        Console.WriteLine($"[BlogDetails] Reading blog {id} with culture {fullCulture}");
        Console.WriteLine($"[BlogDetails] TitleKey: {titleKey}, Found: {!string.IsNullOrWhiteSpace(translatedTitle)}");
        Console.WriteLine($"[BlogDetails] ContentKey: {contentKey}, Found: {!string.IsNullOrWhiteSpace(translatedContent)}");

        if (!string.IsNullOrWhiteSpace(translatedTitle))
            blog.Title = translatedTitle;
        if (!string.IsNullOrWhiteSpace(translatedContent))
            blog.Content = translatedContent;

        ViewBag.Title = $"{blog.Title} | Alpha Blog";
        ViewBag.MetaDescription = blog.Content?.Length > 160
            ? blog.Content.Substring(0, 160) + "..."
            : blog.Content;
        ViewBag.MetaKeywords = $"alpha blog, {blog.Title}, iş güvenliği, güvenlik ayakkabısı";

        var relatedCategories = blog.CategoryBlogs?.Select(cb => cb.Category).ToList();
        var relatedProducts = blog.ProductBlogs?.Select(pb => pb.Product).ToList();

        var model = new BlogDetailsViewModel
        {
            Blog = blog,
            RelatedCategories = relatedCategories ?? new(),
            RelatedProducts = relatedProducts ?? new(),

            BlogDetail_PublishedOn = _localization.GetKey("BlogDetail_PublishedOn") ?? "Published on",
            BlogDetail_ByAuthor = _localization.GetKey("BlogDetail_ByAuthor") ?? "by",
            BlogDetail_RecentPosts = _localization.GetKey("BlogDetail_RecentPosts") ?? "Recent Posts",
            BlogDetail_CategoriesTitle = _localization.GetKey("BlogDetail_CategoriesTitle") ?? "Categories",
            BlogDetail_NextPost = _localization.GetKey("BlogDetail_NextPost") ?? "Next Post",
            BlogDetail_PreviousPost = _localization.GetKey("BlogDetail_PreviousPost") ?? "Previous Post",
            BlogDetail_RelatedCategories = _localization.GetKey("BlogDetail_RelatedCategories") ?? "Related Categories",
            BlogDetail_RelatedProducts = _localization.GetKey("BlogDetail_RelatedProducts") ?? "Related Products",
            BlogDetail_EmptyVideoMsg = _localization.GetKey("BlogDetail_EmptyVideoMsg") ?? "No video available.",
            BlogDetail_EmptyMapMsg = _localization.GetKey("BlogDetail_EmptyMapMsg") ?? "No map available.",
            BlogDetail_EmptyCategory = _localization.GetKey("BlogDetail_EmptyCategory") ?? "General",
            ViewButton = _localization.GetKey("ViewButton") ?? "View"
        };

        ViewBag.RecentBlogs = await _blogRepository.GetRecentBlogsAsync();
        ViewBag.Categories = relatedCategories;

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
  [HttpGet("{culture}/about")]
  public IActionResult About(string culture)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        ViewBag.Title = _localization.GetKey("SEO_About_Title") 
                        ?? "Hakkımızda | Alpha Güvenlik Ayakkabıları";

        ViewBag.MetaDescription = _localization.GetKey("SEO_About_Description") 
                        ?? "Alpha Ayakkabı olarak, iş güvenliği ve konforu bir araya getiren yüksek kaliteli ayakkabılar üretiyoruz. Biz kimiz, neden tercih edilmeliyiz, öğrenin.";

        ViewBag.MetaKeywords = _localization.GetKey("SEO_About_Keywords") 
                        ?? "hakkımızda, alpha ayakkabı, iş güvenliği, güvenlikli ayakkabı, askeri bot";


        var model = new AboutViewModel
        {
            // Hero Section
            HeroTitle = _localization.GetKey("About_HeroTitle"),
            HeroDescription = _localization.GetKey("About_HeroDescription"),

            // Who We Are
            WhoWeAreTitle = _localization.GetKey("About_WhoWeAreTitle"),
            WhoWeAreDescription1 = _localization.GetKey("About_WhoWeAreDescription1"),
            WhoWeAreDescription2 = _localization.GetKey("About_WhoWeAreDescription2"),

            // Core Values
            CoreValuesTitle = _localization.GetKey("About_CoreValuesTitle"),
            QualityAssuranceTitle = _localization.GetKey("About_QualityAssuranceTitle"),
            QualityAssuranceDescription = _localization.GetKey("About_QualityAssuranceDescription"),
            InnovationTitle = _localization.GetKey("About_InnovationTitle"),
            InnovationDescription = _localization.GetKey("About_InnovationDescription"),
            CustomerFocusTitle = _localization.GetKey("About_CustomerFocusTitle"),
            CustomerFocusDescription = _localization.GetKey("About_CustomerFocusDescription"),
            SustainabilityTitle = _localization.GetKey("About_SustainabilityTitle"),
            SustainabilityDescription = _localization.GetKey("About_SustainabilityDescription"),

            // Why Choose Us
            WhyChooseUsTitle = _localization.GetKey("About_WhyChooseUsTitle"),
            ExperienceTitle = _localization.GetKey("About_ExperienceTitle"),
            ExperienceDescription = _localization.GetKey("About_ExperienceDescription"),
            FocusTitle = _localization.GetKey("About_FocusTitle"),
            FocusDescription = _localization.GetKey("About_FocusDescription"),
            TrustTitle = _localization.GetKey("About_TrustTitle"),
            TrustDescription = _localization.GetKey("About_TrustDescription"),

            // Product Range
            ProductRangeTitle = _localization.GetKey("About_ProductRangeTitle"),
            ProductRangeDescription = _localization.GetKey("About_ProductRangeDescription"),
            SafetyShoesTitle = _localization.GetKey("About_SafetyShoesTitle"),
            SafetyShoesDescription = _localization.GetKey("About_SafetyShoesDescription"),
            IndustrialBootsTitle = _localization.GetKey("About_IndustrialBootsTitle"),
            IndustrialBootsDescription = _localization.GetKey("About_IndustrialBootsDescription"),
            PersonnelShoesTitle = _localization.GetKey("About_PersonnelShoesTitle"),
            PersonnelShoesDescription = _localization.GetKey("About_PersonnelShoesDescription")
        };

        return View(model);
    }
#endregion
#region oto mail

    /// <summary>
    /// Sends confirmation email to user after contact form submission
    /// </summary>
    private async Task SendBackEmail(ContactFormViewModel model)
    {
        try
        {
            // Get current culture to determine which email template to use
            var culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            
            // Map culture to template file
            var templateFile = culture.ToLower() switch
            {
                "tr" => "UserNotification_tr.html",
                "de" => "UserNotification_de.html",
                "fr" => "UserNotification_fr.html",
                "ar" => "UserNotification_ar.html",
                _ => "UserNotification_en.html"
            };

            // Set subject based on culture
            var subject = culture.ToLower() switch
            {
                "tr" => "Mesajınız Alındı - Alpha Ayakkabı",
                "de" => "Ihre Nachricht wurde empfangen - Alpha Ayakkabı",
                "fr" => "Votre message a été reçu - Alpha Ayakkabı",
                "ar" => "تم استلام رسالتك - Alpha Ayakkabı",
                _ => "Your Message Has Been Received - Alpha Ayakkabı"
            };

            // Read email template from file
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", templateFile);
            
            if (!System.IO.File.Exists(templatePath))
            {
                Console.WriteLine($"[EMAIL WARNING] Template not found: {templatePath}, using default");
                templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "UserNotification_en.html");
            }

            var emailBody = await System.IO.File.ReadAllTextAsync(templatePath);

            // Send confirmation email to user
            await _emailSender.SendEmailAsync(model.Email, subject, emailBody);
            
            Console.WriteLine($"[EMAIL] Confirmation sent to user: {model.Email} (Culture: {culture})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EMAIL ERROR] Failed to send confirmation to user: {ex.Message}");
            // Don't throw - we don't want to fail the contact form submission if confirmation email fails
        }
    }

#endregion

#region Contact
        private void PopulateContactViewModel(ContactViewModel m)
        {
            m.Contact_Title               = _localization.GetKey("Contact_Title")               ?? "Contact Us";
            m.Contact_HeroDescription     = _localization.GetKey("Contact_HeroDescription")     ?? "We're here to assist you.";
            m.Contact_OurAddress          = _localization.GetKey("Contact_OurAddress")          ?? "Our Address";
            m.Contact_PhoneNumber         = _localization.GetKey("Contact_PhoneNumber")         ?? "Phone Number";
            m.Contact_EmailUs             = _localization.GetKey("Contact_EmailUs")             ?? "Email Us";
            m.Contact_SendUsMessage       = _localization.GetKey("Contact_SendUsMessage")       ?? "Send Us a Message";
            m.Contact_YourName            = _localization.GetKey("Contact_YourName")            ?? "Your Name";
            m.Contact_YourEmail           = _localization.GetKey("Contact_YourEmail")           ?? "Your Email";
            m.Contact_Subject             = _localization.GetKey("Contact_Subject")             ?? "Subject";
            m.Contact_Message             = _localization.GetKey("Contact_Message")             ?? "Message";
            m.Contact_SendMessageButton   = _localization.GetKey("Contact_SendMessageButton")   ?? "Send";
            m.Contact_OurLocation         = _localization.GetKey("Contact_OurLocation")         ?? "Our Location";
            m.Contact_FollowUsSocialMedia = _localization.GetKey("Contact_FollowUsSocialMedia") ?? "Follow Us";
            m.Contact_UseFormBelow        = _localization.GetKey("Contact_UseFormBelow")        ?? "Please use the contact form below";

            // İletişim detayları
            m.Contact_OurAddressValue     = _localization.GetKey("Contact_OurAddressValue")     ?? "123 Alpha Street";
            m.Contact_PhoneNumberValue    = _localization.GetKey("Contact_PhoneNumberValue")    ?? "+1 (555) 123-4567";
            m.Contact_EmailAddressValue   = _localization.GetKey("Contact_EmailAddressValue")   ?? "info@alphaayakkabi.com";
        }

        /// <summary>
        /// GET: /Home/Contact
        /// </summary>
        [HttpGet("{culture}/contact")]
        public IActionResult Contact(string culture)
        {
            CultureInfo.CurrentUICulture = new CultureInfo(culture);
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            ViewBag.Title = _localization.GetKey("SEO_Contact_Title") 
                            ?? "İletişim | Alpha Güvenlik Ayakkabıları";
            ViewBag.MetaDescription = _localization.GetKey("SEO_Contact_Description") 
                            ?? "Bize ulaşın. Alpha Ayakkabı ile iletişime geçin ve iş güvenliği çözümlerimizi keşfedin.";
            ViewBag.MetaKeywords = _localization.GetKey("SEO_Contact_Keywords") 
                            ?? "iletişim, alpha ayakkabı, iş güvenliği, güvenlikli ayakkabı, ulaşım";
            var model = new ContactViewModel
            {
                RecaptchaSiteKey = _configuration["reCAPTCHA:SiteKey"] ?? "",
                CloudflareTurnstileSiteKey = _configuration["Cloudflare:TurnstileSiteKey"] ?? ""
            };
            PopulateContactViewModel(model);
            return View(model);
        }

        /// <summary>
        /// POST: /Home/Contact
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Contact")]
        public async Task<IActionResult> ContactPost(ContactViewModel model)
        {
            var _recaptchaSiteKey = _configuration["reCAPTCHA:SiteKey"];
            model.RecaptchaSiteKey = _recaptchaSiteKey;
            PopulateContactViewModel(model);

            // 1) HoneyPot kontrolü
            if (!string.IsNullOrEmpty(model.Honey))
            {
                ModelState.AddModelError("", "Bot activity detected.");
                return View(model);
            }

            // 2) Rate Limit kontrolü - Increased to 2 minutes
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            lock (_rateLimitLock)
            {
                if (_contactRateLimit.TryGetValue(userIp, out var lastSubmit))
                {
                    if (DateTime.UtcNow - lastSubmit < TimeSpan.FromMinutes(2))
                    {
                        PopulateContactViewModel(model);
                        ModelState.AddModelError("", "Çok sık deneme yaptınız. Lütfen 2 dakika bekleyin.");
                        return View(model);
                    }
                }
                _contactRateLimit[userIp] = DateTime.UtcNow;
            }

            // 3) Cloudflare Turnstile kontrolü (replaces reCAPTCHA)
            if (!Request.Form.TryGetValue("cf-turnstile-response", out var token)
                || string.IsNullOrWhiteSpace(token.ToString())
                || !await VerifyTurnstileAsync(token.ToString(), userIp))
            {
                PopulateContactViewModel(model);
                ModelState.AddModelError(string.Empty, "Bot doğrulama başarısız. Lütfen tekrar deneyin.");
                return View(model);
            }

            // 4) Email format validation
            if (!IsValidEmail(model.Email))
            {
                PopulateContactViewModel(model);
                ModelState.AddModelError("Email", "Geçersiz e-posta formatı. Lütfen geçerli bir e-posta adresi girin.");
                return View(model);
            }

            // 5) Public email provider validation (no bot/temporary emails)
            if (!IsPublicEmailProvider(model.Email))
            {
                PopulateContactViewModel(model);
                ModelState.AddModelError("Email", "Lütfen Gmail, Yahoo, Outlook gibi geçerli bir e-posta sağlayıcısı kullanın.");
                return View(model);
            }

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // --- Admin notification email using HTML template ---
                var adminEmail = _configuration.GetValue<string>("EmailSender:AdminNotificationEmail") 
                              ?? _configuration.GetValue<string>("EmailSender:Username") 
                              ?? "info@alphaayakkabi.com";
                              
                var adminSubject = "Yeni İletişim Formu Mesajı";
                
                // Read admin email template
                var adminTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "AdminNotification.html");
                string adminBody;
                
                if (System.IO.File.Exists(adminTemplatePath))
                {
                    adminBody = await System.IO.File.ReadAllTextAsync(adminTemplatePath);
                    
                    // Replace placeholders with actual data (sanitized for XSS)
                    adminBody = adminBody
                        .Replace("{UserName}", System.Net.WebUtility.HtmlEncode(model.Name))
                        .Replace("{UserEmail}", System.Net.WebUtility.HtmlEncode(model.Email))
                        .Replace("{UserMessage}", System.Net.WebUtility.HtmlEncode(model.Message).Replace("\n", "<br/>"));
                }
                else
                {
                    // Fallback to inline HTML if template not found
                    adminBody = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='UTF-8'>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; }}
                            .header {{ background: linear-gradient(135deg, #e79c00, #ff9900); padding: 20px; text-align: center; color: white; }}
                            .content {{ background: white; padding: 30px; margin-top: 20px; border-radius: 8px; }}
                            .field {{ margin-bottom: 15px; }}
                            .label {{ font-weight: bold; color: #555; }}
                            .footer {{ text-align: center; padding: 15px; color: #888; font-size: 12px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h2>🔔 New Contact Form Message</h2>
                            </div>
                            <div class='content'>
                                <div class='field'>
                                    <span class='label'>From:</span> {System.Net.WebUtility.HtmlEncode(model.Name)}
                                </div>
                                <div class='field'>
                                    <span class='label'>Email:</span> {System.Net.WebUtility.HtmlEncode(model.Email)}
                                </div>
                                <div class='field'>
                                    <span class='label'>Subject:</span> {System.Net.WebUtility.HtmlEncode(model.Subject)}
                                </div>
                                <hr/>
                                <div class='field'>
                                    <span class='label'>Message:</span>
                                    <p>{System.Net.WebUtility.HtmlEncode(model.Message).Replace("\n", "<br/>")}</p>
                                </div>
                            </div>
                            <div class='footer'>
                                &copy; 2025 Alpha Ayakkabı. All rights reserved.
                            </div>
                        </div>
                    </body>
                    </html>";
                }

                // --- Send admin email with metadata for Worker callback ---
                _logger.LogInformation($"Sending contact form to admin: {adminEmail}");
                
                try
                {
                    // Add metadata headers for Worker to use in callback
                    var customHeaders = new Dictionary<string, string>
                    {
                        { "X-User-Email", model.Email },
                        { "X-User-Culture", CultureInfo.CurrentCulture.Name },
                        { "X-User-Name", model.Name },
                        { "X-Webhook-Url", $"{Request.Scheme}://{Request.Host}/api/email/send-confirmation" }
                    };
                    
                    await _emailSender.SendEmailAsync(adminEmail, adminSubject, adminBody, customHeaders);
                    _logger.LogInformation("✅ Admin notification sent - Worker will validate and trigger user confirmation");
                    
                    // NOTE: User confirmation is now sent by Worker via webhook after spam validation
                    // This ensures users only get confirmation if the message passes spam checks
                    
                    TempData["SuccessMessage"] = _localization.GetKey("ContactSuccessMessage")
                                                ?? "Your message has been sent!";
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "❌ Failed to send email");
                    
                    // Show success to user for security (don't reveal email issues)
                    TempData["SuccessMessage"] = _localization.GetKey("ContactSuccessMessage")
                                                ?? "Your message has been sent!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Contact form validation error");
                TempData["ErrorMessage"] = _localization.GetKey("ContactErrorMessage")
                                        ?? "There was an error processing your message.";
            }

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

            // v2'de sadece success kontrolü yeterli
            bool success = result?.success == true;
            return success;
        }

        /// <summary>
        /// Verifies Cloudflare Turnstile token
        /// </summary>
        private async Task<bool> VerifyTurnstileAsync(string token, string userIp)
        {
            try
            {
                var secretKey = _configuration["Cloudflare:TurnstileSecretKey"];
                if (string.IsNullOrEmpty(secretKey))
                {
                    _logger.LogError("Cloudflare Turnstile secret key is not configured.");
                    return false;
                }

                using var client = new HttpClient();
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("secret", secretKey),
                    new KeyValuePair<string, string>("response", token),
                    new KeyValuePair<string, string>("remoteip", userIp)
                });

                var response = await client.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", content);
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonDocument.Parse(jsonResponse);

                if (result.RootElement.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                {
                    _logger.LogInformation("Turnstile verification successful for IP: {IP}", userIp);
                    return true;
                }

                _logger.LogWarning("Turnstile verification failed for IP: {IP}", userIp);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Turnstile verification exception for IP: {IP}", userIp);
                return false;
            }
        }




#endregion

#region  Services
    [HttpGet("{culture}/hizmetler")]
    public async Task<IActionResult> Services(string culture,string category, string brand, string search)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        ViewBag.Title = _localization.GetKey("SEO_Services_Title") 
                        ?? "Ürünlerimiz ve Hizmetlerimiz | Alpha Güvenlik Ayakkabıları";

        ViewBag.MetaDescription = _localization.GetKey("SEO_Services_Description") 
                        ?? "Alpha Ayakkabı iş güvenliği ayakkabıları, askeri botlar ve endüstriyel çözümleriyle hizmetinizde. Tüm ürünleri keşfedin.";

        ViewBag.MetaKeywords = _localization.GetKey("SEO_Services_Keywords") 
                        ?? "alpha ürünleri, iş güvenliği ayakkabısı, askeri bot, endüstriyel ayakkabı, alpha hizmetleri";

        // Retrieve all products and categories
        var allProducts = await _productRepository.GetAllAsync();
        var categories = await _categoryRepository.GetAllAsync();
        // 1) Retrieve localized strings from your resource service
        var servicesTitle = _localization.GetKey("Services_Title") ?? "Our Services";
        var servicesHeroDescription = _localization.GetKey("Services_HeroDescription") 
            ?? "Discover Alpha Safety Shoes' premium products and services...";
        var servicesCatalogFilterTitle = _localization.GetKey("Services_CatalogFilterTitle") 
            ?? "Catalog & Filters";
        var servicesViewCatalogButton = _localization.GetKey("Services_ViewCatalogButton") 
            ?? "View Our Catalog";
        var servicesFilterProductsTitle = _localization.GetKey("Services_FilterProductsTitle") 
            ?? "Filter Products";
        var servicesCategoryLabel = _localization.GetKey("Services_CategoryLabel") 
            ?? "Category";
        var servicesBrandLabel = _localization.GetKey("Services_BrandLabel") 
            ?? "Brand";
        var servicesBrandPlaceholder = _localization.GetKey("Services_BrandPlaceholder") 
            ?? "Enter brand name";
        var servicesSearchProductLabel = _localization.GetKey("Services_SearchProductLabel") 
            ?? "Search Product";
        var servicesApplyFiltersButton = _localization.GetKey("Services_ApplyFiltersButton") 
            ?? "Apply Filters";
        var servicesOurProductsTitle = _localization.GetKey("Services_OurProductsTitle") 
            ?? "Our Products";
        var servicesNoProductsMessage = _localization.GetKey("Services_NoProductsMessage") 
            ?? "No products found matching your criteria.";

        var Btn = _localization.GetKey("ViewButton") 
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





    [HttpGet("{culture}/urun/{id}/{slug?}")]
    public async Task<IActionResult> ProductDetail(string culture, int id, string? slug)
    {
        if (id <= 0)
            return BadRequest("Invalid product ID.");

        // 🌍 Culture ayarla
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        CultureInfo.CurrentCulture = new CultureInfo(culture);

        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
            return NotFound("Product not found.");

        // 🔤 Doğru slug mı? Değilse yönlendir.
        var expectedSlug = product.Url.ToLower();
        if (!string.Equals(slug, expectedSlug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("ProductDetail", new { culture = culture, id = product.ProductId, slug = expectedSlug });
        }

        // 🧠 SEO Meta Etiketleri
        ViewBag.Title = $"{product.Name} | Alpha Güvenlik Ayakkabıları";
        ViewBag.MetaDescription = !string.IsNullOrWhiteSpace(product.Description)
            ? (product.Description.Length > 160
                ? product.Description.Substring(0, 160) + "..."
                : product.Description)
            : "Alpha Ayakkabı'nın en güvenilir iş güvenliği ve askeri ayakkabı ürünlerini keşfedin.";
        ViewBag.MetaKeywords = $"alpha ayakkabı, {product.Name}, iş güvenliği ayakkabısı, askeri bot, endüstriyel ayakkabı";

        // 🔄 ViewBag çeviri etiketleri (aynen kalabilir)
        ViewBag.BrandLabel = _localization.GetKey("ProductDetail_BrandLabel") ?? "Brand";
        ViewBag.DescriptionLabel = _localization.GetKey("ProductDetail_DescriptionLabel") ?? "Description";
        ViewBag.InsoleLabel = _localization.GetKey("ProductDetail_InsoleLabel") ?? "Insole";
        ViewBag.UpperLabel = _localization.GetKey("ProductDetail_UpperLabel") ?? "Upper";
        ViewBag.ImagesLabel = _localization.GetKey("ProductDetail_ImagesLabel") ?? "Images";
        ViewBag.DateAddedLabel = _localization.GetKey("ProductDetail_DateAddedLabel") ?? "Date Added";
        ViewBag.CategoryLabel = _localization.GetKey("ProductDetail_CategoryLabel") ?? "Category";
        ViewBag.BodyNoLabel = _localization.GetKey("ProductDetail_BodyNoLabel") ?? "Body Number";
        ViewBag.BrandFieldLabel = _localization.GetKey("ProductDetail_BrandFieldLabel") ?? "Brand";
        ViewBag.ModelLabel = _localization.GetKey("ProductDetail_ModelLabel") ?? "Model";
        ViewBag.SoleLabel = _localization.GetKey("ProductDetail_SoleLabel") ?? "Sole";
        ViewBag.LiningLabel = _localization.GetKey("ProductDetail_LiningLabel") ?? "Lining";
        ViewBag.ProtectionLabel = _localization.GetKey("ProductDetail_ProtectionLabel") ?? "Protection";
        ViewBag.MidsoleLabel = _localization.GetKey("ProductDetail_MidsoleLabel") ?? "Midsole";
        ViewBag.SizeLabel = _localization.GetKey("ProductDetail_SizeLabel") ?? "Size";
        ViewBag.CertificateLabel = _localization.GetKey("ProductDetail_CertificateLabel") ?? "Certificate";
        ViewBag.StandardLabel = _localization.GetKey("ProductDetail_StandardLabel") ?? "Standard";
        ViewBag.Message = _localization.GetKey("ProductDetail_CategoryLabel") ?? "Categories";
        ViewBag.BTN = _localization.GetKey("ViewButton") ?? "Details";

        // 🌐 Dynamic content translations - Read directly from RESX files
        string? TryLocalize(string field)
        {
            var key = $"Product_{product.ProductId}_{field}";
            var translated = _resxService.Read(key, culture);
            Console.WriteLine($"[ProductDetail] Trying to translate '{key}' for culture '{culture}': '{translated}'");
            return !string.IsNullOrWhiteSpace(translated) ? translated : null;
        }

        product.Description = TryLocalize("Description") ?? product.Description;
        product.Upper = TryLocalize("Upper") ?? product.Upper;
        product.Insole = TryLocalize("Insole") ?? product.Insole;
        product.Lining = TryLocalize("Lining") ?? product.Lining;
        product.Protection = TryLocalize("Protection") ?? product.Protection;
        product.Midsole = TryLocalize("Midsole") ?? product.Midsole;
        product.Sole = TryLocalize("Sole") ?? product.Sole;

        var relatedBlogs = product.ProductBlogs?.Select(pb => pb.Blog).Where(b => b != null).ToList() ?? new();
        var relatedCategories = product.ProductCategories?.Select(pc => pc.Category).Where(c => c != null).ToList() ?? new();
        var recentProducts = await _productRepository.GetRecentProductsAsync() ?? new();

        // Helper function to clean up "Test" or empty values
        // Helper function to get localized default values
        string GetLocalizedDefault(string key, string fallback)
        {
            var localized = _localization.GetKey(key);
            return !string.IsNullOrWhiteSpace(localized) ? localized : fallback;
        }

        string CleanValue(string value, string defaultValue)
        {
            // If value is null, empty, or "Test", return the default
            if (string.IsNullOrWhiteSpace(value) || value.Trim().Equals("Test", StringComparison.OrdinalIgnoreCase))
                return defaultValue;
            return value;
        }

        // Get localized "Not specified" text once
        var notSpecified = GetLocalizedDefault("ProductDetail_NotSpecified", "Belirtilmemiş");
        var noDescription = GetLocalizedDefault("ProductDetail_NoDescription", "Açıklama mevcut değil");

        var viewModel = new ProductDetailViewModel
        {
            ProductId = product.ProductId,
            Name = product.Name,
            Description = CleanValue(product.Description, noDescription),
            Upper = CleanValue(product.Upper, notSpecified),
            Insole = CleanValue(product.Insole, notSpecified),
            Lining = CleanValue(product.Lining, notSpecified),
            Protection = CleanValue(product.Protection, notSpecified),
            Midsole = CleanValue(product.Midsole, notSpecified),
            Sole = CleanValue(product.Sole, notSpecified),
            Model = CleanValue(product.Model, notSpecified),
            Standard = CleanValue(product.Standard, notSpecified),
            Certificate = CleanValue(product.Certificate, notSpecified),
            Brand = CleanValue(product.Brand, "Alpha"),
            Size = CleanValue(product.Size, notSpecified),
            BodyNo = product.BodyNo,
            DateAdded = product.DateAdded,
            ProductImages = product.ProductImages?.ToList() ?? new(),
            RelatedBlogs = relatedBlogs,
            RelatedCategories = relatedCategories,
            RecentProducts = recentProducts,
            CategoryName = relatedCategories.FirstOrDefault()?.Name ?? "Unknown Category"
        };

        ViewBag.RelatedBlogs = relatedBlogs;
        ViewBag.RelatedCategories = relatedCategories;
        ViewBag.RecentProducts = recentProducts;

        return View("ProductDetail", viewModel);
    }




#endregion



#region Index
    [HttpGet("{culture}")]
    public async Task<IActionResult> Index(string culture)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        ViewBag.Title = _localization.GetKey("SEO_Index_Title") ?? "Alpha Ayakkabı - Güvenlikli İş Ayakkabıları";
        ViewBag.MetaDescription = _localization.GetKey("SEO_Index_Description") ?? "İş güvenliği için en kaliteli ayakkabılar. Alpha ile tanışın.";
        ViewBag.MetaKeywords = _localization.GetKey("SEO_Index_Keywords") ?? "iş güvenliği ayakkabısı, alpha bot, askeri bot";

        var heroTitle = _localization.GetKey("HeroTitle");
        var heroDescription = _localization.GetKey("HeroDescription");
        var heroLink = _localization.GetKey("HeroLink");
        var CountryQuote = _localization.GetKey("CountryQuote");
        var country1 = _localization.GetKey("country1");
        var country2 = _localization.GetKey("country2");
        var country3 = _localization.GetKey("country3");
        var country4 = _localization.GetKey("country4");  
        var country5 = _localization.GetKey("country5");  
        var country6 = _localization.GetKey("country6");  
        var country7 = _localization.GetKey("country7");  
        var country8 = _localization.GetKey("country8");          
        var country9 = _localization.GetKey("country9");  
        var country10 = _localization.GetKey("country10");  
        var country11 = _localization.GetKey("country11");  
        var country12 = _localization.GetKey("country12");    
        var Talker1 = _localization.GetKey("Talker1");  
        var Talker2 = _localization.GetKey("Talker2"); 
        var Talker3 = _localization.GetKey("Talker3");         
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
[HttpGet("{culture}/privacy")]
public IActionResult Privacy(string culture)
{
    CultureInfo.CurrentUICulture = new CultureInfo(culture);
    CultureInfo.CurrentCulture = new CultureInfo(culture);
    ViewBag.Title = _localization.GetKey("Privacy_SEOTitle") 
                ?? "Gizlilik Politikası | Alpha İş Güvenliği Ayakkabıları";
    ViewBag.MetaDescription = _localization.GetKey("Privacy_SEODescription") 
                    ?? "Alpha Ayakkabı’nın gizlilik politikası hakkında detaylı bilgilere buradan ulaşabilirsiniz.";
    ViewBag.MetaKeywords = _localization.GetKey("Privacy_SEOKeywords") 
                    ?? "gizlilik politikası, alpha güvenlik ayakkabıları, veri güvenliği";

    var model = new PrivacyViewModel
    {
        // Hero Section
        Title = _localization.GetKey("Privacy_Title"),
        HeroDescription = _localization.GetKey("Privacy_HeroDescription"),

        // Information We Collect
        InfoCollectionTitle = _localization.GetKey("Privacy_InfoCollectionTitle"),
        InfoCollectionPersonal = _localization.GetKey("Privacy_InfoCollectionPersonal"),
        InfoCollectionPayment = _localization.GetKey("Privacy_InfoCollectionPayment"),
        InfoCollectionUsage = _localization.GetKey("Privacy_InfoCollectionUsage"),
        InfoCollectionCookies = _localization.GetKey("Privacy_InfoCollectionCookies"),

        // How We Use Your Information
        UsageTitle = _localization.GetKey("Privacy_UsageTitle"),
        UsageIntro = _localization.GetKey("Privacy_UsageIntro"),
        UsagePurpose1 = _localization.GetKey("Privacy_UsagePurpose1"),
        UsagePurpose2 = _localization.GetKey("Privacy_UsagePurpose2"),
        UsagePurpose3 = _localization.GetKey("Privacy_UsagePurpose3"),

        // How We Protect Your Data
        ProtectionTitle = _localization.GetKey("Privacy_ProtectionTitle"),
        ProtectionIntro = _localization.GetKey("Privacy_ProtectionIntro"),
        ProtectionSSL = _localization.GetKey("Privacy_ProtectionSSL"),
        ProtectionFirewalls = _localization.GetKey("Privacy_ProtectionFirewalls"),
        ProtectionAccess = _localization.GetKey("Privacy_ProtectionAccess"),

        // Sharing Information
        SharingIntro =_localization.GetKey("Privacy_SharingIntro"),
        SharingTitle = _localization.GetKey("Privacy_SharingTitle"),
        SharingThirdParty = _localization.GetKey("Privacy_SharingThirdParty"),
        SharingLegal = _localization.GetKey("Privacy_SharingLegal"),

        // Your Rights
        RightsTitle = _localization.GetKey("Privacy_RightsTitle"),
        RightsAccess = _localization.GetKey("Privacy_RightsAccess"),
        RightsDelete = _localization.GetKey("Privacy_RightsDelete"),
        RightsOptOut = _localization.GetKey("Privacy_RightsOptOut"),

        // Contact Section
        ContactTitle = _localization.GetKey("Privacy_ContactTitle"),
        ContactQuestion = _localization.GetKey("Privacy_ContactQuestion"),
        ContactEmail = _localization.GetKey("Privacy_ContactEmail") ?? "support@alphaayakkabi.com",
        ContactPhone = _localization.GetKey("Privacy_ContactPhone"),
        ContactAddress = _localization.GetKey("Privacy_ContactAddress"),
        UseContactFormText = _localization.GetKey("Privacy_UseContactForm") ?? "Please use our contact form"
    };

    return View(model);
}
#endregion


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    /// <summary>
    /// Validates email address format
    /// </summary>
    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates that email is from a public provider (not temporary/bot email)
    /// </summary>
    private bool IsPublicEmailProvider(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        // Extract domain from email
        var parts = email.ToLower().Split('@');
        if (parts.Length != 2)
            return false;

        var domain = parts[1];

        // List of allowed public email providers
        var allowedProviders = new[]
        {
            "gmail.com", "googlemail.com",
            "yahoo.com", "yahoo.co.uk", "yahoo.fr", "yahoo.de",
            "outlook.com", "hotmail.com", "live.com", "msn.com",
            "icloud.com", "me.com", "mac.com",
            "protonmail.com", "proton.me", "pm.me",
            "aol.com",
            "zoho.com", "zoho.eu",
            "yandex.com", "yandex.ru",
            "mail.com", "email.com",
            "gmx.com", "gmx.de", "gmx.net",
            "mail.ru",
            "fastmail.com",
            "tutanota.com", "tuta.io"
        };

        return allowedProviders.Contains(domain);
    }
}

