using System.Collections.Generic;

namespace UmbracoAutomation.WebApi
{
    public class DomainSet
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public List<string> Domains { get; set; }
    }
}
