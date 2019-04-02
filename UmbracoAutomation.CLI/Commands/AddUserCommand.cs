using ManyConsole;
using System;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Persistence;
using Umbraco.Web.Security.Providers;

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
                    var mbp = new UsersMembershipProvider();
                    var existingUser = ctx.Services.UserService.GetByUsername(Name);
                    if (existingUser != null)
                    {
                        ctx.Services.UserService.Delete(existingUser, true);
                    }

                    var newUser = ctx.Services.UserService.CreateUserWithIdentity(Name, Email);
                    var userGroup = ctx.Services.UserService.GetUserGroupsByAlias(Group).FirstOrDefault() as IReadOnlyUserGroup;
                    newUser.RawPasswordValue = mbp.HashPasswordForStorage(Password);
                    ctx.Services.UserService.SavePassword(newUser, Password);
                        
                    if (userGroup != null)
                    {
                        var db = ApplicationContext.Current.DatabaseContext.Database;
                        var insert = new Sql("INSERT INTO umbracoUser2UserGroup VALUES (" + newUser.Id + ", " + userGroup.Id + ")");
                        var res = db.Execute(insert);
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
