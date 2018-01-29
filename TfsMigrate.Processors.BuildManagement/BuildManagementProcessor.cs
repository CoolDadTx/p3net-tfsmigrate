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
using Microsoft.TeamFoundation.Build.WebApi;
using TfsMigrate.Data;
using TfsMigrate.Diagnostics;
using TfsMigrate.Processors.BuildManagement.Data;
using TfsMigrate.Processors.BuildManagement.Tfs;
using TfsMigrate.Tfs;

namespace TfsMigrate.Processors.BuildManagement
{
    public class BuildManagementProcessor : Processor
    {
        protected override async Task InitializeCoreAsync ( CancellationToken cancellationToken )
        {
            await base.InitializeCoreAsync(cancellationToken).ConfigureAwait(false);

            Settings = await Host.GetSettingsAsync<BuildManagementSettings>("BuildManagement", cancellationToken).ConfigureAwait(false) ?? new BuildManagementSettings();
        }

        protected override async Task RunCoreAsync ( CancellationToken cancellationToken )
        {
            var context = new MigrationContext() {
                SourceServer = new TfsServer(Host.Settings.SourceCollectionUrl, Host.Settings.SourceAccessToken),
                SourceProjectName = Host.Settings.SourceProject,

                TargetServer = new TfsServer(Host.Settings.TargetCollectionUrl, Host.Settings.TargetAccessToken),
                TargetProjectName = Host.Settings.TargetProject
            };
            
            //Templates
            if (Settings.CopyTemplates)
            {
                await MigrateTemplatesAsync(context, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            };

            //Definitions
            await MigrateBuildsAsync(context, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

        #region Private Members
        
        private async Task MigrateBuildsAsync ( MigrationContext context, CancellationToken cancellationToken )
        {
            Logger.StartActivity($"Migrating Build Definitions");
            using (var logger = Logger.BeginScope("MigrateBuilds"))
            {
                //Get the list of build definitions to migrate
                var definitions = await context.SourceService.GetFullDefinitionsAsync(context.SourceProject, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                
                //Enumerate the items to be migrated                
                foreach (var definition in definitions)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    context.Definitions.Add(await MigrateBuildAsync(context, definition, cancellationToken).ConfigureAwait(false));
                };
            };
            Logger.StopActivity($"Migrated Build Definitions: {context.Definitions.Succeeded()} Succeeded, {context.Definitions.Skipped()} Skipped, {context.Definitions.Errors()} Failed");
        }

        private async Task<MigratedBuildDefinition> MigrateBuildAsync ( MigrationContext context, BuildDefinition definition, CancellationToken cancellationToken )
        {
            Logger.StartActivity($"Migrating build definition '{definition.Name}'");

            var migratedItem = new MigratedBuildDefinition(definition);

            using (var logger = Logger.BeginScope("MigrateBuildDefinition"))
            {
                try
                {
                    //Is this blacklisted
                    if (Settings.ExcludeDefinitions.Contains(definition.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        Logger.Warning($"Definition is excluded, skipping");
                        migratedItem.Skipped = true;
                        return migratedItem;
                    };

                    //Does the definition already exist                    
                    var existingItem = await context.TargetService.GetDefinitionAsync(context.TargetProject, migratedItem.Name, cancellationToken).ConfigureAwait(false);
                    if (existingItem != null && Settings.Overwrite)
                    {
                        Logger.Info($"Build definition '{migratedItem.Name}' already exists and overwrite is true, removing definition");

                        //Remove definition
                        await context.TargetService.DeleteDefinitionAsync(existingItem, cancellationToken).ConfigureAwait(false);
                        Logger.Debug($"Deleted build definition '{existingItem.Path}'");

                        existingItem = null;
                    };

                    //Need to clean up the definition so we can insert it
                    PrepareDefinition(context, definition);
                    
                    definition = await context.TargetService.AddDefinitionAsync(definition, cancellationToken).ConfigureAwait(false);
                    Logger.Info($"Created build definition '{definition.Path}' with Id '{definition.Id}'");
                } catch (Exception e)
                {
                    migratedItem.Error = e;
                    Logger.Error(e);
                };
            };

            return migratedItem;
        }

        private async Task MigrateTemplatesAsync ( MigrationContext context, CancellationToken cancellationToken )
        {
            Logger.StartActivity($"Migrating Build Templates");
            using (var logger = Logger.BeginScope("MigrateBuildTemplates"))
            {
                var sourceTemplates = await context.SourceService.GetBuildTemplatesAsync(context.SourceProject, cancellationToken).ConfigureAwait(false);
                var destinationTemplates = await context.TargetService.GetBuildTemplatesAsync(context.TargetProject, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                //Only want to migrate the custom ones
                sourceTemplates = sourceTemplates.Where(t => t.CanDelete);

                //Enumerate the items to be migrated   
                foreach (var template in sourceTemplates)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    context.Templates.Add(await MigrateTemplateAsync(context, template, destinationTemplates, cancellationToken).ConfigureAwait(false));
                };
            };
            Logger.StopActivity($"Migrated Build Definitions: {context.Definitions.Succeeded()} Succeeded, {context.Definitions.Skipped()} Skipped, {context.Definitions.Errors()} Failed");
        }

        private async Task<MigratedBuildTemplate> MigrateTemplateAsync ( MigrationContext context, BuildDefinitionTemplate template, IEnumerable<BuildDefinitionTemplate> existingTemplates, CancellationToken cancellationToken )
        {
            Logger.StartActivity($"Migrating build template '{template.Name}'");

            var migratedItem = new MigratedBuildTemplate(template);

            using (var logger = Logger.BeginScope("MigrateBuildTemplate"))
            {
                try
                {
                    //Does the template already exist                    
                    var existingItem = existingTemplates.FirstOrDefault(t => String.Compare(t.Name, template.Name, true) == 0);
                    if (existingItem != null)
                    {
                        Logger.Info($"Build template '{migratedItem.Name}' already exists, skipping");
                        migratedItem.Skipped = true;
                        return migratedItem;
                    };
                    
                    template = await context.TargetService.AddTemplateAsync(context.TargetProject, template, cancellationToken).ConfigureAwait(false);
                    Logger.Debug($"Created build template '{template.Name}' with Id '{template.Id}'");
                } catch (Exception e)
                {
                    migratedItem.Error = e;
                    Logger.Error(e);
                };
            };

            return migratedItem;
        }

        private void PrepareDefinition ( MigrationContext context, BuildDefinition definition )
        {
            //Reset the core properties            
            definition.Project = context.TargetProject;
            definition.Id = 0;
            definition.Queue = null;
            
            //Map the task groups
            foreach (var step in definition.Steps)
            {
                var mapping = Settings.TaskGroups.FirstOrDefault(g => g.SourceGroupId == step.TaskDefinition.Id);
                if (mapping != null)
                    step.TaskDefinition.Id = mapping.TargetGroupId;
            };

            //Set the target agent queue
            definition.Queue = new AgentPoolQueue() { Name = Settings.TargetAgentQueue };

            //Should we add a trigger for the build

            //var trigger = new ScheduleTrigger();
            //var schedule = new Schedule() {
            //    StartHours = 3,
            //    DaysToBuild = ScheduleDays.Monday | ScheduleDays.Tuesday | ScheduleDays.Wednesday | ScheduleDays.Thursday | ScheduleDays.Friday
            //};
            //schedule.BranchFilters.Add();
            //trigger.Schedules.Add(schedule);

            //definition.Triggers.Clear();            
        }

        private BuildManagementSettings Settings { get; set; }
        #endregion
    }
}
