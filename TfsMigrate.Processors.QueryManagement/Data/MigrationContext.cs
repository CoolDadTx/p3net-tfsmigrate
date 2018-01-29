/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;

using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;

using TfsMigrate.Tfs;

namespace TfsMigrate.Processors.QueryManagement.Data
{
    class MigrationContext
    {
        public TfsServer SourceServer { get; set; }

        public WorkItemTrackingHttpClient SourceService => SourceServer?.GetClient<WorkItemTrackingHttpClient>();

        public TeamProject SourceProject { get; set; }

        public TfsServer TargetServer { get; set; }

        public WorkItemTrackingHttpClient TargetService => TargetServer?.GetClient<WorkItemTrackingHttpClient>();

        public TeamProject TargetProject { get; set; }

        public List<MigratedQuery> Queries { get; } = new List<MigratedQuery>();
    }
}
