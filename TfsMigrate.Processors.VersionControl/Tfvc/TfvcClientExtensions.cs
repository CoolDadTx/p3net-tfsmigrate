/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;

using TfsMigrate.Diagnostics;
using TfsMigrate.IO;
using TfsMigrate.Tfs;

namespace TfsMigrate.Processors.VersionControl.Tfvc
{
    static class TfvcClientExtensions
    {
        public static async Task<TfvcItem> FindItemAsync ( this TfvcHttpClient source, string itemPath, bool throwIfNotFound, CancellationToken cancellationToken )
        {
            TfvcItem item = null;
            try
            {
                item = await source.TryCatchAsync(c => c.GetItemAsync(itemPath, cancellationToken: cancellationToken)).ConfigureAwait(false);
            } catch (VssServiceException e)
            {
                if (!e.IsWarning())
                    throw e;

                /* Ignore because we cannot get the message code from an "item not found" error */
            };

            if (item == null && throwIfNotFound)
                throw new Exception("Item not found.");

            return item;
        }

        public static async Task<(string itemPath, Version version)> FindLatestVersionAsync ( this TfvcHttpClient source, string itemPath, CancellationToken cancellationToken )
        {
            //Find the releases branch
            var releasesBranch = await source.FindItemAsync(itemPath, false, cancellationToken).ConfigureAwait(false);
            if (releasesBranch == null)
                return (null, null);

            //Enumerate the child folders ordered by "version"
            var projectName = ItemPath.GetProject(itemPath);
            var children = await source.GetFoldersAsync(projectName, releasesBranch.Path, false, cancellationToken).ConfigureAwait(false);

            return (from c in children
                    let v = TryParseVersion(c.GetName())
                    where v != null
                    orderby v descending
                    select (c.Path, v)).FirstOrDefault();
        }

        public static async Task GetAllAsync ( this TfvcHttpClient source, string itemPath, string targetPath, CancellationToken cancellationToken )
        {
            Logger.Debug($"Downloading '{itemPath}'");

            var request = new TfvcItemRequestData()
            {
                ItemDescriptors = new[]
                {
                    new TfvcItemDescriptor() { Path = itemPath, RecursionLevel = VersionControlRecursionType.Full }
                }
            };

            //Download the items
            using (var stream = await source.GetItemsBatchZipAsync(request))
            {
                Logger.Debug($"Extracting '{itemPath}' to '{targetPath}'");
                var fileCount = await ExtractZipAsync(stream, targetPath, itemPath, cancellationToken).ConfigureAwait(false);
                Logger.Debug($"Extracted {fileCount} files to '{targetPath}'");
            };
        }
        
        public static async Task<List<TfvcItem>> GetFoldersAsync ( this TfvcHttpClient source, string projectName, string basePath, bool recursive, CancellationToken cancellationToken )
        {
            Logger.Debug($"Getting folders for '{basePath}', Recursion = {recursive}...");

            var level = recursive ? VersionControlRecursionType.Full : VersionControlRecursionType.None;
            var items = await source.GetItemsAsync(projectName, scopePath: basePath, recursionLevel: level, includeLinks: false, cancellationToken: cancellationToken).ConfigureAwait(false) ?? new List<TfvcItem>();

            return items.Where(i => i.IsFolder).ToList();
        }
        
        #region Private Members

        private static async Task<long> ExtractZipAsync ( Stream stream, string targetPath, string itemPath, CancellationToken cancellationToken )
        {
            var fileCount = 0L;
            using (var archive = new ZipArchive(stream))
            {
                foreach (var entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    //The entry contains the full path of the items from the root of TFS so skip everything up to the item path            
                    var localFileName = StripItemPath(entry.FullName, itemPath);
                    if (String.IsNullOrEmpty(localFileName))
                        continue;

                    //This could either be a file or folder
                    if (entry.Length > 0)
                    {
                        var localTarget = FileSystem.BuildPath(targetPath, localFileName);

                        await FileSystem.CreateDirectoryAsync(Path.GetDirectoryName(localTarget), false, cancellationToken).ConfigureAwait(false);

                        ++fileCount;
                        Logger.Debug($"Extracting '{localFileName}' to '{localTarget}'");
                        await Task.Run(() => entry.ExtractToFile(localTarget, true)).ConfigureAwait(false);
                    };
                };
            };

            return fileCount;
        }        

        private static string StripItemPath ( string targetPath, string itemPath )
        {
            if (targetPath.StartsWith(itemPath, StringComparison.OrdinalIgnoreCase))
                return targetPath.Substring(itemPath.Length + 1);

            return targetPath;
        }

        private static Version TryParseVersion ( string input )
        {
            input = input.Trim('v', 'V');

            if (Version.TryParse(input, out var ver))
                return ver;

            return null;
        }
        #endregion
    }
}
