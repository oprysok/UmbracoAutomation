using ManyConsole;
using System;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Persistence;

namespace UmbracoAutomation.CLI.Commands
{
    public class AddUserCommand : ConsoleCommand
    {
        private const int Success = 0;
        private const int Failure = 2;
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Group { get; set; }

        public AddUserCommand()
        {
            // Register the actual command with a simple (optional) description.
            IsCommand("AddUser", "Adds a new Umbraco user. If target user already exist then password is updated.");

            // Required options/flags, append '=' to obtain the required value.
            HasRequiredOption("n|name=", "User name.", p => Name = p.Trim());
            HasRequiredOption("e|email=", "Email.", p => Email = p.Trim());
            HasRequiredOption("p|password=", "Password.", p => Password = p.Trim());
            HasOption("g|group=", "Umbraco user group alias.", p => Group = p.Trim());

            // EXAMPLE: AddUser -n=test -e=test@example.com -p=Password1
        }

        public override int Run(string[] remainingArguments)
        {
            try
            {
                var bootManager = new CoreBootManager(new UmbracoApplication());
                bootManager.Initialize();
                
                bootManager.Complete(ctx =>
                {
                    var db = ApplicationContext.Current.DatabaseContext.Database;
                    var userId = 0;
                    var existingUser = ctx.Services.UserService.GetByUsername(Name);
                    
                    if (existingUser != null)
                    {
                        userId = existingUser.Id;
                        ctx.Services.UserService.SavePassword(existingUser, Password);
                    } else
                    {
                        var newUser = ctx.Services.UserService.CreateUserWithIdentity(Name, Email);
                        userId = newUser.Id;
                        ctx.Services.UserService.SavePassword(newUser, Password);
                    }

                    if (ctx.Services.UserService.GetUserGroupsByAlias(Group).FirstOrDefault() is IReadOnlyUserGroup userGroup)
                    {
                        var deleteGroup = new Sql("DELETE FROM umbracoUser2UserGroup WHERE userId = " + userId + " AND userGroupId = " + userGroup.Id + ";");
                        db.Execute(deleteGroup);
                        var insertGroup = new Sql("INSERT INTO umbracoUser2UserGroup VALUES (" + userId + ", " + userGroup.Id + ")");
                        db.Execute(insertGroup);
                    }

                    // Update cmsDocument
                    var updateCmsDocument = new Sql("UPDATE cmsDocument SET documentUser = " + userId + " where documentUser NOT IN (SELECT id FROM umbracoUser);");
                    db.Execute(updateCmsDocument);
                    // Update history on Info tab
                    var updateLogs = new Sql("UPDATE [umbracoLog] SET userId = " + userId + " WHERE userId NOT IN (SELECT id FROM umbracoUser);");
                    db.Execute(updateLogs);
                    // Update umbracoNode
                    var updateNodes = new Sql("UPDATE umbracoNode SET nodeUser = " + userId + " WHERE nodeUser NOT IN (SELECT id FROM umbracoUser)");
                    db.Execute(updateNodes);

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
