using System;
using System.Collections.Generic;
using System.Text;

namespace UmbracoAutomation.WebApi
{
    public class DomainDirective
    {
        public SiteRootBy By { get; set; }
        public string Selector { get; set; }
        public DomainAction Action { get; set; }
        public string Value { get; set; }
    }
}
