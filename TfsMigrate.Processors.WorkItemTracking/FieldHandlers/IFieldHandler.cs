/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

using TfsMigrate.Processors.WorkItemTracking.Data;
using TfsMigrate.WorkItemTracking;

namespace TfsMigrate.Processors.WorkItemTracking.FieldHandlers
{
    public interface IFieldHandler
    {
        void Initialize ( MigrationContext context, WorkItemTrackingSettings settings );

        Task<WorkItemFieldValue> HandleAsync ( JsonPatchDocument document, WorkItemFieldValue field, CancellationToken cancellationToken );
    }

    public abstract class FieldHandler : IFieldHandler
    {
        public abstract Task<WorkItemFieldValue> HandleAsync ( JsonPatchDocument document, WorkItemFieldValue field, CancellationToken cancellationToken );

        public virtual void Initialize ( MigrationContext context, WorkItemTrackingSettings settings )
        {
            Context = context;
            Settings = settings;
        }

        protected MigrationContext Context { get; private set; }

        protected WorkItemTrackingSettings Settings { get; private set; }
    }
}
