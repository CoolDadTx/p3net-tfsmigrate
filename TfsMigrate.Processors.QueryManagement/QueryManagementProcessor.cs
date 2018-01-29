/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Fsmb.Apollo;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

using TfsMigrate.Data;
using TfsMigrate.Diagnostics;
using TfsMigrate.Processors.QueryManagement.Data;
using TfsMigrate.Tfs;
using TfsMigrate.WorkItemTracking;

namespace TfsMigrate.Processors.QueryManagement
{
    public class QueryManagementProcessor : Processor
    {
        protected override async Task InitializeCoreAsync ( CancellationToken cancellationToken )
        {
            await base.InitializeCoreAsync(cancellationToken).ConfigureAwait(false);

            Settings = await GetSettingsAsync<QueryManagementSettings>("QueryManagement", cancellationToken).ConfigureAwait(false) ?? new QueryManagementSettings();
        }

        protected override async Task RunCoreAsync ( CancellationToken cancellationToken )
        {
            var context = new MigrationContext() {
                SourceServer = new TfsServer(Host.Settings.SourceCollectionUrl, Host.Settings.SourceAccessToken),
                TargetServer = new TfsServer(Host.Settings.TargetCollectionUrl, Host.Settings.TargetAccessToken)
            };

            context.SourceProject = await context.SourceServer.FindProjectAsync(Host.Settings.SourceProject, cancellationToken).ConfigureAwait(false);
            context.TargetProject = await context.TargetServer.FindProjectAsync(Host.Settings.TargetProject, cancellationToken).ConfigureAwait(false);

            await MigrateQueriesAsync(context, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

        #region Private Members

        private bool IsQueryExcluded ( IEnumerable<string> excludedQueries, string queryPath )
        {
            //It needs to match one of the excluded queries, ignoring shared
            queryPath = queryPath.ToRelativeSharedQueryPath();

            foreach (var query in excludedQueries)
            {
                if (query.Contains("*"))
                {
                    var basePath = query.LeftOf("*").Trim('/', '\\');
                    if (queryPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                        return true;

                } else if (String.Compare(query.Trim('/', '\\'), queryPath.Trim('/', '\\'), true) == 0)
                    return true;
            };

            return false;
        }
        
        private async Task MigrateQueriesAsync ( MigrationContext context, CancellationToken cancellationToken )
        {
            Logger.StartActivity($"Migrating Queries");
            using (var logger = Logger.BeginScope("MigrateQueries"))
            {
                //Get the list of queries to migrate
                var sourceQueries = await context.SourceService.GetSharedQueriesAsync(context.SourceProject, cancellationToken).ConfigureAwait(false);

                //Enumerate the items to be migrated                
                foreach (var query in sourceQueries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    context.Queries.Add(await MigrateQueryAsync(context, query, cancellationToken).ConfigureAwait(false));
                };
            };
            Logger.StopActivity($"Migrated Queries: {context.Queries.Succeeded()} Succeeded, {context.Queries.Skipped()} Skipped, {context.Queries.Errors()} Failed");
        }

        private async Task<MigratedQuery> MigrateQueryAsync ( MigrationContext context, QueryHierarchyItem query, CancellationToken cancellationToken )
        {
            Logger.StartActivity($"Migrating query '{query.Path}'");

            var migratedItem = new MigratedQuery(query);

            using (var logger = Logger.BeginScope("MigrateQuery"))
            {
                try
                {
                    //Is this excluded
                    if (IsQueryExcluded(Settings.ExcludeQueries, query.Path))
                    {
                        Logger.Warning($"Query is excluded, skipping");
                        migratedItem.Skipped = true;
                        return migratedItem;
                    };

                    //Does the query already exist                    
                    var existingItem = await context.TargetService.GetQueryAsync(context.TargetProject, migratedItem.Name, false, cancellationToken).ConfigureAwait(false);
                    if (existingItem != null && Settings.Overwrite)
                    {
                        Logger.Info($"Query '{migratedItem.Name}' already exists and overwrite is true, removing query");

                        //Remove query
                        cancellationToken.ThrowIfCancellationRequested();
                        await context.TargetService.RemoveQueryAsync(context.TargetProject, existingItem, cancellationToken).ConfigureAwait(false);

                        existingItem = null;
                    };
                                        
                    cancellationToken.ThrowIfCancellationRequested();
                    query = await context.TargetService.AddQueryAsync(context.TargetProject, query, cancellationToken).ConfigureAwait(false);
                    Logger.Info($"Migrated query '{query.Name}', Id = {query.Id}");                    
                } catch (Exception e)
                {
                    migratedItem.Error = e;

                    Logger.Error(e);
                };
            };

            return migratedItem;
        }

        private QueryManagementSettings Settings { get; set; }
        #endregion
    }
}
