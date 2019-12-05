using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Umbraco.Core.Models;
using Umbraco.Core.Scoping;
using Umbraco.Core.Services;
using System.Text.RegularExpressions;

namespace UmbracoAutomation.WebApi
{
    public static class MySitestHelper
    {
        public static List<IContent> GetSiteRootNodes(IScopeProvider sp, ServiceContext sc, int parentId = 0)
            => ContentHelper.GetDescendantsOfType(sp, sc, "siteRoot", parentId);

        public static void ConvertBookingCategoryToType(ServiceContext sc, IContent activity)
        {
            var activityType = ContentHelper.GetContentType(sc, "scheduleActivity");
            if (!activityType.PropertyTypes.Any(p => p.Alias == "type"))
                return;
            var adult = "[\r\n  {\r\n    \"key\": \"Adult\",\r\n    \"label\": \"Adult / Fitness\"\r\n  }\r\n]";
            var child = "[{\"key\":\"Child\",\"label\":\"Child\"}]";
            var ministars = "[{\"key\":\"MiniStars\",\"label\":\"Mini Stars\"}]";
            var type = activity.GetValue<string>("type");
            if (string.IsNullOrEmpty(type))
            {
                var bookingCategory = ContentHelper.GetPrevalueAsString(sc, activity.GetValue<int>("bookingCategory"));
                var bookingSubCategory = ContentHelper.GetPrevalueAsString(sc, activity.GetValue<int>("bookingSubCategory"));
                var newType = adult;
                var newTypeName = "adult";

                if (bookingCategory == "Child" & string.IsNullOrEmpty(bookingSubCategory))
                {
                    newType = child;
                    newTypeName = "child";
                }

                if (bookingCategory == "Child" & !string.IsNullOrEmpty(bookingSubCategory))
                {
                    newType = ministars;
                    newTypeName = "ministars";

                }

                activity.SetValue("type", newType);
                Console.WriteLine($"Activity '{activity.Name}' ({activity.Id}) - Converting booking category/sub-category {bookingCategory}{bookingSubCategory} to type:{newTypeName}");
                ContentHelper.SaveAndPublish(sc, activity);
            }
        }

        public static List<IContent> GetConceptRootNodes(IScopeProvider sp, ServiceContext sc, int parentId = 0)
            => ContentHelper.GetDescendantsOfType(sp, sc, "conceptRoot", parentId);

        public static List<IContent> GetActivities(IScopeProvider sp, ServiceContext sc, int parentId = 0)
            => ContentHelper.GetDescendantsOfType(sp, sc, "scheduleActivity", parentId);

        public static List<IContent> GetPages(IScopeProvider sp, ServiceContext sc, int parentId = 0)
            => ContentHelper.GetDescendantsOfType(sp, sc, "page", parentId);

