using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Umbraco.Core.Models;
using Umbraco.Core.Scoping;
using Umbraco.Core.Services;

namespace UmbracoAutomation.WebApi
{
    public static class CommonTasks
    {
        public static object FetchMedia(IEnumerable<string> mediaFiles, FetchMediaParams data)
        {
            var sourceUrl = new Uri(data.SourceUrl);
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.DefaultConnectionLimit = 9999;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            var errors = new List<string>();
            var downloaded = new List<string>();

            Parallel.ForEach(mediaFiles, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
            {
                if (!string.IsNullOrEmpty(i))
                {
                    using (var client = new WebClient())
                    {
                        var uriPath = new System.Uri((data.OutputDir + i).Replace("/", "\\"));
                        var filePath = uriPath.LocalPath;
                        if (!System.IO.File.Exists(filePath))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                            var url = (sourceUrl + i);
                            try
                            {
                                client.DownloadFile(url, filePath);
                                downloaded.Add(url); ;
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"{url} - {ex.Message}");
                            }
                        }
                    }
                }
            });
            dynamic result = new System.Dynamic.ExpandoObject();
            result.Downloaded = downloaded;
            result.Errors = errors;
            return result;
        }

        public static IEnumerable<DomainSet> GetHostnames(IScopeProvider sp, ServiceContext sc, ExportHostnamesParams data)
        {
            var selectors = data.Selector.Split(',').Select(s => s.Trim()).ToList();
            return MySitestHelper.GetSiteRootNodes(sp, sc, 0)
                .Where(n => data.Selector == "*"
                    ? true
                    : data.By == SiteRootBy.Id ? selectors.Contains(n.Id.ToString()) : selectors.Contains(n.Name)
                ).ToList()
                .Select(s => {
                    return new DomainSet
                    {
                        SiteId = s.Id,
                        SiteName = s.Name,
                        Domains = sc.DomainService.GetAssignedDomains(s.Id, false)
                           .Where(d => d.DomainName.Contains(data.Suffix)).Select(d => d.DomainName.ToString()).ToList()
                    };
                }).Where(d => d.Domains.Count > 0).OrderBy(d => d.SiteName);
        }

        public static object SetPropertyValue(IScopeProvider sp, ServiceContext sc, List<PropertyValueRule> rules)
        {
            var items = new List<IContent>();
            dynamic response = new System.Dynamic.ExpandoObject();
            response.ruleResults = new List<dynamic>();
            response.publishResults = new System.Dynamic.ExpandoObject();
            response.publishResults.success = new List<string>(); ;
            response.publishResults.fail = new List<string>();

            foreach (var r in rules)
            {
                dynamic ruleResult = new System.Dynamic.ExpandoObject();
                ruleResult.rule = r;
                var appliedTo = new List<string>();
                var nodes = r.By == UmbracoDocumentBy.Id
                    ? sc.ContentService.GetByIds(r.GetSelectors<int>()).ToList()
                    : ContentHelper.GetDescendantsOfType(sp, sc, r.GetSelectors()[0], 0).ToList();
                var values = r.GetValues();
                for (var i = 0; i < nodes.Count; i++)
                {
                    var val = (nodes.Count > 1 && nodes.Count == values.Count) ? values[i] : r.Value;
                    nodes[i].SetValue(r.Name, val);
                    appliedTo.Add($"({nodes[i].ContentType.Alias}){nodes[i].Id}-{nodes[i].Name}");
                    if (!items.Any(x => x.Id == nodes[i].Id))
                    {
                        items.Add(nodes[i]);
                    }
                }
                ruleResult.appliedTo = appliedTo;
                response.ruleResults.Add(ruleResult);
            };

            Parallel.ForEach(items, item =>
            {
                try
                {
                    var message = $"({item.ContentType.Alias}){item.Id}-{item.Name}";
                    if (!ContentHelper.SaveAndPublish(sc, item))
                    {
                        response.publishResults.fail.Add(message);
                    }
                    else
                    {
                        response.publishResults.success.Add(message);
                    }
                }
                catch (Exception ex)
                {
                    response.publishResults.fail.Add($"({item.ContentType.Alias}){item.Id} - {ex.Message}");
                }
            });
            return response;
        }

        public static List<string> UpdateDomain(IScopeProvider sp, ServiceContext sc, [FromBody] List<DomainDirective> directives, int? lanuageCode)
        {
            var excludeDomainRegex = "\\.local$|\\.int$|\\.dmz$";
            var appliedTo = new List<string>();
            foreach (var directive in directives)
            {
                dynamic directiveResult = new System.Dynamic.ExpandoObject();
                directiveResult.directive = directive;
                var selectors = directive.Selector.Split(',').Select(s => s.Trim()).ToList();
                MySitestHelper.GetSiteRootNodes(sp, sc, 0)
                    .Where(n => directive.Selector == "*"
                        ? true
                        : directive.By == SiteRootBy.Id ? selectors.Contains(n.Id.ToString()) : selectors.Contains(n.Name)
                    ).ToList()
                    .ForEach(s =>
                    {
                        if (directive.Action == DomainAction.Set)
                        {
                            foreach (var host in directive.Value.Split(',').Select(x => x.Trim()))
                            {
                                if (!string.IsNullOrEmpty(host))
                                {
                                    var existingDomain = sc.DomainService.GetByName(host);
                                    var domain = existingDomain ?? new UmbracoDomain(host);
                                    if (existingDomain != null)
                                    {
                                        if (domain.RootContentId != null)
                                        {
                                            var referencedNode = sc.ContentService.GetById((int)existingDomain.RootContentId);
                                            appliedTo.Add($"del ({referencedNode.ContentType.Alias}){referencedNode.Id}-{referencedNode.Name}:{host}");
                                        }
                                    }
                                    domain.RootContentId = s.Id;
                                    if (domain.LanguageId == null)
                                    {
                                        domain.LanguageId = lanuageCode;
                                    }
                                    appliedTo.Add($"set ({s.ContentType.Alias}){s.Id}-{s.Name}:{host}");
                                    sc.DomainService.Save(domain);
                                }
                            }
                        }
                        else
                        {
                            sc.DomainService.GetAssignedDomains(s.Id, false)
                            .Where(d => !Regex.IsMatch(d.DomainName, excludeDomainRegex)).ToList().ForEach(i => {
                                var newName = i.DomainName.TrimEnd('/').Replace(".", "_") + "." + directive.Value;
                                if (sc.DomainService.Exists(newName))
                                {
                                    var existing = sc.DomainService.GetByName(newName);
                                    sc.DomainService.Delete(existing);
                                    appliedTo.Add($"del ({s.ContentType.Alias}){s.Id}-{s.Name}:{i.DomainName.TrimEnd('/')}");
                                }
                                appliedTo.Add($"update ({s.ContentType.Alias}){s.Id}-{s.Name}:{i.DomainName.TrimEnd('/')}=>{newName}");
                                i.DomainName = newName;
                                sc.DomainService.Save(i);
                            });
                        }
                    });
            }
            return appliedTo;
        }

        public static async Task<object> Upgrade([FromBody] UpgradeParams data, string host)
        {
            var client = new HttpClient();
            string response;
            try
            {
                var resp = await client.GetAsync($"http://{host}/install/api/GetSetup");
                response = await resp.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                response = null;
            }

            if (!string.IsNullOrEmpty(response))
            {
                var responseObj = System.Web.Helpers.Json.Decode(response.Substring(response.IndexOf("{")));
                if (responseObj.Message == null)
                {
                    var installId = (string)responseObj.installId;
                    var isUpgrade = (string)responseObj.steps[0].name == "Upgrade";
                    if (isUpgrade)
                    {
                        var model = responseObj.steps[0].model;
                        dynamic authData = new System.Dynamic.ExpandoObject();
                        authData.username = data.User;
                        authData.password = data.Password;
                        var authResponse = string.Empty;
                        try
                        {
                            var authContent = new StringContent(JsonConvert.SerializeObject(authData), Encoding.UTF8, "application/json");
                            var authResp = await client.PostAsync($"http://{host}/umbraco/backoffice/UmbracoApi/Authentication/PostLogin", authContent);
                            authResponse = await authResp.Content.ReadAsStringAsync();
                        }
                        catch (Exception ex)
                        {
                            authResponse = ex.Message;
                        }

                        if (!string.IsNullOrEmpty(authResponse))
                        {
                            var cont = true;
                            var answers = new List<string>();
                            var attempts = 0;
                            do
                            {
                                try
                                {
                                    dynamic installData = new System.Dynamic.ExpandoObject();
                                    dynamic installDataInstructions = new System.Dynamic.ExpandoObject();
                                    installDataInstructions.Upgrade = model;
                                    installData.installId = installId;
                                    installData.instructions = installDataInstructions;
                                    var installContent = new StringContent(JsonConvert.SerializeObject(installData), Encoding.UTF8, "application/json");
                                    var installResp = await client.PostAsync($"http://{host}/install/api/PostPerformInstall", installContent);
                                    var installResponse = await installResp.Content.ReadAsStringAsync();
                                    var installResponseObj = System.Web.Helpers.Json.Decode(installResponse.Substring(installResponse.IndexOf("{")));

                                    var progressMessage = (bool)installResponseObj.complete
                                        ? "Upgrade complete!"
                                        : $"{(string)installResponseObj.stepCompleted} - Done. Starting {(string)installResponseObj.nextStep}...";

                                    if (!answers.Contains(progressMessage))
                                    {
                                        answers.Add(progressMessage);
                                    }

                                    cont = !(bool)installResponseObj.complete;
                                    Thread.Sleep(1000);
                                }
                                catch (Exception ex)
                                {
                                    answers.Add(ex.Message + ex.StackTrace);
                                }
                                attempts++;
                            }
                            while (cont == true && attempts < 30);
                            return answers;
                        }
                        return ("Authentication failed." + authResponse);
                    }
                }
            }
            return "No upgrade needed";
        }

    }
}
