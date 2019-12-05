using System.Collections.Generic;
using System.Linq;
using umbcore = Umbraco.Core;
using Umbraco.Web.WebApi;
using System.Web.Http;
using Umbraco.Core.Persistence;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Models;
using System.Threading.Tasks;
using System;

namespace UmbracoAutomation.WebApi
{
    public class AutomationController : UmbracoApiController
    {

        [HttpPost]
        public object FetchMedia([FromBody] FetchMediaParams data)
        {
            var db = umbcore.ApplicationContext.Current.DatabaseContext.Database;
            var res = db.Query<string>(new Sql("select mediaPath from cmsMedia"));
            return CommonTasks.FetchMedia(res, data);
        }

        [HttpPost]
        public IEnumerable<DomainSet> GetHostnames([FromBody] ExportHostnamesParams data) =>
            CommonTasks.GetHostnames(null, Services, data);

        [HttpPost]
        public string AddUser(AddUserParams data)
        {
            IUser user;
            var db = umbcore.ApplicationContext.Current.DatabaseContext.Database;

            var existingUser = Services.UserService.GetByUsername(data.Name);
            if (existingUser != null)
            {
                Umbraco.MembershipHelper.ChangePassword(data.Name, new Umbraco.Web.Models.ChangingPasswordModel
                {
                    OldPassword = existingUser.RawPasswordValue,
                    NewPassword = data.Password
                }, "UsersMembershipProvider");
                user = existingUser;
            }
            else
            {
                var newuser = Services.UserService.CreateUserWithIdentity(data.Name, data.Email);
                newuser.RawPasswordValue = "temppass";
                Services.UserService.Save(newuser);

                Umbraco.MembershipHelper.ChangePassword(data.Name, new Umbraco.Web.Models.ChangingPasswordModel
                {
                    OldPassword = "temppass",
                    NewPassword = data.Password
                }, "UsersMembershipProvider");
                user = newuser;
            }

                if (Services.UserService.GetUserGroupsByAlias(data.Group).FirstOrDefault() is IReadOnlyUserGroup userGroup)
                {
                    var deleteGroup = new Sql("DELETE FROM umbracoUser2UserGroup WHERE userId = " + user.Id + " AND userGroupId = " + userGroup.Id + ";");
                    db.Execute(deleteGroup);
                    var insertGroup = new Sql("INSERT INTO umbracoUser2UserGroup VALUES (" + user.Id + ", " + userGroup.Id + ")");
                    db.Execute(insertGroup);
                }
                // Update cmsDocument
                var updateCmsDocument = new Sql("UPDATE cmsDocument SET documentUser = " + user.Id + " where documentUser NOT IN (SELECT id FROM umbracoUser);");
                db.Execute(updateCmsDocument);
                // Update history on Info tab
                var updateLogs = new Sql("UPDATE [umbracoLog] SET userId = " + user.Id + " WHERE userId NOT IN (SELECT id FROM umbracoUser);");
                db.Execute(updateLogs);
                // Update umbracoNode
                var updateNodes = new Sql("UPDATE umbracoNode SET nodeUser = " + user.Id + " WHERE nodeUser NOT IN (SELECT id FROM umbracoUser)");
                db.Execute(updateNodes);
            return "done";
        }

        [HttpPost]
        public object SetPropertyValue([FromBody] List<PropertyValueRule> rules) =>
            CommonTasks.SetPropertyValue(null, Services, rules);

        [HttpPost]
        public List<string> UpdateDomain([FromBody] List<DomainDirective> directives) {
            var db = umbcore.ApplicationContext.Current.DatabaseContext.Database;
            var res = db.ExecuteScalar<int?>(new Sql($"SELECT [id] FROM [umbracoLanguage] WHERE languageISOCode = 'en-GB'"));
            return CommonTasks.UpdateDomain(null, Services, directives, res);
        }

        [HttpPost]
        public async Task<object> Upgrade([FromBody] UpgradeParams data) =>
            await CommonTasks.Upgrade(data, Request.RequestUri.Host.ToString());
    }
}
