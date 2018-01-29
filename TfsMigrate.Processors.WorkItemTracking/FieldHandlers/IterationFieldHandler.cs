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

using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using TfsMigrate.Diagnostics;
using TfsMigrate.Processors.WorkItemTracking.Data;
using TfsMigrate.WorkItemTracking;

namespace TfsMigrate.Processors.WorkItemTracking.FieldHandlers
{
    class IterationFieldHandler : FieldHandler
    {
        public delegate Task<MigratedIteration> MigrateIterationDelegate ( string iterationName, CancellationToken cancellationToken );

        public MigrateIterationDelegate OnMigrateAsync { get; set; }

        public override async Task<WorkItemFieldValue> HandleAsync ( JsonPatchDocument document, WorkItemFieldValue field, CancellationToken cancellationToken )
        {
            var targetIteration = field.Value?.ToString();

            //If the value is empty then we cannot migrate it but we cannot remove it either so we'll ignore the "value"
            if (String.IsNullOrEmpty(targetIteration))
            {
                Logger.Warning($"Iteration is empty (which is illegal) so skipping the update");
                return null;
            };

            //If the iteration has already been migrated then just convert it as is
            var migratedIteration = Context.MigratedIterations.FirstOrDefault(a => String.Compare(a.SourcePath.FullPath, targetIteration, true) == 0 && (a.Succeeded || a.Skipped));
            if (migratedIteration == null)
            {
                //Migrate it
                migratedIteration = await OnMigrateAsync(targetIteration, cancellationToken).ConfigureAwait(false);
            };

            return field;
        }
    }
}
