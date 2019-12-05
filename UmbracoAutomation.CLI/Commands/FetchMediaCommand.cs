using ManyConsole;
using System;
using Umbraco.Core;
using Umbraco.Core.Persistence;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace UmbracoAutomation.CLI.Commands
{
    public class FetchMediaCommand : ConsoleCommand
    {
        private const int Success = 0;
        private const int Failure = 2;
        public string OutputDir { get; set; }
        public string SourceUrl { get; set; }

        public FetchMediaCommand()
        {
            // Register the actual command with a simple (optional) description.
            IsCommand("FetchMedia", "Downloads missing media files from source URL.");

            // Required options/flags, append '=' to obtain the required value.
            HasRequiredOption("o|output=", "Output directory. Umbraco app root directory.", p => OutputDir = p.Trim());
            HasRequiredOption("s|source=", "Source Umbraco URL.", p => SourceUrl = p.Trim());

            // EXAMPLE: FetchMedia -n=test -e=test@example.com -p=Password1
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
                    var sourceUrl = new Uri(SourceUrl);
                    var getMediaSql = new Sql("select mediaPath from cmsMedia");
                    var res = db.Query<string>(getMediaSql);

                    if (!Directory.Exists(OutputDir))
                    {
                        throw new Exception("Output directory doesn't exist " + OutputDir);
                    }

                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.DefaultConnectionLimit = 9999;
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

                    var errors = new List<string>();

                    Parallel.ForEach(res, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
                    {
                        if (!string.IsNullOrEmpty(i))
                        {
                            using (var client = new WebClient())
                            {
                                var filePath = (OutputDir + i).Replace("/", "\\");
                                if (!File.Exists(filePath))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                                    var url = (sourceUrl + i);
                                    try
                                    {
                                        client.DownloadFile(url, filePath);
                                        Console.Out.WriteLine("Downloaded: " + url);
                                    }
                                    catch (Exception ex)
                                    {
                                        errors.Add($"Failed - {url} : {ex.Message}");
                                    }
                                }
                            }
                        }
                    });
                    errors.ForEach(Console.Out.WriteLine);
                    Console.Out.WriteLine("Done");
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
