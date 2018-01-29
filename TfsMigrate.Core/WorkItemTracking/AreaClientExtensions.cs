/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

using TfsMigrate.Diagnostics;
using TfsMigrate.Tfs;

namespace TfsMigrate.WorkItemTracking
{
    public static class AreaClientExtensions
    {        
        public static async Task<WorkItemClassificationNode> CreateAreaAsync ( this WorkItemTrackingHttpClient source, NodePath areaPath, CancellationToken cancellationToken )
        {
            //Get the parent area, if any            
            if (areaPath.Parent != null)
            {
                var parentArea = await source.GetAreaAsync(areaPath.Parent, false, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                if (parentArea == null)
                    parentArea = await source.CreateAreaAsync(areaPath.Parent, cancellationToken).ConfigureAwait(false);
            };

            var newArea = new WorkItemClassificationNode() {
                Name = areaPath.Name
            };
            
            newArea = await source.CreateOrUpdateClassificationNodeAsync(newArea, areaPath.Project, TreeStructureGroup.Areas, path: areaPath.Parent?.RelativePath, cancellationToken: cancellationToken).ConfigureAwait(false);
            Logger.Debug($"Created area '{areaPath}' with Id {newArea.Id}");

            return newArea;
        }

        public static async Task DeleteAreaAsync ( this WorkItemTrackingHttpClient source, NodePath existingPath, string newPath, CancellationToken cancellationToken )
        {
            var newNode = await source.GetAreaAsync(new NodePath(newPath), true, cancellationToken).ConfigureAwait(false);

            await source.DeleteClassificationNodeAsync(existingPath.Project, TreeStructureGroup.Areas, path: existingPath.RelativePath, reclassifyId: newNode.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
                          
            Logger.Debug($"Deleted area '{existingPath}");
        }
        
        public static async Task<WorkItemClassificationNode> GetAreaAsync ( this WorkItemTrackingHttpClient source, NodePath areaPath, bool throwIfNotFound, CancellationToken cancellationToken )
        {
            Logger.Debug($"Getting area '{areaPath}'");

            var area = await source.TryCatchAsync(c => c.GetClassificationNodeAsync(areaPath.Project, TreeStructureGroup.Areas, path: areaPath.RelativePath, depth: 20, cancellationToken: cancellationToken)).ConfigureAwait(false);                
            cancellationToken.ThrowIfCancellationRequested();

            if (area == null && throwIfNotFound)
                throw new Exception("Area not found");
            
            return area;
        }

        public static async Task<IEnumerable<WorkItemClassificationNode>> GetAreasAsync ( this WorkItemTrackingHttpClient source, string project, CancellationToken cancellationToken )
        {
            Logger.Debug($"Getting areas for '{project}'");

            var root = await source.GetClassificationNodeAsync(project, TreeStructureGroup.Areas, null, depth: 20, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (root != null)
                return root.Children;

            return Enumerable.Empty<WorkItemClassificationNode>();
        }
    }
}
