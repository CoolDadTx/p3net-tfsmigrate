/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using TfsMigrate.Diagnostics;
using TfsMigrate.Processors.WorkItemTracking.FieldHandlers;
using TfsMigrate.Tfs;

namespace TfsMigrate.Processors.WorkItemTracking.Data
{
    public class MigrationContext
    {
        internal List<MigratedArea> MigratedAreas { get; } = new List<MigratedArea>();

        internal List<MigratedIteration> MigratedIterations { get; } = new List<MigratedIteration>();

        internal List<int> WorkItemsToMigrate { get; } = new List<int>();

        public List<MigratedWorkItem> MigratedWorkItems { get; } = new List<MigratedWorkItem>();

        public TfsServer SourceServer { get; set; }
        
        public WorkItemTrackingHttpClient SourceService => SourceServer?.GetClient<WorkItemTrackingHttpClient>();
        
        public string SourceProjectName { get; set; }

        public string TargetProjectName { get; set; }        

        public TfsServer TargetServer { get; set; }

        public WorkItemTrackingHttpClient TargetService => TargetServer.GetClient<WorkItemTrackingHttpClient>();

        public IDictionary<string, string> TargetUsers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        internal Dictionary<string, List<IFieldHandler>> FieldHandlers = new Dictionary<string, List<IFieldHandler>>(StringComparer.OrdinalIgnoreCase);

        public async Task<TeamProject> GetSourceProjectAsync ( CancellationToken cancellationToken )
        {
            if (_source == null && SourceServer != null && !String.IsNullOrEmpty(SourceProjectName))
                _source = await SourceServer.FindProjectAsync(SourceProjectName, cancellationToken).ConfigureAwait(false);

            return _source;
        }

        public async Task<IEnumerable<string>> GetTargetFieldsAsync ( CancellationToken cancellationToken )
        {
            if (_targetFields == null)
            {
                Logger.Debug($"Getting defined work item fields for '{TargetProjectName}");

                var fields = new List<string>();
                var targetProject = await GetTargetProjectAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                var items = await TargetService.GetFieldsAsync(targetProject.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (items != null)
                    fields.AddRange(items.Select(i => i.ReferenceName));

                _targetFields = fields;
            };

            return _targetFields;
        }

        public async Task<TeamProject> GetTargetProjectAsync ( CancellationToken cancellationToken )
        {
            if (_target == null && TargetServer != null && !String.IsNullOrEmpty(TargetProjectName))
                _target = await TargetServer.FindProjectAsync(TargetProjectName, cancellationToken).ConfigureAwait(false);

            return _target;
        }

        //Not efficient but it works
        public MigratedWorkItem GetMigratedWorkItem ( int sourceId ) => MigratedWorkItems.FirstOrDefault(i => i.SourceId == sourceId);

        public bool HasMigratedWorkItem ( int sourceId ) => GetMigratedWorkItem(sourceId) != null;

        internal Queue<MigratedWorkItem> MigrationQueue { get; set; } = new Queue<MigratedWorkItem>();
        #region Private

        private TeamProject _source, _target;
        private List<string> _targetFields;
        #endregion
    }
}
