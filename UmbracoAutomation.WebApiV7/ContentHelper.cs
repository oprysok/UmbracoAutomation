using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models;
using Umbraco.Core.Scoping;
using Umbraco.Core.Services;

namespace UmbracoAutomation.WebApi
{
    public static class ContentHelper
    {
        public static IContentType GetContentType(ServiceContext sc, string alias)
        {
            return sc.ContentTypeService.GetContentType(alias);
        }

        public static string GetPrevalueAsString(ServiceContext sc, int prevalue)
        {
            return sc.DataTypeService.GetPreValueAsString(prevalue);
        }

        public static void ChangeDocumentType(IContent item, IContentType newType, bool clearProperties)
        {
            item.ChangeContentType(newType, clearProperties);
        }

        public static void SetTemplate(IContent item, ITemplate template)
        {
            item.Template = template;
        }

        public static IContent GetParent(ServiceContext sc, IContent item)
        {
            return item.Parent();
        }

        public static List<IContent> GetDescendantsOfType(IScopeProvider sp, ServiceContext sc, string docType, int parentId = 0)
        {
            var type = GetContentType(sc, docType);
            return (parentId == 0 ?
                sc.ContentService.GetContentOfContentType(type.Id).Where(n => n.HasPublishedVersion & n.Status != ContentStatus.Expired) :
                sc.ContentService.GetDescendants(parentId).Where(n => n.ContentTypeId == type.Id & n.HasPublishedVersion & n.Status != ContentStatus.Expired)).ToList();
        }
        public static bool SaveAndPublish(ServiceContext sc, IContent document)
        {
            var result = sc.ContentService.SaveAndPublishWithStatus(document);
            return result.Success;
        }

        public static int? GetDropdownValueId(ServiceContext sc, string definitionName, string value)
        {
            var datatype = sc.DataTypeService.GetDataTypeDefinitionByName(definitionName);
            var dropdown = sc.DataTypeService.GetPreValuesCollectionByDataTypeId(datatype.Id);

            var dropdownItem = dropdown.PreValuesAsDictionary
                .ToList()
                .Where(x => x.Value.Value.Equals(value, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
            if (dropdownItem.Value != null)
            {
                return dropdownItem.Value.Id;
            }
            return null;
        }
    }
}
