/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using Plossum.CommandLine;

namespace TfsMigrate.CommandLine
{
    [CommandLineManager(ApplicationName = "TfsMigrate", EnabledOptionStyles = OptionStyles.Windows | OptionStyles.Unix)]
    internal class CommandLineOptions
    {
        [CommandLineOption(Name="help", Aliases="?", Description = "Display help", BoolFunction = BoolFunction.TrueIfPresent)]
        public bool Help { get; set; }

        [CommandLineOption(Name = "logFile", Description = "The file to log to (default: TfsMigrate.log)")]
        public string LogFile { get; set; } = "TfsMigrate.log";

        [CommandLineOption(Name="processor", Description = "The processor to run", MinOccurs = 1)]
        public string Processor { get; set; }

        [CommandLineOption(Name="settings", Description = "The path and name of the settings file (default: settings.json)")]
        public string Settings { get; set; } = "settings.json";

        [CommandLineOption(Name="v", Aliases="verbose", Description = "Set to true for verbose debugging. Can also be set in the settings files.", BoolFunction = BoolFunction.TrueIfPresent)]
        public bool Verbose { get; set; }
    }
}