/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Fsmb.Apollo;

using Microsoft.TeamFoundation.SourceControl.WebApi;
using TfsMigrate.Data;

namespace TfsMigrate.Processors.VersionControl.Data
{
    class MigratedProject : MigratedObject
    {
        public string SourcePath { get; set; }

        public string LocalFullPath { get; set; }

        public string DestinationPath { get; set; }

        public string DestinationFullPath => StringExtensions.Combine(DestinationProjectName, DestinationPath);
        
        public string DestinationProjectName { get; set; }

        public GitRepository DestinationRepo { get; set; }

        public string BaselinePath { get; set; }

        public string DevelopmentPath { get; set; }

        public Version Version { get; set; }

        public void StartProfiling () => _elapsedTime.Start();
        public TimeSpan StopProfiling ()
        {
            _elapsedTime.Stop();
            return _elapsedTime.Elapsed;
        }

        public TimeSpan ElapsedTime => _elapsedTime.Elapsed;

        #region Private Members

        private Stopwatch _elapsedTime = new Stopwatch();
        #endregion
    }
}
