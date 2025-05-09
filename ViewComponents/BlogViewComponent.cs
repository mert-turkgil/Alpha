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

            // Map to BlogIndexModel with translations
            var blogModels = pagedBlogs.Select(blog => new BlogIndexModel
            {
                Blogs = new List<Blog> { blog },
                Title = _blogResxService.Read($"Title_{blog.BlogId}_{blog.Url}_{culture}", culture) ?? blog.Title,
                Content = ProcessContentImagesForEdit(
                    _blogResxService.Read($"Content_{blog.BlogId}_{blog.Url}_{culture}", culture) ?? blog.Content
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

            var baseUrl = $"{Request.Scheme}://{Request.Host}/"; 
            return content.Replace("src=\"/", $"src=\"{baseUrl}");
        }

    }
}
