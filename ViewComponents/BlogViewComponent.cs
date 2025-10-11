using Alpha.Entity; // Blog entity
using Alpha.Services;
using Alpha.Models; // BlogIndexModel
using Data.Abstract; // Blog repository
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;

namespace Alpha.ViewComponents
{
    public class BlogViewComponent : ViewComponent
    {
        private readonly IBlogRepository _blogRepository;
        private readonly LanguageService _localization;
        private readonly IBlogResxService _blogResxService;

        public BlogViewComponent(IBlogRepository blogRepository, LanguageService localization,IBlogResxService blogResxService)
        {
            _blogRepository = blogRepository;
            _blogResxService = blogResxService;
            _localization = localization;
        }

        public async Task<IViewComponentResult> InvokeAsync(int pageIndex = 0, int pageSize = 3)
        {
            var culture = CultureInfo.CurrentCulture.Name;
            var uiCulture = CultureInfo.CurrentUICulture.Name;
            
            // Debug: Log current culture
            Console.WriteLine($"[BlogViewComponent] Current Culture: {culture}, UI Culture: {uiCulture}");

            // Header translation
            var head = _localization.GetKey("HomeBlogHead");
            string message = _localization.GetKey("Recently");

            // Fetch all blogs
            List<Blog> allBlogs = await _blogRepository.GetAllAsync();

            // Sort by date descending
            var sortedBlogs = allBlogs.OrderByDescending(b => b.Date).ToList();

            // Paginate blogs in groups of three
            List<Blog> pagedBlogs = sortedBlogs
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToList();

            // Fallback to all blogs if no recent ones exist
            if (!pagedBlogs.Any())
            {
                pagedBlogs = sortedBlogs;
                message = _localization.GetKey("NoRecentBlogs");
            }

            // Map to BlogIndexModel with translations for all cultures
            // NOTE: Keys do NOT have language suffix - the culture is determined by which .resx file we read from
            var blogModels = pagedBlogs.Select(blog => new BlogIndexModel
            {
                Blogs = new List<Blog> { blog },
                
                // English (US) - Key without language suffix
                TitleUS = _blogResxService.Read($"Title_{blog.BlogId}_{blog.Url}", "en-US") ?? blog.Title,
                ContentUS = ProcessContentImagesForEdit(
                    _blogResxService.Read($"Content_{blog.BlogId}_{blog.Url}", "en-US") ?? blog.Content
                ),
                
                // Turkish (TR) - Key without language suffix
                TitleTR = _blogResxService.Read($"Title_{blog.BlogId}_{blog.Url}", "tr-TR") ?? blog.Title,
                ContentTR = ProcessContentImagesForEdit(
                    _blogResxService.Read($"Content_{blog.BlogId}_{blog.Url}", "tr-TR") ?? blog.Content
                ),
                
                // German (DE) - Key without language suffix
                TitleDE = _blogResxService.Read($"Title_{blog.BlogId}_{blog.Url}", "de-DE") ?? blog.Title,
                ContentDE = ProcessContentImagesForEdit(
                    _blogResxService.Read($"Content_{blog.BlogId}_{blog.Url}", "de-DE") ?? blog.Content
                ),
                
                // French (FR) - Key without language suffix
                TitleFR = _blogResxService.Read($"Title_{blog.BlogId}_{blog.Url}", "fr-FR") ?? blog.Title,
                ContentFR = ProcessContentImagesForEdit(
                    _blogResxService.Read($"Content_{blog.BlogId}_{blog.Url}", "fr-FR") ?? blog.Content
                ),
                
                // Arabic (SA) - Key without language suffix
                TitleAR = _blogResxService.Read($"Title_{blog.BlogId}_{blog.Url}", "ar-SA") ?? blog.Title,
                ContentAR = ProcessContentImagesForEdit(
                    _blogResxService.Read($"Content_{blog.BlogId}_{blog.Url}", "ar-SA") ?? blog.Content
                ),
                
                // Fallback
                Title = blog.Title,
                Content = ProcessContentImagesForEdit(blog.Content)
            }).ToList();
            
            // Debug: Log what was loaded
            foreach (var model in blogModels)
            {
                var blog = model.Blogs.FirstOrDefault();
                if (blog != null)
                {
                    Console.WriteLine($"[BlogViewComponent] Blog {blog.BlogId} loaded:");
                    Console.WriteLine($"  TitleUS: {model.TitleUS?.Substring(0, Math.Min(30, model.TitleUS?.Length ?? 0))}");
                    Console.WriteLine($"  TitleTR: {model.TitleTR?.Substring(0, Math.Min(30, model.TitleTR?.Length ?? 0))}");
                }
            }


            // Pass data to view
            ViewBag.Head = head;
            ViewBag.Message = message;

            return View(blogModels); // Return the model list
        }

        /// <summary>
        /// Process images in content by fixing relative paths.
        /// </summary>
        private string ProcessContentImagesForEdit(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            var baseUrl = $"{Request.Scheme}://{Request.Host}/"; 
            return content.Replace("src=\"/", $"src=\"{baseUrl}");
        }

    }
}
