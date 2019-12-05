using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using umbcore = Umbraco.Core;
using Umbraco.Web.WebApi;
using System.Web.Http;
using Umbraco.Core.Scoping;
using NPoco;
using Umbraco.Core.Models.Membership;

namespace UmbracoAutomation.WebApi
{
    public class AutomationController : UmbracoApiController
    {
        private IScopeProvider _scopeProvider;

        public AutomationController(IScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

        [HttpPost]
        public object FetchMedia([FromBody] FetchMediaParams data)
        {
            IEnumerable<string> res;
            using (var scope = _scopeProvider.CreateScope())
            {
                res = scope.Database.Query<string>(new Sql("SELECT DISTINCT path FROM umbracoMediaVersion WHERE path LIKE '/media%' ORDER BY path"));
                scope.Complete();
                return CommonTasks.FetchMedia(res, data);
            }
        }

        [HttpPost]
        public IEnumerable<DomainSet> GetHostnames([FromBody] ExportHostnamesParams data) =>
            CommonTasks.GetHostnames(_scopeProvider, Services, data);

        [HttpPost]
        public string AddUser(AddUserParams data)
        {
            IUser user;
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

            using (var scope = _scopeProvider.CreateScope())
            {
                if (Services.UserService.GetUserGroupsByAlias(data.Group).FirstOrDefault() is IReadOnlyUserGroup userGroup)
                {
                    var deleteGroup = new Sql("DELETE FROM umbracoUser2UserGroup WHERE userId = " + user.Id + " AND userGroupId = " + userGroup.Id + ";");
                    scope.Database.Execute(deleteGroup);
                    var insertGroup = new Sql("INSERT INTO umbracoUser2UserGroup VALUES (" + user.Id + ", " + userGroup.Id + ")");
                    scope.Database.Execute(insertGroup); 
                }
                // Update history on Info tab
                var updateLogs = new Sql("UPDATE [umbracoLog] SET userId = " + user.Id + " WHERE userId NOT IN (SELECT id FROM umbracoUser);");
                scope.Database.Execute(updateLogs);
                // Update umbracoNode
                var updateNodes = new Sql("UPDATE umbracoNode SET nodeUser = " + user.Id + " WHERE nodeUser NOT IN (SELECT id FROM umbracoUser)");
                scope.Database.Execute(updateNodes);
                scope.Complete();
            }
            return "done";
        }

        [HttpPost]
        public object SetPropertyValue([FromBody] List<PropertyValueRule> rules) =>
            CommonTasks.SetPropertyValue(_scopeProvider, Services, rules);

        [HttpPost]
        public List<string> UpdateDomain([FromBody] List<DomainDirective> directives)
        {
            int? languageCode = null;
            using (var scope = _scopeProvider.CreateScope())
            {
                languageCode = scope.Database.ExecuteScalar<int?>(new Sql($"SELECT [id] FROM [umbracoLanguage] WHERE languageISOCode = 'en-GB'"));
                scope.Complete();
            }
            return CommonTasks.UpdateDomain(_scopeProvider, Services, directives, languageCode);
        }

        [HttpPost]
        public async Task<object> Upgrade([FromBody] UpgradeParams data) =>
            await CommonTasks.Upgrade(data, Request.RequestUri.Host.ToString());
    }
}
