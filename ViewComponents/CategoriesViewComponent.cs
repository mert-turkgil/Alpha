using Microsoft.AspNetCore.Mvc;
using Data.Abstract; // your repo interfaces
using Alpha.Services; // your localization, etc.
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Alpha.Models;

namespace Alpha.WebUi.ViewComponents
{
    public class CategoriesViewComponent : ViewComponent
    {
        private readonly ICategoryRepository _categoryService;
        private readonly LanguageService _localization;

        public CategoriesViewComponent(ICategoryRepository categoryService, LanguageService localization)
        {
            _categoryService = categoryService;
            _localization = localization;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // 1) Possibly read route data or query for selectedCategory, etc.
            if (RouteData.Values["category"] != null)
                ViewBag.SelectedCategory = RouteData.Values["category"];

            // 2) Localization examples
            ViewBag.Message = _localization.GetKey("HomeCategoryBody"); 
            ViewBag.Body = _localization.GetKey("HomeCategoryHead");

            // 3) Get and sort categories
            var categories = await _categoryService.GetAllAsync();
            var ordered = categories.OrderBy(c => c.Name).ToList();

            // 4) Build a small ViewModel
            var vm = new CategoryGridViewModel
            {
                Categories = ordered,
                TotalItems = ordered.Count
            };

            return View(vm);
        }
    }
}