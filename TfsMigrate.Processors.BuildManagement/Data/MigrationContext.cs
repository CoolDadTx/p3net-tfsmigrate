/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;

using TfsMigrate.Processors.BuildManagement.Tfs;
using TfsMigrate.Tfs;

namespace TfsMigrate.Processors.BuildManagement.Data
{
    class MigrationContext
    {
        #region Construction

        public MigrationContext ()
        {
            _sourceProject = new Lazy<TeamProject>(() => SourceServer?.FindProjectAsync(SourceProjectName, CancellationToken.None).Result);
            _targetProject = new Lazy<TeamProject>(() => TargetServer?.FindProjectAsync(TargetProjectName, CancellationToken.None).Result);
        }
        #endregion

        public TfsServer SourceServer { get; set; }

        public BuildHttpClient SourceService => SourceServer?.GetClient<BuildHttpClient>();

        public string SourceProjectName { get; set; }

        public TeamProject SourceProject => _sourceProject.Value;

        public TfsServer TargetServer { get; set; }

        public BuildHttpClient TargetService => TargetServer?.GetClient<BuildHttpClient>();

        public string TargetProjectName { get; set; }

        public TeamProject TargetProject => _targetProject.Value;

        public List<MigratedBuildDefinition> Definitions { get; } = new List<MigratedBuildDefinition>();
        public List<MigratedBuildTemplate> Templates { get; } = new List<MigratedBuildTemplate>();

        #region Private Members

        private readonly Lazy<TeamProject> _sourceProject;
        private readonly Lazy<TeamProject> _targetProject;
        #endregion
    }
}
