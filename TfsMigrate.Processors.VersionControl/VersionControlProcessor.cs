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

using P3Net.Kraken.Text.Substitution;

using Microsoft.TeamFoundation.SourceControl.WebApi;

using TfsMigrate.Data;
using TfsMigrate.Diagnostics;
using TfsMigrate.IO;
using TfsMigrate.Processors.VersionControl.Data;
using TfsMigrate.Processors.VersionControl.Git;
using TfsMigrate.Processors.VersionControl.Tfvc;
using TfsMigrate.Tfs;

namespace TfsMigrate.Processors.VersionControl
{    
    public class VersionControlProcessor : Processor
    {
        protected override async Task InitializeCoreAsync ( CancellationToken cancellationToken )
        {
            await base.InitializeCoreAsync(cancellationToken).ConfigureAwait(false);

            Settings = await Host.GetSettingsAsync<VersionControlSettings>("VersionControl", cancellationToken).ConfigureAwait(false) ?? new VersionControlSettings();
        }

        protected override async Task RunCoreAsync ( CancellationToken cancellationToken )
        {
            var context = new MigrationContext() {
                SourceServer = new TfsServer(Host.Settings.SourceCollectionUrl, Host.Settings.SourceAccessToken),
                TargetServer = new TfsServer(Host.Settings.TargetCollectionUrl, Host.Settings.TargetAccessToken),
                GitCommand = new GitCommand(Settings.GitCommandLine),
                OutputPath = FileSystem.BuildPath(Host.Settings.OutputPath, "repos"),
            };

            //Clean the output directory
            Logger.Info($"Cleaning output path '{context.OutputPath}'");
            await FileSystem.CreateDirectoryAsync(context.OutputPath, true, cancellationToken).ConfigureAwait(false);

            await MigrateProjectsAsync(context, Settings.Projects, cancellationToken).ConfigureAwait(false);
        }

        #region Private Members        

        private async Task MigrateProjectsAsync ( MigrationContext context, IEnumerable<ProjectSettings> projectsToMigrate, CancellationToken cancellationToken )
        {            
            context.StartProfiling();

            Logger.StartActivity($"Migrating {projectsToMigrate.Count()} projects");
            using (var logger = Logger.BeginScope("MigrateProjects"))
            {
                foreach (var projectToMigrate in projectsToMigrate)
                {
                    context.Projects.Add(await MigrateProjectAsync(context, projectToMigrate, cancellationToken).ConfigureAwait(false));
                }
            };

            var totalTime = context.StopProfiling();
            Logger.StopActivity($"Projects: {context.Projects.Succeeded()} Succeeded, {context.Projects.Errors()} Failed, Elapsed Time = {totalTime}");
        }

