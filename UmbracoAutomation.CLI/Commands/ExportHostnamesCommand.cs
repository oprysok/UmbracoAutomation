using ManyConsole;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Umbraco.Core;

namespace UmbracoAutomation.CLI.Commands
{
    class DomainSet
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public List<string> Domains { get; set; }
    }
    public class ExportHostnamesCommand : ConsoleCommand
    {
        private const int Success = 0;
        private const int Failure = 2;
        private string SiteRootDoctypeAlias = Properties.Settings.Default.SiteRootDocType;
        private string Output { get; set; }
        public string Suffix { get; set; }
        public string Admin { get; set; }
        public List<int> SiteId { get; set; }

        public ExportHostnamesCommand()
        {
            // Register the actual command with a simple (optional) 4description.
            IsCommand("ExportHostnames", "Export URLs to HTML.");

            // Add a longer description for the help on that specific command.
            HasLongDescription("This can be used to update Umbraco hostnames during deployment process to dev environent. \n " +
                "Example: example.com > example_com.app.dev.int");

            // Required options/flags, append '=' to obtain the required value.
            HasRequiredOption("s|suffix=", "The expected suffix (ending) of the hostnames after update. E.g.: app.dev.int", p => Suffix = p.Trim());

            HasRequiredOption("o|out=", "The name of file", p => Output = p.Trim());

            HasRequiredOption("a|admin=", "The admin server hostname", p => Admin = p.Trim());

            HasOption("i|id=", "The id of the siteRoot.", p => {
                try {
                    SiteId = p.Split(',').Select(s => int.Parse(s.Trim())).ToList();
                } catch (FormatException)
                {
                    SiteId = new List<int>() { };
                }
            });

            // EXAMPLE: UpdateHostnames -s=test
        }
        public override int Run(string[] remainingArguments)
        {
            try
            {
                var bootManager = new CoreBootManager(new UmbracoApplication());
                var msb = new StringBuilder();
                var sb = new StringBuilder();
                msb.Append($"<html><body>" +
                    "<style>a { color: #359cbb;} td { padding: 10px; border-bottom: 1px solid #777;}</style>" +
                    $"<h1>Backoffice URL</h1><a href=\"http://{Admin}/umbraco\" target=\"_blank\">{Admin}</a></br>" +
                    "<h1>URLs</h1><table><tr><th>Name</th><th>URLs</th><th>Prod URLs</th></tr>\n");
                bootManager.Initialize();
                bootManager.Complete(ctx =>
                {
                    var contentType = ctx.Services.ContentTypeService.GetContentType(SiteRootDoctypeAlias);
                    var sites = ctx.Services.ContentService.GetContentOfContentType(contentType.Id)
                        .Where((n) => {
                            var isPublished = n.HasPublishedVersion;
                            var notTrashed = n.Status != Umbraco.Core.Models.ContentStatus.Trashed;
                            if (SiteId != null && SiteId.Count > 0)
                            {
                                return isPublished && notTrashed && SiteId.Contains(n.Id);
                            }
                            return isPublished && notTrashed;
                        });
                    var domainSets = sites.Select(s => {
                            return new DomainSet {
                                SiteId = s.Id,
                                SiteName = s.Name,
                                Domains = ctx.Services.DomainService.GetAssignedDomains(s.Id, false)
                                   .Where(d => d.DomainName.Contains(Suffix)).Select(d => d.DomainName.ToString()).ToList()
                            };
                        }).OrderBy(d => d.SiteName);
                    foreach (var i in domainSets)
                    {
                        var urls = string.Join("", i.Domains.Select(d => $"<a target=\"_blank\" href=\"http://{d}\">{d}</a></br>").ToList());
                        var prodUrls = string.Join("", i.Domains.Select(d => $"<a target=\"_blank\" href=\"http://{d.Replace($".{Suffix}", "").Replace("_", ".")}\">{d.Replace($".{Suffix}", "").Replace("_", ".")}</a></br>").ToList());
                        var edit = $"<a target=\"_blank\" href=\"http://{Admin}/umbraco/#content/content/edit/{i.SiteId}\">{i.SiteId}</a>";
                        sb.Append($"<tr><td>{i.SiteName} ({edit})</td><td>{urls}</td><td>{prodUrls}</td></tr>\n");
                    }
                });
                msb.Append(sb.ToString());
                msb.Append("</table></body></html>");
                File.WriteAllText(Output, msb.ToString());
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
