using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Abstract;
using Alpha.Models;

namespace Alpha.Services
{
    public class NavbarService : INavbarService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly LanguageService _localization;

        public NavbarService(ICategoryRepository categoryRepository, LanguageService localization)
        {
            _categoryRepository = categoryRepository;
            _localization = localization;
        }

        public async Task<NavbarViewModel> GetNavbarViewModelAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return new NavbarViewModel
            {
                Home = _localization.GetKey("Home").Value,
                About = _localization.GetKey("About").Value,
                Services = _localization.GetKey("Services").Value,
                Blog = _localization.GetKey("Blog").Value,
                Privacy = _localization.GetKey("Privacy").Value,
                Contact = _localization.GetKey("Contact").Value,
                NH1 = _localization.GetKey("NH1").Value,
                NH2 = _localization.GetKey("NH2").Value,
                NH3 = _localization.GetKey("NH3").Value,
                Categories = categories
            };
        }
    }
}