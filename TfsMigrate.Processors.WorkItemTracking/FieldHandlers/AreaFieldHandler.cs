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
    class AreaFieldHandler : FieldHandler
    {
        public delegate Task<MigratedArea> MigrateAreaDelegate ( string areaName, CancellationToken cancellationToken );

        public MigrateAreaDelegate OnMigrateAsync { get; set; }

        public override async Task<WorkItemFieldValue> HandleAsync ( JsonPatchDocument document, WorkItemFieldValue field, CancellationToken cancellationToken )
        {
            var targetArea = field.Value?.ToString();

            //If the value is empty then we cannot migrate it but we cannot remove it either so we'll ignore the "value"
            if (String.IsNullOrEmpty(targetArea))
            {
                Logger.Warning($"Area is empty (which is illegal) so skipping the update");
                return null;
            };

            //If the area has already been migrated then just convert it as is
            var migratedArea = Context.MigratedAreas.FirstOrDefault(a => String.Compare(a.SourcePath.FullPath, targetArea, true) == 0 && (a.Succeeded || a.Skipped));
            if (migratedArea == null)
            {
                //Migrate the area first
                migratedArea = await OnMigrateAsync(targetArea, cancellationToken).ConfigureAwait(false);
            };

            return field;
        }
    }
}
