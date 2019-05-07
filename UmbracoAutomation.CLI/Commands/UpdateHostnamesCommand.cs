using ManyConsole;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Umbraco.Core;

namespace UmbracoAutomation.CLI.Commands
{
    public class UpdateHostnamesCommand : ConsoleCommand
    {
        private const int Success = 0;
        private const int Failure = 2;
        private string SiteRootDoctypeAlias = Properties.Settings.Default.SiteRootDocType;
        private string ExcludeDomainRegex; 
        public string Suffix { get; set; }
        public string Exclude { get => ExcludeDomainRegex; set => ExcludeDomainRegex = value; }

        public UpdateHostnamesCommand()
        {
            ExcludeDomainRegex = "\\.local$|\\.int$|\\.dmz$";

            // Register the actual command with a simple (optional) 4description.
            IsCommand("UpdateHostnames", "Update hostname for each site root node.");

            // Add a longer description for the help on that specific command.
            HasLongDescription("This can be used to update Umbraco hostnames during deployment process to dev environent. \n " +
                "Example: example.com > example_com.app.dev.int");

            // Required options/flags, append '=' to obtain the required value.
            HasRequiredOption("s|suffix=", "The expected suffix (ending) of the hostnames after update. E.g.: app.dev.int", p => Suffix = p.Trim());

            HasOption("e|exclude=", "Regex pattern to skip updates for matched domains. Default pattern '\\.local$|\\.int$|\\.dmz$'.", p => Exclude = p.Trim());

            // EXAMPLE: UpdateHostnames -s=test
        }
        public override int Run(string[] remainingArguments)
        {
            try
            {
                var bootManager = new CoreBootManager(new UmbracoApplication());
                bootManager.Initialize();
                bootManager.Complete(ctx =>
                {
                    var res = ctx.Services.ContentTypeService.GetContentType(SiteRootDoctypeAlias);
                    ctx.Services.ContentService.GetContentOfContentType(res.Id)
                        .Where(n => n.HasPublishedVersion)
                        .ToList()
                        .ForEach(m =>
                        {
                            ctx.Services.DomainService.GetAssignedDomains(m.Id, false)
                            .Where(d => !Regex.IsMatch(d.DomainName, Exclude)).ToList().ForEach(i => {
                                var newName = i.DomainName.TrimEnd("/").Replace(".", "_") + "." + Suffix;
                                if (ctx.Services.DomainService.Exists(newName))
                                {
                                    Console.WriteLine($"Target domain {newName} already exists. Deleting.");
                                    var existing = ctx.Services.DomainService.GetByName(newName);
                                    ctx.Services.DomainService.Delete(existing);
                                }
                                i.DomainName = newName;
                                ctx.Services.DomainService.Save(i);
                            });
                        });
                });

                Console.Out.WriteLine("Updated");
                return Success;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);

                return Failure;
            }
        }
    }
}
