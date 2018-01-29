/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.TeamFoundation.SourceControl.WebApi;

using TfsMigrate.Processors.VersionControl.Git;
using TfsMigrate.Tfs;

namespace TfsMigrate.Processors.VersionControl.Data
{
    internal class MigrationContext
    {        
        public TfsServer SourceServer { get; set; }

        public TfvcHttpClient SourceService => SourceServer?.GetClient<TfvcHttpClient>();
        
        public TfsServer TargetServer { get; set; }

        public GitHttpClient TargetService => TargetServer?.GetClient<GitHttpClient>();

        public GitCommand GitCommand { get; set; }

        public List<MigratedProject> Projects { get; } = new List<MigratedProject>();

        public string OutputPath { get; set; }
        
        public void StartProfiling () => _elapsedTime.Start();
        public TimeSpan StopProfiling ()
        {
            _elapsedTime.Stop();
            return _elapsedTime.Elapsed;
        }

        public TimeSpan ElapsedTime => _elapsedTime.Elapsed;

        private Stopwatch _elapsedTime = new Stopwatch();
    }
}
