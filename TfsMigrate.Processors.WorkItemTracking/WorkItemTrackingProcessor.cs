/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using P3Net.Kraken;
using TfsMigrate.Data;
using TfsMigrate.Diagnostics;
using TfsMigrate.Processors.WorkItemTracking.Data;
using TfsMigrate.Processors.WorkItemTracking.FieldHandlers;
using TfsMigrate.Tfs;
using TfsMigrate.WorkItemTracking;

namespace TfsMigrate.Processors.WorkItemTracking
{
    public class WorkItemTrackingProcessor : Processor
    {
        protected override async Task InitializeCoreAsync( CancellationToken cancellationToken )
        {
            await base.InitializeCoreAsync(cancellationToken).ConfigureAwait(false);

            Settings = await Host.GetSettingsAsync<WorkItemTrackingSettings>("WorkItemTracking", cancellationToken).ConfigureAwait(false) ?? new WorkItemTrackingSettings();
        }

        protected override async Task RunCoreAsync( CancellationToken cancellationToken )
        {
            var context = new MigrationContext()
            {
                SourceServer = new TfsServer(Host.Settings.SourceCollectionUrl, Host.Settings.SourceAccessToken),
                SourceProjectName = Host.Settings.SourceProject,

                TargetServer = new TfsServer(Host.Settings.TargetCollectionUrl, Host.Settings.TargetAccessToken),
                TargetProjectName = Host.Settings.TargetProject
            };

            //Load the users
            await LoadUsersAsync(context, Settings.Users, cancellationToken).ConfigureAwait(false);

            //Set up the field handlers
            InitializeFieldHandlers(context);
            cancellationToken.ThrowIfCancellationRequested();

            await MigrateAreasAsync(context, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            await MigrateIterationsAsync(context, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            await MigrateWorkItemsAsync(context, cancellationToken).ConfigureAwait(false);
        }

        #region Private Members

        private async Task AddMigrationCommentAsync( MigrationContext context, WorkItem sourceItem, WorkItem targetItem, CancellationToken cancellationToken )
        {
            var msg = $"<p>Migrated from TFS.</p><p>Old ID = {sourceItem.Id}</p><p>Old Url = {sourceItem.Url}</p>";

            var doc = new JsonPatchDocument();
            doc.AddField(WorkItemFields.History, msg);

            //If there is a tag then add it
            if (!String.IsNullOrEmpty(Settings.MigrationTag))
            {
                //tags are stored as a single value so we need to combine the existing tags with the new one
                var tags = targetItem.TryGetField(WorkItemFields.Tag)?.ValueAsString();
                if (!String.IsNullOrEmpty(tags))
                    tags += $";{Settings.MigrationTag}";
                else
                    tags = Settings.MigrationTag;
                doc.AddField(WorkItemFields.Tag, tags);
            };

            await context.TargetService.UpdateWorkItemUnrestrictedAsync(targetItem, doc, cancellationToken).ConfigureAwait(false);
        }

        private void AddToMigrationQueue( Queue<MigratedWorkItem> queue, MigratedWorkItem item )
        {
            var existing = queue.FirstOrDefault(i => i.SourceId == item.SourceId);
            if (existing == null)
                queue.Enqueue(item);
        }

        private IFieldHandler CreateCustomHandler( string typeName )
        {
            try
            {
                var type = Type.GetType(typeName, true, true);
                return Activator.CreateInstance(type) as IFieldHandler;
            } catch (Exception e)
            {
                throw new Exception($"Failed to load custom type handler '{typeName}'", e);
            };
        }

        private async Task<JsonPatchDocument> CreatePatchDocumentAsync( MigrationContext context, WorkItemUpdate update, CancellationToken cancellationToken )
        {
            var doc = new JsonPatchDocument();

            if (update.Fields == null)
                update.Fields = new Dictionary<string, WorkItemFieldUpdate>(StringComparer.OrdinalIgnoreCase);

            var fields = update.Fields;

            //Ensure that the ChangedBy/Changed Date fields are being set so the history is maintained
            fields.EnsureFieldSet(WorkItemFields.ChangedBy, update.RevisedBy.Name);
            fields.EnsureFieldSet(WorkItemFields.ChangedDate, update.RevisedDate);

            //Copy the fields
            foreach (var field in fields)
            {
                cancellationToken.ThrowIfCancellationRequested();

                context.FieldHandlers.TryGetValue(field.Key, out var handlers);
                if (handlers != null || Settings.IncludeAllFields)
                {
                    var newField = new WorkItemFieldValue() { Name = field.Key, Value = field.Value.NewValue };

                    if (handlers != null)
                    {
                        foreach (var handler in handlers)
                        {
                            newField = await handler.HandleAsync(doc, newField, cancellationToken).ConfigureAwait(false);
                            if (newField == null)
                                break;
                        };
                    };

                    if (newField != null)
                    {
                        //The final check before we add the item is to ensure that the field actually exists in the target system
                        var allowedFields = await context.GetTargetFieldsAsync(cancellationToken).ConfigureAwait(false);
                        if (!allowedFields.Contains(newField.Name, StringComparer.OrdinalIgnoreCase))
                            Logger.Warning($"{newField.Name} was not found in target, skipping");
                        else
                        {
                            var name = (newField.Name != field.Key) ? $"{newField.Name} (renamed from {field.Key})" : newField.Name;
                            var value = newField.Value?.ToString() ?? "";
                            if (value.Length > 75)
                                value = value.Left(75) + "...";

                            Logger.Debug($"{name} = {value}");

                            //Add or update it
                            if (field.Value.OldValue != null)
                                doc.UpdateField(newField.Name, newField.Value);
                            else
                                doc.AddField(newField.Name, newField.Value);
                        };
                    } else
                        Logger.Debug($"{field.Key} is marked as ignore");
                } else
                    Logger.Debug($"{field.Key} has no handlers, skipping");
            };

            return doc;
        }

        private async Task<Queue<MigratedWorkItem>> GetWorkItemsAsync( MigrationContext context, CancellationToken cancellationToken )
        {
            var queue = new Queue<MigratedWorkItem>();

            // Run the queries to get the list of work items to migrate
            Logger.Info("Querying work items");

            foreach (var query in Settings.Queries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var results = await QueryWorkItemsAsync(context, query, cancellationToken).ConfigureAwait(false);
                    foreach (var result in results)
                        AddToMigrationQueue(queue, result);
                } catch (Exception e)
                {
                    Logger.Error(e);
                };
            };
            Logger.Info($"Found {queue.Count} items");

            return queue;
        }

        private void InitializeFieldHandlers( MigrationContext context )
        {
            foreach (var fieldSetting in Settings.Fields)
            {
                var handlers = new List<IFieldHandler>();
                context.FieldHandlers[fieldSetting.Name] = handlers;

                if (fieldSetting.Ignore)
                    handlers.Add(IgnoreFieldHandler.Instance);
                else
                {
                    if (!String.IsNullOrEmpty(fieldSetting.TargetName))
                        handlers.Add(new RenameFieldHandler() { NewName = fieldSetting.TargetName });

                    if (!String.IsNullOrEmpty(fieldSetting.Value))
                        handlers.Add(new ValueFieldHandler() { ExpressionValue = fieldSetting.Value });

                    if (!String.IsNullOrEmpty(fieldSetting.Handler))
                        handlers.Add(CreateCustomHandler(fieldSetting.Handler));

                    if (fieldSetting.IsUser)
                        handlers.Add(UserFieldHandler.Instance);

                    foreach (var handler in handlers)
                    {
                        handler.Initialize(context, Settings);

                        if (handler is AreaFieldHandler areaHandler)
                        {
                            areaHandler.OnMigrateAsync = ( a, ct ) => MigrateAreaAsync(context, new AreaSettings() { SourcePath = a }, ct);
                        } else if (handler is IterationFieldHandler iterationHandler)
                        {
                            iterationHandler.OnMigrateAsync = ( name, ct ) => MigrateIterationAsync(context, new IterationSettings() { SourcePath = name }, ct);
                        };
                    };
                };
            };
        }

        private Task LoadUsersAsync( MigrationContext context, IEnumerable<UserSettings> users, CancellationToken cancellationToken )
        {
            foreach (var user in users)
            {
                context.TargetUsers[user.Source] = user.Target;
                cancellationToken.ThrowIfCancellationRequested();
            };

            return Task.CompletedTask;
        }

        private async Task MigrateAreasAsync( MigrationContext context, CancellationToken cancellationToken )
        {
            Logger.StartActivity($"Migrating {Settings.Areas.Count} Areas");
            using (var logger = Logger.BeginScope("MigrateAreas"))
            {
                //Enumerate the items to be migrated
                foreach (var item in Settings.Areas)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await MigrateAreaAsync(context, item, cancellationToken).ConfigureAwait(false);
                };
            };
            Logger.StopActivity($"Migrated Areas: {context.MigratedAreas.Succeeded()} Succeeded, {context.MigratedAreas.Skipped()} Skipped, {context.MigratedAreas.Errors()} Failed");
        }

