using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpha.Models;

namespace Alpha.Services
{
    public class FooterService : IFooterService
    {
        private readonly LanguageService _localization;

        public FooterService( LanguageService localization)
        {
            _localization = localization;
        }

        public FooterViewModel GetFooterViewModelAsync()
        {
            return new FooterViewModel
            {
                Home = _localization.GetKey("Home"),
                About = _localization.GetKey("About"),
                Services = _localization.GetKey("Services"),
                Blog = _localization.GetKey("Blog"),
                Privacy = _localization.GetKey("Privacy"),
                Contact = _localization.GetKey("Contact"),
                footerHeader = _localization.GetKey("FooterHead"),
                footerBody = _localization.GetKey("FooterBody"),
                QuickLinks = _localization.GetKey("QuickLink"),
                Follow = _localization.GetKey("Follow")
            };
        }
    }
}