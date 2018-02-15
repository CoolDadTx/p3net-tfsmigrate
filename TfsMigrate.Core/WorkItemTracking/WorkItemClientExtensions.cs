/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using P3Net.Kraken;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

using TfsMigrate.Tfs;

namespace TfsMigrate.WorkItemTracking
{
    public static class WorkItemClientExtensions
    {
        public static Task<WorkItem> CreateWorkItemUnrestrictedAsync ( this WorkItemTrackingHttpClient source, TeamProject project, JsonPatchDocument document, string itemType, CancellationToken cancellationToken )
        {
            //Null values aren't allowed when creating a new item so remove any patches that are null
            document.RemoveAll(o => o.Value == null);

            return source.CreateWorkItemAsync(document, project.Name, itemType, bypassRules: true, cancellationToken: cancellationToken);
        }

        public static async Task<WorkItem> GetWorkItemAsync ( this WorkItemTrackingHttpClient source, int id, bool includeChildren, bool throwIfNotFound, CancellationToken cancellationToken )
        {
            var results = await GetWorkItemsAsync(source, new[] { id }, includeChildren, cancellationToken).ConfigureAwait(false);

            var item = results?.FirstOrDefault();
            if (item != null || !throwIfNotFound)
                return item;

            throw new Exception("Work item not found");
        }

        public static async Task<IEnumerable<WorkItem>> GetWorkItemsAsync ( this WorkItemTrackingHttpClient source, IEnumerable<int> ids, bool includeChildren, CancellationToken cancellationToken )
        {
            var expandFlags = WorkItemExpand.Fields;
            if (includeChildren)
                expandFlags = WorkItemExpand.All;

            var results = await source.TryCatchAsync(c => c.GetWorkItemsAsync(ids, expand: expandFlags, errorPolicy: WorkItemErrorPolicy.Omit, cancellationToken: cancellationToken)).ConfigureAwait(false);
             

            return results ?? Enumerable.Empty<WorkItem>();
        }

        public static async Task<IEnumerable<WorkItemUpdate>> GetWorkItemHistoryAsync ( this WorkItemTrackingHttpClient source, int id, CancellationToken cancellationToken )
        {
            var comments = new List<WorkItemUpdate>();

            var skip = 0;
            while (true)
            {
                var updates = await source.GetUpdatesAsync(id, top: 100, skip: skip, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (updates == null || !updates.Any())
                    break;

                comments.AddRange(updates);

                skip += updates.Count;
            };

            return comments;
        }

        //Gets the work item IDs resulting from the given query
        public static async Task<IEnumerable<WorkItemReference>> QueryAsync ( this WorkItemTrackingHttpClient source, Guid id, CancellationToken cancellationToken )
        {
            var result = await source.TryCatchAsync(c => c.QueryByIdAsync(id, cancellationToken: cancellationToken)).ConfigureAwait(false);

            //The results may be a flat list or a tree so flatten to a list
            var items = new List<WorkItemReference>();

            if (result.WorkItems != null)
                items.AddRange(result.WorkItems);

            //If there is a tree of items then we want to add the target which includes the root node and each child
            if (result.WorkItemRelations != null)
            {
                var relations = result.WorkItemRelations.Select(r => r.Target);

                foreach (var relation in relations)
                    if (!items.Any(i => i.Id == relation.Id))
                        items.Add(relation);
            };

            return items;
        }

        public static Task<WorkItem> UpdateWorkItemUnrestrictedAsync ( this WorkItemTrackingHttpClient source, WorkItem item, JsonPatchDocument document, CancellationToken cancellationToken )
        { 
            //Null values should be treated as remove on updates
            foreach (var op in document)
            {
                if (String.IsNullOrEmpty(op.Value?.ToString()))
                {
                    op.Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Remove;
                    op.Value = null;  //required by API
                };
            };

            return source.UpdateWorkItemAsync(document, item.Id.Value, bypassRules: true, cancellationToken: cancellationToken);
        }
    }
}
