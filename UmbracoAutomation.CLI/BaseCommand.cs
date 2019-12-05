using System;
using ManyConsole;
using Umbraco.Core;

namespace UmbracoAutomation.CLI
{
    public abstract class BaseCommand : ConsoleCommand
    {
        private const int Success = 0;
        private const int Failure = 2;
        public virtual string DescriptionLong { get; }
        protected abstract string Description { get; }

        public abstract void BootManagerAction(ApplicationContext ctx);

        public BaseCommand()
        {
            // Register the actual command with a simple (optional) 4description.
            IsCommand(GetType().Name.TrimEnd("Command"), Description);
            if (!string.IsNullOrEmpty(DescriptionLong))
            {
                // Add a longer description for the help on that specific command.
                HasLongDescription(DescriptionLong);
            }
        }

        public override int Run(string[] remainingArguments)
        {
            try
            {
                var bootManager = new CoreBootManager(new UmbracoApplication());
                bootManager.Initialize();
                bootManager.Complete(ctx =>
                {
                    BootManagerAction(ctx);
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