        private async Task<MigratedArea> MigrateAreaAsync( MigrationContext context, AreaSettings area, CancellationToken cancellationToken )
        {
            Logger.StartActivity($"Migrating area '{area.SourcePath}' to '{area.ActualDestinationPath}'");

            var migratedArea = new MigratedArea()
            {
                SourcePath = new NodePath(area.SourcePath),
                TargetPath = new NodePath(area.ActualDestinationPath),
            };
            context.MigratedAreas.Add(migratedArea);

            try
            {
                using (var logger = Logger.BeginScope("MigrateArea"))
                {
                    //Does the area already exist
                    var existingArea = await context.TargetService.GetAreaAsync(migratedArea.TargetPath, false, cancellationToken).ConfigureAwait(false);
                    if (existingArea != null)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        Logger.Info($"Area '{migratedArea.TargetPath}' already exists, skipping");
                        migratedArea.TargetId = existingArea.Id;
                        migratedArea.Skipped = true;

                        return migratedArea;
                    };

                    //Create the destination area
                    existingArea = await context.TargetService.CreateAreaAsync(migratedArea.TargetPath, cancellationToken).ConfigureAwait(false);
                    migratedArea.TargetId = existingArea.Id;
                };

                Logger.StopActivity($"Migrated area '{migratedArea.SourcePath}' - Id {migratedArea.TargetId}");
            } catch (Exception e)
            {
                migratedArea.Error = e;
                Logger.Error(e);
            };

