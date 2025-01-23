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
                Home = _localization.GetKey("Home").Value,
                About = _localization.GetKey("About").Value,
                Services = _localization.GetKey("Services").Value,
                Blog = _localization.GetKey("Blog").Value,
                Privacy = _localization.GetKey("Privacy").Value,
                Contact = _localization.GetKey("Contact").Value,
                footerHeader = _localization.GetKey("FooterHead").Value,
                footerBody = _localization.GetKey("FooterBody").Value,
                QuickLinks = _localization.GetKey("QuickLink").Value,
                Follow = _localization.GetKey("Follow").Value
            };
        }
    }
}