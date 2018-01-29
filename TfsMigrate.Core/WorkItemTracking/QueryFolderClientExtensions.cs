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
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

using TfsMigrate.Tfs;

namespace TfsMigrate.WorkItemTracking
{
    public static class QueryFolderClientExtensions
    {
        public static async Task<QueryHierarchyItem> AddFolderAsync ( this WorkItemTrackingHttpClient source, TeamProject project, QueryPath folderPath, CancellationToken cancellationToken )
        {            
            var folder = await source.GetFolderAsync(project, folderPath.FullPath, cancellationToken).ConfigureAwait(false);
            if (folder != null)
                return folder;

            //Ensure the parent folder exists, unless it is a root path
            cancellationToken.ThrowIfCancellationRequested();
            if (folderPath.Parent != null && !folderPath.Parent.IsRoot)            
                await source.AddFolderAsync(project, folderPath.Parent, cancellationToken).ConfigureAwait(false);

            var query = new QueryHierarchyItem() { Name = folderPath.Name, IsFolder = true };            

            cancellationToken.ThrowIfCancellationRequested();
            return await source.CreateQueryAsync(query, project.Id, folderPath.Parent?.FullPath, cancellationToken: cancellationToken).ConfigureAwait(false);
        }        

        public static async Task<QueryHierarchyItem> GetFolderAsync ( this WorkItemTrackingHttpClient source, TeamProject project, string folderPath, CancellationToken cancellationToken )
        {
            var query = await source.TryCatchAsync(c => c.GetQueryAsync(project.Id, folderPath, QueryExpand.All,
                                                            depth: 0, includeDeleted: false, cancellationToken: cancellationToken),
                                                            new[] { "TF401243" }).ConfigureAwait(false);

            if (query == null || query.Id == Guid.Empty)
                return null;

            return query;
        }

        public static async Task<IEnumerable<QueryHierarchyItem>> GetFolderContentsAsync ( this WorkItemTrackingHttpClient source, TeamProject project, string folderPath, CancellationToken cancellationToken )
        {
            var query = await source.TryCatchAsync(c => c.GetQueryAsync(project.Id, folderPath, QueryExpand.All,
                                                            depth: 1, includeDeleted: false, cancellationToken: cancellationToken),
                                                            new[] { "TF401243" }).ConfigureAwait(false);

            if (query == null || query.Id == Guid.Empty || !query.HasChildren.GetValueOrDefault())
                return Enumerable.Empty<QueryHierarchyItem>();

            //Enumerate the children
            var items = new List<QueryHierarchyItem>();
            foreach (var child in query.Children)
            {
                if (child.IsFolder.GetValueOrDefault())
                    items.AddRange(await source.GetFolderContentsAsync(project, child.Path, cancellationToken).ConfigureAwait(false));
                else
                    items.Add(child);

                cancellationToken.ThrowIfCancellationRequested();
            };

            return items;
        }
    }
}
