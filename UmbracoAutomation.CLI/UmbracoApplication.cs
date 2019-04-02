using Umbraco.Core;

namespace UmbracoAutomation.CLI
{
    public class UmbracoApplication : UmbracoApplicationBase
    {
        protected override IBootManager GetBootManager()
        {
            return new CoreBootManager(this);
        }
    }
}
