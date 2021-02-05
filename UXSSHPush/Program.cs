using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Reflection;

namespace UXSSHPush
{
    class CommandlineOptions
    {
        public bool Verbose { get; set; }
        public bool CreateEmptyConfig { get; set; }
        public bool ShowHelp { get; set; }
        public string ConfigurationFile { get; set; }
    }
    class Program
    {
        static CommandlineOptions ParseCommandlineArguments(string[] args)
        {
            CommandlineOptions Options = new CommandlineOptions();
            foreach (string arg in args)
            {
                if (arg.Contains("--help"))
                {
                    Options.ShowHelp = true;
                }
                else if (arg.Contains("--verbose"))
                {
                    Options.Verbose = true;
                }
                else if (arg.Contains("--create"))
                {
                    Options.CreateEmptyConfig = true;
                }
                else if (arg.Contains("--config"))
                {
                    Options.ConfigurationFile = arg.Substring(9);
                }
                else
                {
                    throw new ArgumentException("Unknown argument: " + arg);
                }
            }
            return Options;
        }

        static void ShowHelp()
        {
            Console.WriteLine("Usage: ");
            Console.WriteLine("Show this help:");
            Console.WriteLine("  --help");
            Console.WriteLine("Be verbose during processing:");
            Console.WriteLine("  --verbose");
            Console.WriteLine("Using specified configfile for processing:");
            Console.WriteLine(@"  --config=""C:\MyRepo\MyConfig.json""");
            Console.WriteLine("Create Empty Configuration:");
            Console.WriteLine("  --create");
            Console.WriteLine("Sample: ");
            Console.WriteLine(@"UXSSHPush --config=""C:\MyRepo\MyConfig.json"" --create");
            Console.WriteLine("");
            Console.WriteLine("Notice:");
            Console.WriteLine("Be sure not commiting configfiles due containing credentials");
            Console.WriteLine("RemoteDir must exist.");
        }
        static bool CreateSample(CommandlineOptions Options)
        {
            Console.WriteLine("Creating empty config file....");
            SSHOptions sSHOptions = new SSHOptions
            {
                Hostname = "my.host.sample",
                Port = 22,
                Localpath = @"C:\MyRepro\bin\publish\",
                Remotepath = @"/home/user/sample",
                Username = "MyUsername",
                Password = "MySecret",
                PrivateKeyFile = null,
                PreCommand = new List<string> { "precommand1", "precommand2" },
                PostCommand = new List<string> { "postcommand1", "postcommand2" },
                Excludefiles = new List<string> { "appsettings.json", ".txt" }
            };

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Options.ConfigurationFile));
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(Options.ConfigurationFile))
                {
                    file.Write(JsonConvert.SerializeObject(sSHOptions, Formatting.Indented));
                }
                Console.WriteLine("Configuration written: " + Options.ConfigurationFile);
            }
            catch
            {
                Console.WriteLine("Error writing config file!");
                return false;
            }
            return true;
        }

        static int Main(string[] args)
        {
            var versionString = Assembly.GetEntryAssembly()
                                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                .InformationalVersion
                                .ToString();
            Console.WriteLine($"Starting application v{versionString}");
            CommandlineOptions Options = ParseCommandlineArguments(args);
            if (Options.ShowHelp)
            {
                ShowHelp();
                return 0;
            }
            else
            {
                if (Options.ConfigurationFile != null)
                {
                    if (Options.CreateEmptyConfig)
                    {
                        if (CreateSample(Options))
                        {
                            return 0;
                        }
                        else
                        {
                            return 1;
                        }
                    }
                    else
                    {
                        SSHOptions sSHOptions;
                        // Reading Configfile
                        Console.WriteLine("Reading Configuration");
                        try
                        {
                            using (System.IO.StreamReader file = new System.IO.StreamReader(Options.ConfigurationFile))
                            {
                                sSHOptions = JsonConvert.DeserializeObject<SSHOptions>(file.ReadToEnd());
                            }
                            if (Options.Verbose)
                            {
                                Console.WriteLine("Using Configuration:");
                                Console.WriteLine("File:           " + Options.ConfigurationFile);
                                Console.WriteLine("Hostname:       " + sSHOptions.Hostname);
                                Console.WriteLine("Port:           " + sSHOptions.Port);
                                Console.WriteLine("Username:       " + sSHOptions.Username);
                                Console.WriteLine("Password:       " + sSHOptions.Password);
                                Console.WriteLine("PrivateKeyFile: " + sSHOptions.PrivateKeyFile);
                                Console.WriteLine("Remotepath:     " + sSHOptions.Remotepath);
                                Console.WriteLine("Localpath:      " + sSHOptions.Localpath);
                                Console.WriteLine("Excluded file pattern:");
                                foreach (string excluded in sSHOptions.Excludefiles)
                                {
                                    Console.WriteLine("  " + excluded);
                                }
                                Console.WriteLine("Pre Commands:");
                                foreach (string pre in sSHOptions.PreCommand)
                                {
                                    Console.WriteLine("  " + pre);
                                }
                                Console.WriteLine("Post Commands:");
                                foreach (string post in sSHOptions.PostCommand)
                                {
                                    Console.WriteLine("  " + post);
                                }
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Error reading config file!");
                            return 1;
                        }
                        try
                        {
                            using (SSHTool sshtool = new SSHTool(sSHOptions, Options.Verbose))
                            {
                                // Processing Configfile
                                sshtool.PreCommands();
                                sshtool.ProcessFiles();
                                sshtool.PostCommands();
                            }
                            Console.WriteLine("Process Finished");
                            return 0;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error during upload. Check your config");
                            Console.WriteLine("Errordetails:" + e.Message);
                            return 1;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Misssing configurationfile");
                    return 1;
                }
            }
        }
    }
}
