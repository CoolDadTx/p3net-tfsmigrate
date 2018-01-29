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
using Microsoft.TeamFoundation.SourceControl.WebApi;

using TfsMigrate.Diagnostics;

namespace TfsMigrate.Processors.VersionControl.Git
{
    static class GitClientExtensions
    {     
        //public static async Task CloneRepositoryAsync ( this GitHttpClient source, GitRepository repo, string outputPath, CancellationToken cancellationToken )
        //{
        //    Logger.Debug($"Cloning repository: Name = '{repo.Name}'");
            
        //    //Create the folder if it does not exist yet
        //    await FileSystem.CreateDirectoryAsync(outputPath, true, cancellationToken).ConfigureAwait(false);

        //    try
        //    {
        //        using (var stream = await source.GetItemZipAsync(repo.Id, "/", cancellationToken: cancellationToken).ConfigureAwait(false))
        //        {
        //            Logger.Debug($"Extracting repo to '{outputPath}");
        //            var fileCount = await FileSystem.ExtractZipAsync(stream, outputPath, cancellationToken: cancellationToken).ConfigureAwait(false);
        //            Logger.Debug($"Extracted {fileCount} files");
        //        };
        //    } catch (VssServiceResponseException e)
        //    {
        //        //Ignore warnings
        //        if (e.LogLevel != EventLogEntryType.Warning)
        //            throw;

        //        Logger.Warning($"Cloning repository caused warning: Name = '{repo.Name}', Message = '{e.Message}");
        //    };            
        //}
        
        public static async Task DeleteRepositoryAsync ( this GitHttpClient source, GitRepository repo, CancellationToken cancellationToken )
        {            
            Logger.Debug($"Deleting Repository: Name = '{repo.Name}'");

            await source.DeleteRepositoryAsync(repo.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public static async Task<GitRepository> FindRepositoryAsync ( this GitHttpClient source, TeamProject project, string repoName, CancellationToken cancellationToken )
        {            
            GitRepository repo = null;

            Logger.Debug($"Finding repository: Project = '{project.Name}', Name = '{repoName}'");
            
            try
            {
                repo = await source.GetRepositoryAsync(project.Id, repoName, cancellationToken: cancellationToken).ConfigureAwait(false);
            } catch
            { /* Ignore missing repos */ };

            if (repo != null)
                Logger.Debug($"Repository '{project.Name}/{repoName}' Id = {repo.Id}");
            else
                Logger.Debug($"Repository '{project.Name}/{repoName}' not found");

            return repo;            
        }
    }
}
