using Alpha.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Globalization;
using Alpha.Models;
using Data.Abstract;

namespace Alpha.ViewComponents
{
    public class CarouselViewComponent : ViewComponent
    {
        private readonly ICarouselRepository _carouselService;
        private readonly LanguageService _localization;
        private readonly IActionDescriptorCollectionProvider _actionDescriptorProvider;

        public CarouselViewComponent(
            ICarouselRepository carouselService,
            LanguageService localization,
            IActionDescriptorCollectionProvider actionDescriptorProvider)
        {
            _carouselService = carouselService;
            _localization = localization;
            _actionDescriptorProvider = actionDescriptorProvider;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var carousels = await _carouselService.GetAllAsync();
            var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            var carouselViewModel = new CarouselResourceViewModel
            {
                CarouselItems = carousels.Select(c =>
                {
                    var action = string.IsNullOrWhiteSpace(c.CarouselLink) ? "Services" : c.CarouselLink;
                    var finalAction = ActionExists(action, "Home") ? action : "Services";

                    var link = Url.Action(finalAction, "Home", new { culture });

                    return new CarouselItemViewModel
                    {
                        CarouselId = c.CarouselId,
                        CarouselImage = c.CarouselImage,
                        CarouselImage600w = c.CarouselImage600w,
                        CarouselImage1200w = c.CarouselImage1200w,
                        CarouselTitle = _localization.GetKey($"Carousel_{c.CarouselId}_Title") ?? c.CarouselTitle,
                        CarouselDescription = _localization.GetKey($"Carousel_{c.CarouselId}_Description") ?? c.CarouselDescription,
                        CarouselLink = link,
                        CarouselLinkText = _localization.GetKey($"Carousel_{c.CarouselId}_LinkText") ?? c.CarouselLinkText
                    };
                }).ToList()
            };

            return View(carouselViewModel);
        }

        private bool ActionExists(string actionName, string controllerName)
        {
            return _actionDescriptorProvider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Any(descriptor =>
                    string.Equals(descriptor.ControllerName, controllerName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(descriptor.ActionName, actionName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