            return migratedArea;
        }

        private async Task MigrateIterationsAsync( MigrationContext context, CancellationToken cancellationToken )
        {
            Logger.StartActivity($"Migrating {Settings.Iterations.Count} Iterations");
            using (var logger = Logger.BeginScope("MigrateIterations"))
            {
                //Enumerate the items to be migrated
                foreach (var item in Settings.Iterations)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await MigrateIterationAsync(context, item, cancellationToken).ConfigureAwait(false);
                };
            };

            Logger.StopActivity($"Migrated Iterations: {context.MigratedIterations.Succeeded()} Succeeded, {context.MigratedIterations.Skipped()} Skipped, {context.MigratedIterations.Errors()} Failed");
        }

        private async Task<MigratedIteration> MigrateIterationAsync( MigrationContext context, IterationSettings iteration, CancellationToken cancellationToken )
        {
            Logger.StartActivity($"Migrating iteration '{iteration.SourcePath}' to {iteration.ActualDestinationPath}'");

            var migratedIteration = new MigratedIteration()
            {
                SourcePath = new NodePath(iteration.SourcePath),
                TargetPath = new NodePath(iteration.ActualDestinationPath),
            };

            context.MigratedIterations.Add(migratedIteration);

            try
            {
                using (var logger = Logger.BeginScope("MigrateIteration"))
                {
                    //Get the source iteration, if it exists
                    var sourceIteration = await context.SourceService.GetIterationAsync(migratedIteration.SourcePath, false, cancellationToken).ConfigureAwait(false);
                    if (sourceIteration != null)
                    {
                        var startDate = sourceIteration.GetStartDate();
                        var endDate = sourceIteration.GetFinishDate();
                        if (startDate.HasValue)
                            migratedIteration.Dates = new DateRange(startDate.Value, endDate.Value);
                    };

                    //Does the node already exist
                    var existingIteration = await context.TargetService.GetIterationAsync(migratedIteration.TargetPath, false, cancellationToken).ConfigureAwait(false);
                    if (existingIteration != null)
                    {
                        Logger.Info($"Iteration '{migratedIteration.TargetPath}' already exists, skipping");
                        migratedIteration.TargetId = existingIteration.Id;
                        migratedIteration.Skipped = true;

                        return migratedIteration;
                    };

                    //Create the destination area
                    existingIteration = await context.TargetService.CreateIterationAsync(migratedIteration.TargetPath, migratedIteration.Dates, cancellationToken).ConfigureAwait(false);
                    migratedIteration.TargetId = existingIteration.Id;
                };

                Logger.StopActivity($"Migrated iteration '{migratedIteration.TargetPath}' - Id {migratedIteration.TargetId}");
            } catch (Exception e)
            {
                migratedIteration.Error = e;
                Logger.Error(e);
            };

            return migratedIteration;
        }

        private async Task<WorkItem> MigrateWorkItemAsync( MigrationContext context, MigratedWorkItem item, CancellationToken cancellationToken )
        {
            using (var logger = Logger.BeginScope("MigrateWorkItem"))
            {
                //Get the work item
                var sourceItem = await context.SourceService.GetWorkItemAsync(item.SourceId, true, false, cancellationToken).ConfigureAwait(false);
                if (sourceItem == null)
                {
                    Logger.Warning($"Work item not found");
                    item.Skipped = true;
                    return null;
                };

                // Migrate the history
                item.Target = await MigrateWorkItemHistoryAsync(context, sourceItem, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                // Process the attachments - ignoring

                // Migrate the relationships 
                if (sourceItem.Relations.HasAny())
                    await MigrateWorkItemRelationsAsync(context, sourceItem.Relations, item.Target, cancellationToken).ConfigureAwait(false);

                // Migrate attachement files
                if (sourceItem.Relations.HasAny(r => r.IsAttachment()) && Settings.IncludeAttachmentFiles)
                    await MigrateWorkItemAttachmentAsync(context, sourceItem.Relations, item.Target, cancellationToken).ConfigureAwait(false);

                // Add note about migration into history
                await AddMigrationCommentAsync(context, sourceItem, item.Target, cancellationToken).ConfigureAwait(false);

                return item.Target;
            };
        }

        private async Task MigrateWorkItemsAsync( MigrationContext context, CancellationToken cancellationToken )
        {
            Logger.StartActivity($"Migrating work items");
            using (var logger = Logger.BeginScope("MigrateWorkItems"))
            {
                //Get the work items to migrate
                var queue = context.MigrationQueue = await GetWorkItemsAsync(context, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                //Now enumerate until we run out
                while (queue.Count > 0)
                {
                    var item = queue.Dequeue();
                    cancellationToken.ThrowIfCancellationRequested();

                    //If the item has already been migrated then don't do it again
                    if (context.HasMigratedWorkItem(item.SourceId))
                        continue;

                    try
                    {
                        context.MigratedWorkItems.Add(item);

                        Logger.StartActivity($"Migrating work item '{item.SourceId}' (Remaining = {queue.Count})");
                        await MigrateWorkItemAsync(context, item, cancellationToken).ConfigureAwait(false);
                    } catch (Exception e)
                    {
                        Logger.Error(e);
                        item.Error = e;
                    };
                };
            };

            Logger.StopActivity($"Work Items: {context.MigratedWorkItems.Succeeded()} Succeeded, {context.MigratedWorkItems.Errors()} Failed, {context.MigratedWorkItems.Count} Total");
        }

        private async Task MigrateWorkItemRelationsAsync( MigrationContext context, IEnumerable<WorkItemRelation> relations, WorkItem targetItem, CancellationToken cancellationToken )
        {
            var doc = new JsonPatchDocument();

            foreach (var relation in relations)
            {
                var isChild = relation.IsChild();
                var isParent = !isChild && relation.IsParent();
                var isRelated = !isChild && !isParent && relation.IsRelated();

                //Not a relation we care about                
                if (!isChild && !isParent && !isRelated)
                    continue;

                //Get the related item information
                var relatedId = relation.GetRelatedId();
                if (relatedId <= 0)
                    continue;

                var related = context.GetMigratedWorkItem(relatedId);

                if (isChild)
                {
                    //If the child has been migrated then add a child link to the related item
                    //else if the target isn't closed (or the override setting is set) then add the related item to migration list
                    if (related != null)
                    {
                        if (related.TargetId != 0)
                        {
                            Logger.Debug($"Adding Child link to {related.TargetId}");
                            doc.AddLink(WorkItemRelations.Child, related.TargetUrl);
                        } else
                            Logger.Warning($"Skipping Child link to {related.SourceId} because it failed to migrate");
                    } else if (!targetItem.IsClosed() || Settings.IncludeChildLinksOnClosed)
                    {
                        Logger.Debug($"Adding Child link {relatedId} to migration list");
                        AddToMigrationQueue(context.MigrationQueue, new MigratedWorkItem() { SourceId = relatedId });
                    };
                } else if (isParent)
                {
                    //If the parent has already been migrated then add a parent link to the related item
                    if (related != null)
                    {
                        if (related.TargetId != 0)
                        {
                            Logger.Debug($"Adding Parent link to {related.TargetId}");
                            doc.AddLink(WorkItemRelations.Parent, related.TargetUrl);
                        } else
                            Logger.Warning($"Skipping Parent link to {related.SourceId} because it failed to migrate");
                    } else if (!targetItem.IsClosed() || Settings.IncludeParentLinksOnClosed)
                    {
                        Logger.Debug($"Adding Parent link {relatedId} to migration list");
                        AddToMigrationQueue(context.MigrationQueue, new MigratedWorkItem() { SourceId = relatedId });
                    };
                } else if (isRelated)
                {
                    //If the related item has already been migrated then add a related link to it
                    //else if the target is not closed (or the override setting is set) then add the related item to the migration list
                    if (related != null)
                    {
                        if (related.TargetId != 0)
                        {
                            Logger.Debug($"Adding Related link to {related.TargetId}");
                            doc.AddLink(WorkItemRelations.Related, related.TargetUrl);
                        } else
                            Logger.Warning($"Skipping Related link to {related.SourceId} because it failed to migrate");
                    } else if (!targetItem.IsClosed() || Settings.IncludeRelatedLinksOnClosed)
                    {
                        Logger.Debug($"Adding Related link {relatedId} to migration list");
                        AddToMigrationQueue(context.MigrationQueue, new MigratedWorkItem() { SourceId = relatedId });
                    };
                }; //else ignore

                cancellationToken.ThrowIfCancellationRequested();
            };

            //If we have made any changes then update the target
            if (doc.Any())
                await context.TargetService.UpdateWorkItemUnrestrictedAsync(targetItem, doc, cancellationToken).ConfigureAwait(false);
        }

        private async Task MigrateWorkItemAttachmentAsync( MigrationContext context, IEnumerable<WorkItemRelation> relations, WorkItem targetItem, CancellationToken cancellationToken )
        {
            var doc = new JsonPatchDocument();

            var attachmentFiles = relations.Where(r => r.IsAttachment());

            foreach (var attachmentFile in attachmentFiles)
            {
                var guid = attachmentFile.GetRelatedGuid();

                if (guid == Guid.Empty)
                    continue;

                var fileName = attachmentFile.Attributes["name"].ToString();
                var attributs = attachmentFile.Attributes.Where(x => x.Key != "id").ToDictionary(x => x.Key, x => x.Value);

                var readOnlyAttachmentContent = await context.SourceService.TryCatchAsync(c => c.GetAttachmentContentAsync(guid, cancellationToken: cancellationToken)).ConfigureAwait(false);

                using (MemoryStream attachmentContent = new MemoryStream())
                {
                    readOnlyAttachmentContent.CopyTo(attachmentContent);
                    attachmentContent.Position = 0;

                    var attachmentReference = await context.TargetService.TryCatchAsync(c => c.CreateAttachmentAsync(attachmentContent, fileName: fileName, cancellationToken: cancellationToken)).ConfigureAwait(false);

                    Logger.Debug($"Adding Attachment file {fileName}");
                    doc.AddLink(new WorkItemRelation { Attributes = attributs, Rel = WorkItemRelations.Attachment, Url = attachmentReference.Url });
                }
            }

            //If we have made any changes then update the target
            if (doc.Any())
                await context.TargetService.UpdateWorkItemUnrestrictedAsync(targetItem, doc, cancellationToken).ConfigureAwait(false);
        }

        private async Task<WorkItem> MigrateWorkItemHistoryAsync( MigrationContext context, WorkItem sourceItem, CancellationToken cancellationToken )
        {
            WorkItem item = null;

            var entries = await context.SourceService.GetWorkItemHistoryAsync(sourceItem.Id.Value, cancellationToken).ConfigureAwait(false);

            //Now migrate the remaining history            
            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Logger.StartActivity($"Migrating history from '{entry.RevisedBy.Name}' on '{entry.RevisedDate}'");
                using (var logger = Logger.BeginScope("MigrateHistory"))
                {

                    var doc = await CreatePatchDocumentAsync(context, entry, cancellationToken).ConfigureAwait(false);

                    //Create or update the item                
                    if (item == null)
                    {
                        var teamProject = await context.GetTargetProjectAsync(cancellationToken).ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();

                        var workItemType = doc.FirstOrDefault(x => x.Path.Contains(WorkItemFields.WorkItemType)).Value.ToString() ?? sourceItem.GetWorkItemType();
                        item = await context.TargetService.CreateWorkItemUnrestrictedAsync(teamProject, doc, workItemType, cancellationToken).ConfigureAwait(false);
                    } else
                        item = await context.TargetService.UpdateWorkItemUnrestrictedAsync(item, doc, cancellationToken).ConfigureAwait(false);
                };
            };

            return item;
        }

        private async Task<IEnumerable<MigratedWorkItem>> QueryWorkItemsAsync( MigrationContext context, QuerySettings query, CancellationToken cancellationToken )
        {
            //Execute the query on the source server to get the WIs and their children
            var sourceProject = await context.GetSourceProjectAsync(cancellationToken).ConfigureAwait(false);

            var existingQuery = await context.SourceService.GetQueryAsync(sourceProject, query.Name, true, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            //Execute the query
            var workItems = await context.SourceService.QueryAsync(existingQuery.Id, cancellationToken).ConfigureAwait(false);
            Logger.Debug($"Query '{query.Name}' returned {workItems?.Count() ?? 0} items");

            return from wi in workItems
                   select new MigratedWorkItem() { SourceId = wi.Id };
        }

        private WorkItemTrackingSettings Settings { get; set; }
        #endregion
    }
}
