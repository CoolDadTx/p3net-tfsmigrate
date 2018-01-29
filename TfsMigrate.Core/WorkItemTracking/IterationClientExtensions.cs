/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Fsmb.Apollo;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

using TfsMigrate.Diagnostics;
using TfsMigrate.Tfs;

namespace TfsMigrate.WorkItemTracking
{
    public static class IterationClientExtensions
    {        
        public static async Task<WorkItemClassificationNode> CreateIterationAsync ( this WorkItemTrackingHttpClient source, NodePath nodePath, DateRange dates, CancellationToken cancellationToken )
        {
            //Get the parent node, if any            
            if (nodePath.Parent != null)
            {
                var parent = await source.GetIterationAsync(nodePath.Parent, false, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                if (parent == null)
                    parent = await source.CreateIterationAsync(nodePath.Parent, DateRange.Empty, cancellationToken).ConfigureAwait(false);
            };

            var newItem = new WorkItemClassificationNode() {
                Name = nodePath.Name
            };

            if (dates != DateRange.Empty)
            {
                newItem.SetStartDate(dates.Start);
                newItem.SetFinishDate(dates.End);
            };
            
            newItem = await source.CreateOrUpdateClassificationNodeAsync(newItem, nodePath.Project, TreeStructureGroup.Iterations, path: nodePath.Parent?.RelativePath, cancellationToken: cancellationToken).ConfigureAwait(false);
            
            if (dates != DateRange.Empty)
                Logger.Debug($"Created iteration '{newItem.Name}' with dates {dates} and Id {newItem.Id}");
            else
                Logger.Debug($"Created iteration '{newItem.Name}' with Id {newItem.Id}");

            return newItem;
        }
        public static async Task DeleteIterationAsync ( this WorkItemTrackingHttpClient source, NodePath existingPath, string newPath, CancellationToken cancellationToken )
        {
            var newNode = await source.GetIterationAsync(new NodePath(newPath), true, cancellationToken).ConfigureAwait(false);

            await source.DeleteClassificationNodeAsync(existingPath.Project, TreeStructureGroup.Iterations, path: existingPath.RelativePath, reclassifyId: newNode.Id, cancellationToken: cancellationToken).ConfigureAwait(false);

            Logger.Debug($"Deleted iteration '{existingPath}");
        }

        public static async Task<WorkItemClassificationNode> GetIterationAsync ( this WorkItemTrackingHttpClient source, NodePath nodePath, bool throwIfNotFound, CancellationToken cancellationToken )
        {
            Logger.Debug($"Getting iteration '{nodePath.FullPath}'");

            var iteration = await source.TryCatchAsync(c => c.GetClassificationNodeAsync(nodePath.Project, TreeStructureGroup.Iterations, path: nodePath.RelativePath, depth: 20, cancellationToken: cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (iteration == null && throwIfNotFound)
                throw new Exception("Iteration not found");

            return iteration;
        }
    }
}
