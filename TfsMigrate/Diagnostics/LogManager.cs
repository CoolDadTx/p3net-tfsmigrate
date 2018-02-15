/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NLog.Targets;
using NLog.Targets.Wrappers;
using TfsMigrate.IO;

namespace TfsMigrate.Diagnostics
{
    public static class LogManager
    {
        public static ILogger GetAppLogger () => GetLogger("App");

        public static ILogger GetLogger ( string name )
        {
            var logger = NLog.LogManager.GetLogger(name);

            return new NLogLogger(logger);
        }

        public static void AddFileLogger ( string filePath )
        {
            //Find the existing console logger target
            var existingTarget = NLog.LogManager.Configuration.FindTargetByName<TargetWithLayout>("console");
            if (existingTarget == null)
                return;

            var filename = Path.GetFileNameWithoutExtension(filePath) + DateTime.Now.ToString("_yyyyMMdd_hhmmss");
            var basePath = Path.GetDirectoryName(filePath);
            var extension = Path.GetExtension(filePath);

            filePath = FileSystem.BuildPath(basePath, filename + extension);

            //Create a new target that uses the same settings
            var fileTarget = new NLog.Targets.FileTarget("file") 
            {
                AutoFlush = true,
                Layout = existingTarget.Layout,                
                CreateDirs = true,
                KeepFileOpen = true,
                FileName = filePath,                
            };

            AddTarget(fileTarget);
        }

        public static void AddTarget ( Target target )
        {
            //Add to the userLoggers
            var groupTarget = NLog.LogManager.Configuration.FindTargetByName<SplitGroupTarget>("userLoggers");
            groupTarget.Targets.Add(target);

            NLog.LogManager.ReconfigExistingLoggers();
        }

        public static void SetVerboseLogging ()
        {
            //Enumerate all the rules
            foreach (var rule in NLog.LogManager.Configuration.LoggingRules)
            {
                //Enable debug logging
                rule.EnableLoggingForLevels(NLog.LogLevel.Debug, NLog.LogLevel.Fatal);
            };
                
            NLog.LogManager.ReconfigExistingLoggers();
        }
    }
}