        public static void SetRecurrencyStartDate(ServiceContext sc, IContent activity)
        {
            var recurrency = ContentHelper.GetPrevalueAsString(sc, activity.GetValue<int>("recurrent"));
            if (!string.IsNullOrEmpty(recurrency) & activity.GetValue<DateTime?>("startDate") == null)
            {
                var weekday = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), sc.ContentService.GetById(activity.ParentId).Name, true);
                DateTime today = DateTime.Today;
                int daysUntil = ((int)weekday - (int)today.DayOfWeek + 7) % 7;
                DateTime next = today.AddDays(daysUntil);
                activity.SetValue("startDate", next);
                Console.WriteLine($"Activity '{activity.Name}' ({activity.Id}) - Updating startDate:{next.ToString()}");
                ContentHelper.SaveAndPublish(sc, activity);
            }
        }

        public static int GetMainHolidayPlannerId(IContent siteRoot) => siteRoot.GetValue<int>("schedulePageId");

        public static void ConvertToNewHolidayPlanner(ServiceContext sc, IContent siteRoot)
        {
            var newHolidayPlannerType = ContentHelper.GetContentType(sc, "holidayPlanner");
            var hpId = GetMainHolidayPlannerId(siteRoot);
            if (hpId == 0) return;
            var holidayPlanner = sc.ContentService.GetById(hpId);
            var oldHolidayPlannerType = ContentHelper.GetContentType(sc, "schedulePage");
            if (holidayPlanner.ContentTypeId == oldHolidayPlannerType.Id)
            {
                Console.WriteLine($"Site '{siteRoot.Name}' ({siteRoot.Id}) - Updating HP {holidayPlanner.Name} ({holidayPlanner.Id}) to new doc type");
                var image = holidayPlanner.GetValue<int>("navigationImage");
                ContentHelper.ChangeDocumentType(holidayPlanner, newHolidayPlannerType, true);
                if (image != 0)
                {
                    holidayPlanner.SetValue("mainImage", image);
                }
                holidayPlanner.SetValue("umbracoNaviHide", false); // for breadcrumbs to work
                ContentHelper.SaveAndPublish(sc, holidayPlanner);
            }
        }

        public static void DisableCaptcha(ServiceContext sc, IContent siteRoot)
        {
            var captcha = siteRoot.GetValue<bool>("enableCaptcha");
            if (captcha)
            {
                siteRoot.SetValue("enableCaptcha", false);
                ContentHelper.SaveAndPublish(sc, siteRoot);
            }
        }

        public static void ConvertSunwingMasterPageToPage(IScopeProvider sp, ServiceContext sc, IContent siteRoot)
        {
            var page = ContentHelper.GetContentType(sc, "page");
            var oldDocType = ContentHelper.GetContentType(sc, "SunwingMasterPage");
            var nodes = ContentHelper.GetDescendantsOfType(sp, sc, oldDocType.Alias, siteRoot.Id);
            var template = sc.FileService.GetTemplates().Where(t => t.Alias == "Page").FirstOrDefault();
            nodes.ForEach(n =>
            {
                ContentHelper.ChangeDocumentType(n, page, false);
                ContentHelper.SetTemplate(n, template);
                ContentHelper.SaveAndPublish(sc, n);
            });
        }

        public static void SetPageTemplate(ServiceContext sc, IContent page)
        {
            Console.WriteLine($"Page '{page.Name}' ({page.Id}) - Setting page template");
            var template = sc.FileService.GetTemplates().Where(t => t.Alias == "Page").FirstOrDefault();
            ContentHelper.SetTemplate(page, template);
            ContentHelper.SaveAndPublish(sc, page);
        }

        public static void TurnOffNoBox(ServiceContext sc, IContent page)
        {
            Console.WriteLine($"Page '{page.Name}' ({page.Id}) - Disabling NoBox");
            var noBox = page.GetValue<bool>("noBox");
            if (noBox)
            {
                page.SetValue("noBox", false);
                ContentHelper.SaveAndPublish(sc, page);
            }
        }

        public static void UpdateConceptBranding(ServiceContext sc, IContent conceptRoot)
        {
            var conceptName = conceptRoot.Name;
            if (string.IsNullOrEmpty(conceptRoot.GetValue<string>("conceptDisplayName")))
            {
                Console.WriteLine($"Concept '{conceptRoot.Name}' ({conceptRoot.Id}) - Setting conceptDisplayName:{conceptName}");
                conceptRoot.SetValue("conceptDisplayName", conceptName);
                ContentHelper.SaveAndPublish(sc, conceptRoot);
            }

            if (conceptRoot.GetValue<int>("conceptTheme") == 0)
            {
                var value = ContentHelper.GetDropdownValueId(sc, "Concept Root - Concept theme", conceptName);
                if (value != null)
                {
                    // Console.WriteLine($"Concept '{conceptRoot.Name}' ({conceptRoot.Id}) - Setting conceptTheme:{dropdownItem.Value.Value}");
                    conceptRoot.SetValue("conceptTheme", value);
                }
                ContentHelper.SaveAndPublish(sc, conceptRoot);
            }
        }

        public static void UpdateSiteBranding(ServiceContext sc, IContent siteRoot)
        {
            var conceptName = ContentHelper.GetParent(sc, siteRoot).Name;
            var siteName = siteRoot.GetValue<string>("siteName");
            if (siteName == null)
                return;
            var newSiteName = Regex.Replace(siteName, conceptName, "", RegexOptions.IgnoreCase).Trim();
            newSiteName = Regex.Replace(newSiteName, "^[\\s-]+", "", RegexOptions.IgnoreCase).ToString();
            if (siteName != newSiteName)
            {
                Console.WriteLine($"Site '{siteRoot.Name}' ({siteRoot.Id}) - Setting siteName:{newSiteName}");
                siteRoot.SetValue("siteName", newSiteName);
                ContentHelper.SaveAndPublish(sc, siteRoot);
            }
        }
    }
}
