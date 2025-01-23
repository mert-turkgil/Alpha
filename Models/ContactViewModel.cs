using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Alpha.Models
{
    public class ContactViewModel
    {
        #nullable disable
        // Localized text properties for "Contact" page
        public string Contact_Title { get; set; }
        public string Contact_HeroDescription { get; set; }

        // Contact Info Cards
        public string Contact_OurAddressValue { get; set; }
        public string Contact_PhoneNumberValue { get; set; }
        public string Contact_EmailAddressValue { get; set; }
        public string Contact_OurAddress { get; set; }
        public string Contact_PhoneNumber { get; set; }
        public string Contact_EmailUs { get; set; }

        // Form Section
        public string Contact_SendUsMessage { get; set; }
        public string Contact_YourName { get; set; }
        public string Contact_YourEmail { get; set; }
        public string Contact_Subject { get; set; }
        public string Contact_Message { get; set; }
        public string Contact_SendMessageButton { get; set; }

        // Map + Social
        public string Contact_OurLocation { get; set; }
        public string Contact_FollowUsSocialMedia { get; set; }
    }
}
