/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using P3Net.Kraken;
using Plossum.CommandLine;

using TfsMigrate.CommandLine;
using TfsMigrate.Diagnostics;
using TfsMigrate.Hosting;
using TfsMigrate.IO;
using TfsMigrate.Settings;

namespace TfsMigrate
{
    class Program
    {
        static int Main ( string[] args )
        {            
            try
            {
                InitializeLogging();
            
                var options = ParseCommandLine(args);
                if (options == null)
                {
                    PauseIfDebuggerAttached();
                    return -2;
                };

                var settingsManager = GetSettingsManager(options.Settings);
                var hostSettings = LoadHostSettings(settingsManager);
                UpdateSettings(hostSettings, options);

                var host = new ConsoleProcessorRunner();
                host.RunProcessorAsync(options.Processor, settingsManager, hostSettings).Wait();

                Logger.Info($"Errors = {_logCounter.ErrorCount}, Warnings = {_logCounter.WarningCount}");                
            } catch
            {
                PauseIfDebuggerAttached();
                return -1;
            };            

            PauseIfDebuggerAttached();
            return 0;
        }

        #region Private Members
        
        static ISettingsManager GetSettingsManager ( string settingsPath )
        {
            if (String.IsNullOrEmpty(settingsPath))
                settingsPath = "settings.json";

            return new JsonSettingsManager(settingsPath);
        }
        
        static void InitializeLogging ( )
        {
            LogManager.AddTarget(_logCounter);

            Logger.BeginScope(LogManager.GetAppLogger());
        }

        static HostSettings LoadHostSettings ( ISettingsManager settingsManager )
        {
            try
            {
                return settingsManager.GetSettingsAsync<HostSettings>("Global", CancellationToken.None).Result;
            } catch (Exception e)
            {                
                Logger.Error(e.GetRootException());
                throw;
            };
        }

        static void PauseIfDebuggerAttached ( )
        {
            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press ENTER to quit");
                Console.ReadLine();
            };
        }

        static void UpdateSettings ( HostSettings settings, CommandLineOptions options )
        {
            //Command line overrides settings
            if (options.Verbose)
                settings.Debug = true;

            //Update logging based upon the options and settings
            if (settings.Debug)
                LogManager.SetVerboseLogging();

            if (!String.IsNullOrEmpty(options.LogFile))
            {
                if (String.IsNullOrEmpty(System.IO.Path.GetDirectoryName(options.LogFile)))
                    options.LogFile = FileSystem.BuildPath(settings.OutputPath, options.LogFile);

                LogManager.AddFileLogger(options.LogFile);
            };
        }

        static CommandLineOptions ParseCommandLine ( string[] arguments )
        {
            var options = new CommandLineOptions();
            var parser = new CommandLineParser(options);
            parser.Parse();

            if (parser.HasErrors)
                Console.WriteLine(parser.UsageInfo.GetErrorsAsString(80));

            if (parser.HasErrors || options.Help)
            { 
                Console.WriteLine(parser.UsageInfo.GetOptionsAsString(80));
                return null;
            };

            return options;
        }

        static readonly CountingTarget _logCounter = new CountingTarget("Counter");
        #endregion
    }
}
