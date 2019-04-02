using ManyConsole;
using System;
using Umbraco.Core;

namespace UmbracoAutomation.CLI.Commands
{
    public class SetPropertyByIdCommand : ConsoleCommand
    {
        private const int Success = 0;
        private const int Failure = 2;
        public int Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public SetPropertyByIdCommand()
        {
            // Register the actual command with a simple (optional) 4description.
            IsCommand("SetPropertyById", "Set a value of doctype property by a node id.");

            // Add a longer description for the help on that specific command.
            HasLongDescription("This can be used to update site settings by a node id.");

            // Required options/flags, append '=' to obtain the required value.
            HasRequiredOption("i|id=", "The id of the node to be updated.", p => Id = int.Parse(p));

            // Required options/flags, append '=' to obtain the required value.
            HasRequiredOption("n|name=", "The name of the property.", p => Name = p);

            // Required options/flags, append '=' to obtain the required value.
            HasRequiredOption("v|value=", "The value of the property.", p => Value = p);
        }
        public override int Run(string[] remainingArguments) {
            try
            {
                var bootManager = new CoreBootManager(new UmbracoApplication());
                bootManager.Initialize();
                bootManager.Complete(ctx =>
                {
                    var conceptRoot = ctx.Services.ContentService.GetById(Id);
                    conceptRoot.SetValue(Name, Value);
                    var result = ctx.Services.ContentService.SaveAndPublishWithStatus(conceptRoot);
                    if (!result.Success)
                    {
                        throw new Exception("Failed to update property.");
                    }
                });

                Console.Out.WriteLine("Updated");
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
