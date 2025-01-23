using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Alpha.Models
{
    public class PrivacyViewModel
    {
        #nullable disable
        // Hero Section
        public string Title { get; set; }
        public string HeroDescription { get; set; }

        // Sections
        public string InfoCollectionTitle { get; set; }
        public string InfoCollectionPersonal { get; set; }
        public string InfoCollectionPayment { get; set; }
        public string InfoCollectionUsage { get; set; }
        public string InfoCollectionCookies { get; set; }

        public string UsageTitle { get; set; }
        public string UsagePurpose1 { get; set; }
        public string UsagePurpose2 { get; set; }
        public string UsagePurpose3 { get; set; }

        public string ProtectionTitle { get; set; }
        public string ProtectionSSL { get; set; }
        public string ProtectionFirewalls { get; set; }
        public string ProtectionAccess { get; set; }

        public string SharingTitle { get; set; }
        public string SharingThirdParty { get; set; }
        public string SharingLegal { get; set; }

        public string RightsTitle { get; set; }
        public string RightsAccess { get; set; }
        public string RightsDelete { get; set; }
        public string RightsOptOut { get; set; }

        public string ContactTitle { get; set; }
        public string ContactQuestion { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string ContactAddress { get; set; }
        public string ProtectionIntro { get; internal set; }
        public string SharingIntro { get; internal set; }
        public string UsageIntro { get; internal set; }
    }

}