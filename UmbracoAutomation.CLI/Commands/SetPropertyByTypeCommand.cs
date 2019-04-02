using ManyConsole;
using System;
using System.Linq;
using Umbraco.Core;

namespace UmbracoAutomation.CLI.Commands
{
    public class SetPropertyByTypeCommand : ConsoleCommand
    {
        private const int Success = 0;
        private const int Failure = 2;
        public string Type { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public SetPropertyByTypeCommand()
        {
            // Register the actual command with a simple (optional) description.
            IsCommand("SetPropertyByType", "Set a value for a node(s) property by its doctype alias.");

            // Add a longer description for the help on that specific command.
            HasLongDescription("This can be used to update site settings by a doctype alias.");

            // Required options/flags, append '=' to obtain the required value.
            HasRequiredOption("t|type=", "The type (doctype alias) of the node(s) to be updated.", p => Type = p.Trim());

            // Required options/flags, append '=' to obtain the required value.
            HasRequiredOption("n|name=", "The name of the property.", p => Name = p);

            // Required options/flags, append '=' to obtain the required value.
            HasRequiredOption("v|value=", "The value of the property.", p => Value = p);
        }
        public override int Run(string[] remainingArguments)
        {
            try
            {
                var bootManager = new CoreBootManager(new UmbracoApplication());
                bootManager.Initialize();
                bootManager.Complete(ctx =>
                {
                    var res = ctx.Services.ContentTypeService.GetContentType(Type);
                    ctx.Services.ContentService.GetContentOfContentType(res.Id)
                        .Where(n => n.HasPublishedVersion)
                        .ToList()
                        .ForEach(m =>
                        {
                            m.SetValue(Name, Value);
                            var result = ctx.Services.ContentService.SaveAndPublishWithStatus(m);
                            if (!result.Success)
                            {
                                throw new Exception("Failed to update property.");
                            }
                        });
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
