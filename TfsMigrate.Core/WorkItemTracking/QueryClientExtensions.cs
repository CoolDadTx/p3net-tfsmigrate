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

using TfsMigrate.Diagnostics;
using TfsMigrate.Tfs;

namespace TfsMigrate.WorkItemTracking
{
    public static class QueryClientExtensions
    {
        public static async Task<QueryHierarchyItem> AddQueryAsync ( this WorkItemTrackingHttpClient source, TeamProject project, QueryHierarchyItem query, CancellationToken cancellationToken )
        {
            //Have to build up the path to the query
            var queryPath = new QueryPath(query.Path);
            if (queryPath.Parent != null && !queryPath.Parent.IsRoot)
                await source.AddFolderAsync(project, queryPath.Parent, cancellationToken: cancellationToken).ConfigureAwait(false);

            //Clone the query
            var newQuery = new QueryHierarchyItem() {
                Clauses = query.Clauses,
                Columns = query.Columns,
                FilterOptions = query.FilterOptions,
                IsPublic = query.IsPublic,
                LinkClauses = query.LinkClauses,
                Name = query.Name,
                QueryType = query.QueryType,
                SortColumns = query.SortColumns,
                SourceClauses = query.SourceClauses,
                TargetClauses = query.TargetClauses,
                Wiql = query.Wiql
            };

            return await source.CreateQueryAsync(newQuery, project.Id, queryPath.Parent?.FullPath, cancellationToken: cancellationToken).ConfigureAwait(false);
        }        
        
        public static async Task<List<QueryHierarchyItem>> GetSharedQueriesAsync ( this WorkItemTrackingHttpClient source, TeamProject project, CancellationToken cancellationToken )
        {
            //We are limited to 2 levels so we have to query for each level one by one
            var queries = new List<QueryHierarchyItem>();

            queries.AddRange(await source.GetFolderContentsAsync(project, QueryExtensions.SharedQueriesPath, cancellationToken: cancellationToken).ConfigureAwait(false));

            return queries;
        }

        public static async Task<QueryHierarchyItem> GetQueryAsync ( this WorkItemTrackingHttpClient source, TeamProject project, string queryPath, bool throwIfNotExists, CancellationToken cancellationToken )
        {            
            var query = await source.TryCatchAsync(c => c.GetQueryAsync(project.Id, queryPath, QueryExpand.All, 
                                                            depth: 1, includeDeleted: false, cancellationToken: cancellationToken), 
                                                            new[] { "TF401243" }).ConfigureAwait(false);

            if (query != null && query.Id != Guid.Empty)
                return query;

            if (throwIfNotExists)
                throw new Exception($"Query '{queryPath}' not found");

            return null;
        }

        public static async Task RemoveQueryAsync ( this WorkItemTrackingHttpClient source, TeamProject project, QueryHierarchyItem query, CancellationToken cancellationToken )
        { 
            await source.DeleteQueryAsync(project.Id, query.Path, cancellationToken: cancellationToken);
            Logger.Debug($"Removed query '{project}/{query.Path}'");
        }
    }
}
