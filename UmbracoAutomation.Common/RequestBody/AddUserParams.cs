using System;
using System.Collections.Generic;
using System.Text;

namespace UmbracoAutomation.WebApi
{
    public class AddUserParams
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Group { get; set; }
    }
}
