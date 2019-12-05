using ManyConsole;
using System;
using System.Collections.Generic;
using System.Linq;
namespace UmbracoAutomation.CLI
{
	public class Program
	{
		public static int Main(string[] args)
		{
            var commands = GetCommands();
            return ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
		}

        public static IEnumerable<ConsoleCommand> GetCommands()
        {
            return ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof(Program)).ToList().Where(c =>
            {
                return c.GetType().Namespace.StartsWith("UmbracoAutomation.CLI.");
            });
        }
    }
}