using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence.Querying;
using Umbraco.Core.Scoping;
using Umbraco.Core.Services;

namespace UmbracoAutomation.WebApi
{
    public static class ContentHelper
    {
        public static IContentType GetContentType(ServiceContext sc, string alias)
        {
            return sc.ContentTypeService.GetAll().Where(x => x.Alias == alias).FirstOrDefault();
        }

        public static string GetPrevalueAsString(ServiceContext sc, int prevalue)
        {
            // to be implemented
            return "Test";
        }

        public static void ChangeDocumentType(IContent item, IContentType newType, bool clearProperties)
        {
            // https://github.com/umbraco/Umbraco-CMS/issues/5070
            // to be implemented
        }

        public static void SetTemplate(IContent item, ITemplate template)
        {
            item.TemplateId = template.Id;
        }

        public static int? GetDropdownValueId(ServiceContext sc, string definitionName, string value)
        {
            // to be implemented
            return null;
        }

        public static IContent GetParent(ServiceContext sc, IContent item)
        {
            return sc.ContentService.GetById(item.ParentId);
        }

        public static List<IContent> GetDescendantsOfType(IScopeProvider sp, ServiceContext sc, string docType, int parentId = 0)
        {
            // var sw = new Stopwatch();
            // sw.Start();
            var typeId = GetContentType(sc, docType).Id;
            using (var scope = sp.CreateScope())
            {
                var query = new Query<IContent>(scope.SqlContext)
                    .Where(w => w.Published && !w.Trashed && w.ContentTypeId == typeId);
                var nodes = parentId == 0 ?
                    sc.ContentService.GetPagedOfType(typeId, 0, 9999, out var total, query)
                        .Where(x => x.Published && x.ContentTypeId == typeId) :
                    sc.ContentService.GetPagedChildren(parentId, 0, 9999, out var total2, query);
                // sw.Stop();
                scope.Complete();
                // System.IO.File.AppendAllText(@"C:\Projects\DID_HYSW2\Tcne.MySites.Web\log.txt", $"\nGetDescedantsOfType {docType} - {sw.ElapsedMilliseconds}");
                return nodes.ToList();
            }
        }

        public static bool SaveAndPublish(ServiceContext sc, IContent document)
        {
            // var sw = new Stopwatch();
            // sw.Start();
            var result = sc.ContentService.SaveAndPublish(document);
            // sw.Stop();
            // System.IO.File.AppendAllText(@"C:\Projects\DID_HYSW2\Tcne.MySites.Web\log.txt", $"\nSaveAndPublish {document.Name} - {sw.ElapsedMilliseconds}");
            return result.Success;
        }
    }
}