        private async Task<MigratedProject> MigrateProjectAsync ( MigrationContext context, ProjectSettings projectToMigrate, CancellationToken cancellationToken )
        {            
            Logger.StartActivity($"Migrating project '{projectToMigrate.SourcePath}'");

            var migratedProject = new MigratedProject() {
                SourcePath = projectToMigrate.SourcePath,
                LocalFullPath = FileSystem.BuildPath(context.OutputPath, projectToMigrate.DestinationPath),

                DestinationPath = projectToMigrate.DestinationPath,
                DestinationProjectName = projectToMigrate.DestinationProject
            };
                           
            migratedProject.StartProfiling();
            try
            {
                using (var logger = Logger.BeginScope("MigrateProject"))
                {
                    // Create the target repo
                    migratedProject.DestinationRepo = await CreateGitRepoAsync(context, migratedProject, cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();

                    if (projectToMigrate.HasBranches)
                    {
                        // Find the baseline branch for this project, if any
                        await FindBaselineBranchAsync(context, migratedProject, cancellationToken).ConfigureAwait(false);
                        if (!String.IsNullOrEmpty(migratedProject.BaselinePath))
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            // Download the baseline
                            await DownloadFolderAsync(context.SourceService, migratedProject.BaselinePath, migratedProject.LocalFullPath, cancellationToken).ConfigureAwait(false);
                            cancellationToken.ThrowIfCancellationRequested();

                            // Clean the folder
                            var fileCount = await CleanFolderAsync(migratedProject.LocalFullPath, cancellationToken).ConfigureAwait(false);
                            cancellationToken.ThrowIfCancellationRequested();

                            // It is possible that the baseline branch is actually empty in which case we'll
                            // get an error when trying to commit so skip this if there are no files
                            if (fileCount > 0)
                            {
                                // Commit the changes to the master branch
                                var msg = $"Committing baseline version from TFS - {migratedProject.BaselinePath}";
                                await CommitRepoAsync(migratedProject, context.GitCommand, msg, cancellationToken).ConfigureAwait(false);
                                cancellationToken.ThrowIfCancellationRequested();

                                // Create a release for this version so it can be found later if there is a release
                                if (migratedProject.Version != null)
                                {
                                    await CreateReleaseBranchAsync(migratedProject, context.GitCommand, cancellationToken).ConfigureAwait(false);

                                    //Switch back to master because we won't be doing anything else with this branch
                                    Logger.Debug($"Changing to branch '{Settings.GitMasterBranch}'");
                                    await context.GitCommand.CheckOutBranchAsync(migratedProject.LocalFullPath, Settings.GitMasterBranch, false, cancellationToken).ConfigureAwait(false);                                    
                                };
                            } else
                                Logger.Warning($"No files in baseline '{migratedProject.BaselinePath}', skipping commit of master branch");

                            cancellationToken.ThrowIfCancellationRequested();
                        };
                    };

                    // Download the latest version of the code to master
                    {
                        migratedProject.DevelopmentPath = projectToMigrate.HasBranches ? ItemPath.BuildPath(migratedProject.SourcePath, Settings.DevelopmentBranch) : migratedProject.SourcePath;

                        // We need to be able to tell what stuff was deleted so wipe the directory structure and start over, except the .git folder
                        await FileSystem.ClearDirectoryAsync(migratedProject.LocalFullPath, new[] { ".git" }, cancellationToken).ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();

                        // Download the dev branch
                        await DownloadFolderAsync(context.SourceService, migratedProject.DevelopmentPath, migratedProject.LocalFullPath, cancellationToken).ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();

                        // Clean the folder
                        await CleanFolderAsync(migratedProject.LocalFullPath, cancellationToken).ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();

                        // Copy the template files over any existing files
                        await CopyTemplateAsync(migratedProject.LocalFullPath, Settings.TemplatePath, cancellationToken).ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();

                        // Update the metadata file, if any
                        if (!String.IsNullOrEmpty(Settings.MetadataFile))
                        {
                            var metadataFile = FileSystem.BuildPath(migratedProject.LocalFullPath, Settings.MetadataFile);
                            await UpdateMetadataFileAsync(metadataFile, migratedProject, cancellationToken).ConfigureAwait(false);
                            cancellationToken.ThrowIfCancellationRequested();
                        };

                        // Commit the changes
                        var msg = $"Committing latest version from TFS - {migratedProject.DevelopmentPath}";
                        await CommitRepoAsync(migratedProject, context.GitCommand, msg, cancellationToken).ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();
                    };

                    //Clean up the structure if set
                    if (Settings.CleanAfterCommit)
                    {
                        await FileSystem.RemoveDirectoryAsync(migratedProject.LocalFullPath, cancellationToken).ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();
                    };
                };

                var totalTime = migratedProject.StopProfiling();
                Logger.StopActivity($"Migrated project '{projectToMigrate.SourcePath}' in {totalTime}");
            } catch (Exception e)
            {
                migratedProject.Error = e;
                Logger.Error(e);
            };

            return migratedProject;
        }

        private Task<string> BuildBranchNameAsync ( MigratedProject project, string branch, CancellationToken cancellationToken )
        {
            return Task.Run(() => {
                var engine = new TextSubstitutionEngine("{", "}");
                engine.Rules.Add(new SimpleTextSubstitutionRule("major", project.Version?.Major.ToString() ?? "0"));
                engine.Rules.Add(new SimpleTextSubstitutionRule("minor", project.Version?.Minor.ToString() ?? "0"));
                engine.Rules.Add(new SimpleTextSubstitutionRule("build", project.Version?.Build.ToString() ?? "0"));
                engine.Rules.Add(new SimpleTextSubstitutionRule("revision", project.Version?.Revision.ToString() ?? "0"));
                engine.Rules.Add(new SimpleTextSubstitutionRule("version", project.Version?.ToString() ?? "0.0.0.0"));

                engine.Rules.Add(new SimpleTextSubstitutionRule("yyyy", DateTime.Now.ToString("yyyy")));
                engine.Rules.Add(new SimpleTextSubstitutionRule("MM", DateTime.Now.ToString("MM")));
                engine.Rules.Add(new SimpleTextSubstitutionRule("dd", DateTime.Now.ToString("dd")));
                engine.Rules.Add(new SimpleTextSubstitutionRule("MMM", DateTime.Now.ToString("MMM")));
                engine.Rules.Add(new SimpleTextSubstitutionRule("yy", DateTime.Now.ToString("yy")));

                return engine.Process(branch);
            }, cancellationToken);
        }
        
