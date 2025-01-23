using Alpha.Entity; // Blog entity
using Alpha.Services;
using Alpha.Models; // BlogIndexModel
using Data.Abstract; // Blog repository
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Alpha.ViewComponents
{
    public class BlogViewComponent : ViewComponent
    {
        private readonly IBlogRepository _blogRepository;
        private readonly LanguageService _localization;

        public BlogViewComponent(IBlogRepository blogRepository, LanguageService localization)
        {
            _blogRepository = blogRepository;
            _localization = localization;
        }

        public async Task<IViewComponentResult> InvokeAsync(int pageIndex = 0, int pageSize = 3)
        {
            // Get current culture
            var culture = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();

            // Header translation
            var head = _localization.GetKey("HomeBlogHead").Value;
            string message = _localization.GetKey("Recently").Value;

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
                message = _localization.GetKey("NoRecentBlogs").Value;
            }

            // Map to BlogIndexModel with translations
            var blogModels = pagedBlogs.Select(blog => new BlogIndexModel
            {
                Blogs = pagedBlogs,

                // Fetch translations dynamically
                Title = _localization.GetKey($"Title_{blog.BlogId}_{blog.Url}_{culture}").Value ?? blog.Title,
                Content = ProcessContentImagesForEdit(
                    _localization.GetKey($"Content_{blog.BlogId}_{blog.Url}_{culture}")?.Value ?? blog.Content
                )
            }).ToList();

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

            // Replace relative paths with absolute URLs
            return content.Replace("src=\"/", "src=\"https://example.com/");
        }
    }
}
