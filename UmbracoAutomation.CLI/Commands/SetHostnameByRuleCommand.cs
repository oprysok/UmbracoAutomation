using ManyConsole;
using System;
using System.Linq;
using Umbraco.Core;

namespace UmbracoAutomation.CLI.Commands
{
    public class SetHostnameByRuleCommand : ConsoleCommand
    {
        private const int Success = 0;
        private const int Failure = 2;
        public string Rules { get; set; }

        public SetHostnameByRuleCommand()
        {
            // Register the actual command with a simple (optional) description.
            IsCommand("SetHostnameByRule", "Updates/sets hostname for a node by its name using special rules format.");

            // Add a longer description for the help on that specific command.
            HasLongDescription("The general syntax of a single hostname rule is as follows: \n\tNodeName => Hostname[,Hostname]" +
                "\nTo enter multiple rules, each rule should be separated by a semicolon character (;).");

            // Required options/flags, append '=' to obtain the required value.
            HasRequiredOption("r|rule=", "Hostname(s) to node assignement rule(s).", p => Rules = p.Trim());

            // EXAMPLE: SetHostnameByRule -r="MyAwesomeSite => myawesomesite.local"
        }

        public override int Run(string[] remainingArguments)
        {
            try
            {
                var bootManager = new CoreBootManager(new UmbracoApplication());
                bootManager.Initialize();
                bootManager.Complete(ctx =>
                {
                    var rls = Rules.Split(';').ToList();
                    var contentType = ctx.Services.ContentTypeService.GetContentType("umbHomePage");
                    var siteNodes = ctx.Services.ContentService.GetContentOfContentType(contentType.Id);

                    foreach (var r in rls)
                    {
                        var nodeAndHostArray = r.Trim().Split(new string[] { "=>" }, StringSplitOptions.None);
                        if (nodeAndHostArray.Length > 1 && !string.IsNullOrEmpty(nodeAndHostArray[1]))
                        {
                            var nodeName = nodeAndHostArray[0].Trim();
                            var targetNode = siteNodes.FirstOrDefault(s => s.Name == nodeName);
                            if (targetNode != null)
                            {
                                nodeAndHostArray[1].Split(',').ToList().ForEach(h =>
                                {
                                    var host = h.Trim();
                                    if (!string.IsNullOrEmpty(host))
                                    {
                                        var configuredDomain = ctx.Services.DomainService.GetByName(host) ?? new Umbraco.Core.Models.UmbracoDomain(host);
                                        configuredDomain.LanguageId = configuredDomain.LanguageId ?? 2;
                                        configuredDomain.RootContentId = targetNode.Id;
                                        ctx.Services.DomainService.Save(configuredDomain);
                                    }
                                });
                            }
                        }
                    }
                });
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