        private async Task<long> CleanFolderAsync ( string folderPath, CancellationToken cancellationToken )
        {
            Logger.Info($"Cleaning folder '{folderPath}'");
            
            await FileSystem.RemoveDirectoriesAsync(folderPath, Settings.CleanFolders, true, cancellationToken).ConfigureAwait(false);
            await FileSystem.RemoveFilesAsync(folderPath, Settings.CleanFiles, true, cancellationToken).ConfigureAwait(false);

            var files = await FileSystem.GetFilesAsync(folderPath, true, cancellationToken).ConfigureAwait(false);

            //Remove any files not under the .git folder
            files = from f in files
                    where !f.Contains(@"\.git\")
                    select f;

            return files?.Count() ?? 0;
        }
                
        private async Task CopyTemplateAsync ( string outputPath, string templatePath, CancellationToken cancellationToken)
        {
            Logger.Info($"Copying template files to '{outputPath}'");
            
            if (String.IsNullOrEmpty(templatePath))
                return;

            if (!Directory.Exists(templatePath))
            {
                Logger.Warning($"Template path '{templatePath}' not found");
                return;
            };

            await FileSystem.CopyDirectoryAsync(outputPath, templatePath, cancellationToken).ConfigureAwait(false);            
        }

        private async Task<GitRepository> CreateGitRepoAsync ( MigrationContext context, MigratedProject project, CancellationToken cancellationToken )
        {
            var targetProject = await context.TargetServer.FindProjectAsync(project.DestinationProjectName, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
                                        
            var repo = new GitRepository() {
                ProjectReference = targetProject,
                Name = project.DestinationPath
            };

            //Get the repo, if it exists    
            var existingRepo = await context.TargetService.FindRepositoryAsync(targetProject, repo.Name, cancellationToken).ConfigureAwait(false);

            Logger.Debug($"Creating Git repo '{project.DestinationFullPath}'");

            //Delete the repo if it exists
            if (existingRepo != null)
            {
                Logger.Debug($"Deleting repo '{project.DestinationFullPath}'");
                await context.TargetService.DeleteRepositoryAsync(existingRepo, cancellationToken).ConfigureAwait(false);
            };

            //Clone the repo locally
            Logger.Debug($"Initializing repo '{project.DestinationFullPath}' to '{project.LocalFullPath}'");
            return await CreateLocalRepositoryAsync(context, repo, context.OutputPath, cancellationToken).ConfigureAwait(false);         
        }

        private async Task CommitRepoAsync ( MigratedProject project, GitCommand command, string message, CancellationToken cancellationToken )
        {
            Logger.Info($"Committing repo '{project.DestinationFullPath}' - {message}");
            
            //Have to use Git for this
            await command.CommitAndPushChangesAsync(project.LocalFullPath, Settings.GitMasterBranch, message, cancellationToken).ConfigureAwait(false);            
        }

        private async Task CreateReleaseBranchAsync ( MigratedProject project, GitCommand command, CancellationToken cancellationToken )
        {
            Logger.Info($"Creating branch for release for '{project.DestinationFullPath}");
            
            var branchName = await BuildBranchNameAsync(project, Settings.GitReleaseBranch, cancellationToken).ConfigureAwait(false);
            Logger.Info($"Branch = {branchName}");

            //Have to use Git for this
            await command.CheckOutBranchAsync(project.LocalFullPath, branchName, true, cancellationToken).ConfigureAwait(false);

            //Have to use Git for this
            //We have already pulled over the files so we can just commit to snapshot the release branch
            await command.CommitAndPushChangesAsync(project.LocalFullPath, branchName, $"Import of release {project.Version}", cancellationToken).ConfigureAwait(false);            
        }
     
        private async Task<GitRepository> CreateLocalRepositoryAsync ( MigrationContext context, GitRepository repo, string basePath, CancellationToken cancellationToken )
        {
            Logger.Info($"Creating repository at '{basePath}' for repo '{repo.Name}'");

            await context.GitCommand.InitializeRepositoryAsync(basePath, repo.Name, cancellationToken).ConfigureAwait(false);

            //Create the repo on the server if it doesn't exist yet
            if (repo.Id == Guid.Empty)
            {
                Logger.Debug($"Creating repo '{repo.ProjectReference.Name}/{repo.Name}' on server");
                repo = await context.TargetService.CreateRepositoryAsync(repo, cancellationToken: cancellationToken).ConfigureAwait(false);
            };

            //Connect the local repo with the remote one
            var repoPath = FileSystem.BuildPath(basePath, repo.Name);
            await context.GitCommand.SetRepositoryOriginAsync(repoPath, repo.RemoteUrl, cancellationToken).ConfigureAwait(false);

            //Linefeeds generate warnings on Windows so go ahead and configure the repo to use the Windows-appropriate settings
            await context.GitCommand.ConfigureAsync(repoPath, "core.autocrlf false", false, cancellationToken).ConfigureAwait(false);
            return repo;
        }

        private async Task DownloadFolderAsync ( TfvcHttpClient client, string remotePath, string localPath, CancellationToken cancellationToken )
        {
            Logger.Info($"Downloading files from '{remotePath}' to '{localPath}'");
            
            await client.GetAllAsync(remotePath, localPath, cancellationToken).ConfigureAwait(false);

            //Read only files cause us problems so clear the flag now
            await FileSystem.ClearReadOnlyAttributeAsync(localPath, true, cancellationToken).ConfigureAwait(false);            
        }        

        private async Task FindBaselineBranchAsync ( MigrationContext context, MigratedProject project, CancellationToken cancellationToken )
        {
            Logger.Info($"Looking for releases branch for '{project.SourcePath}'");
            
            //Look for the releases folder
            (string itemPath, Version version) = await context.SourceService.FindLatestVersionAsync(ItemPath.BuildPath(project.SourcePath, Settings.ReleaseBranch), cancellationToken).ConfigureAwait(false);

            //If there is a latest release then we'll use it as baseline otherwise download the baseline branch                    
            if (String.IsNullOrEmpty(itemPath))
            {
                Logger.Debug($"No releases found, looking for base branch from '{project.SourcePath}'");

                var baseline = await context.SourceService.FindItemAsync(ItemPath.BuildPath(project.SourcePath, Settings.BaselineBranch), false, cancellationToken).ConfigureAwait(false);
                if (baseline != null)
                {
                    itemPath = baseline.Path;
                };
            };

            if (!String.IsNullOrEmpty(itemPath))
            {
                project.BaselinePath = itemPath;
                project.Version = version;
            };

            Logger.Info($"Found branch for '{project.SourcePath}' - baseline Path = {itemPath}, Version = {version}");            
        }
        
        private async Task UpdateMetadataFileAsync ( string metadataFile, MigratedProject project, CancellationToken cancellationToken )
        {
            Logger.Info($"Updating metadata file '{metadataFile}'");
            
            //Open or create the file
            if (!File.Exists(metadataFile))
            {
                Logger.Warning($"No metadata file found '{metadataFile}");
                return;
            };

            await Task.Run(() => {
                var engine = new TextSubstitutionEngine("{", "}");
                engine.Rules.Add(new ObjectTextSubstitutionRule<MigratedProject>(project));
                engine.Rules.Add(new SimpleTextSubstitutionRule("Date", DateTime.Now.ToString()));
                engine.Rules.Add(new SimpleTextSubstitutionRule("TfsCollectionUrl", Host.Settings.SourceCollectionUrl));

                var text = File.ReadAllText(metadataFile);
                text = engine.Process(text);
                File.WriteAllText(metadataFile, text);
            }, cancellationToken).ConfigureAwait(false);
            
        }

        private VersionControlSettings Settings { get; set; }
        #endregion
    }
}
