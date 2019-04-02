﻿using ManyConsole;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Umbraco.Core;

namespace UmbracoAutomation.CLI.Commands
{

    public class RevertHostnamesCommand : ConsoleCommand
    {
        private const int Success = 0;
        private const int Failure = 2;
        public string Suffix { get; set; }

        public RevertHostnamesCommand()
        {
            // Register the actual command with a simple (optional) description.
            IsCommand("RevertHostnames", "Reverts hostname updates made by UpdateHostnames command.");

            // Add a longer description for the help on that specific command.
            HasLongDescription("Example: example_com.app.dev.int > example.com");

            // Required options/flags, append '=' to obtain the required value.
            HasRequiredOption("s|suffix=", "The expected suffix (ending) of the hostnames after update. E.g.: app.dev.int", p => Suffix = p.Trim());
            // EXAMPLE: RevertHostnames -s=mysites2.dev.int
        }
        public override int Run(string[] remainingArguments)
        {
            try
            {
                var bootManager = new CoreBootManager(new UmbracoApplication());
                bootManager.Initialize();
                bootManager.Complete(ctx =>
                {
                    ctx.Services.DomainService.GetAll(false)
                    .Where(d => Regex.IsMatch(d.DomainName, "_([a-zA-Z])+." + Regex.Escape(Suffix) + "$"))
                    .ToList()
                    .ForEach(i =>
                    {
                        i.DomainName = i.DomainName.TrimEnd("." + Suffix).Replace("_", ".");
                        ctx.Services.DomainService.Save(i);
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