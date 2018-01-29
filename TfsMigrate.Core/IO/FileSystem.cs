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

using Fsmb.Apollo.IO;

namespace TfsMigrate.IO
{
    public static class FileSystem
    {
        public static string BuildPath ( string left, string right )
        {
            return PathExtensions.BuildPath(left, right);
        }

        public static string BuildPath ( string path1, string path2, string path3 )
        {
            return PathExtensions.BuildPath(path1, path2, path3);
        }

        public static string BuildPath ( string path1, string path2, string path3, params string[] other )
        {
            return PathExtensions.BuildPath(path1, path2, path3, other);
        }

        public static async Task ClearDirectoryAsync ( string path, CancellationToken cancellationToken )
        {
            if (!Directory.Exists(path))
                return;

            //Ensure there are no read only files
            await ClearReadOnlyAttributeAsync(path, true, cancellationToken).ConfigureAwait(false);

            //Delete all files
            await Task.Run(() => {
                foreach (var file in Directory.GetFiles(path))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    File.Delete(file);
                };
            }).ConfigureAwait(false);

            //Delete all child directories
            var children = Directory.GetDirectories(path);
            foreach (var child in children)
            {
                await RemoveDirectoryAsync(child, cancellationToken).ConfigureAwait(false);
            };
        }

        public static async Task ClearDirectoryAsync ( string path, IEnumerable<string> excludeFolders, CancellationToken cancellationToken )
        {
            if (!Directory.Exists(path))
                return;

            //Ensure there are no read only files
            await ClearReadOnlyAttributeAsync(path, true, cancellationToken).ConfigureAwait(false);

            //Delete all files
            await Task.Run(() => {
                foreach (var file in Directory.GetFiles(path))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    File.Delete(file);
                };
            }).ConfigureAwait(false);

            //Delete all child directories unless they are in the exclude list
            var children = from d in Directory.GetDirectories(path)
                           where !excludeFolders.Contains(Path.GetFileName(d))
                           select d;
            foreach (var child in children)
            {
                await RemoveDirectoryAsync(child, cancellationToken).ConfigureAwait(false);
            };
        }

        public static async Task ClearReadOnlyAttributeAsync ( string path, bool recurse, CancellationToken cancellationToken )
        {
            var dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists)
                return;

            var options = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            await Task.Run(() => {
                var files = from f in dirInfo.GetFiles("*.*", options)
                            where f.Attributes.HasFlag(FileAttributes.ReadOnly)
                            select f;

                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    file.Attributes &= ~FileAttributes.ReadOnly;
                };
            }).ConfigureAwait(false);
        }

        public static async Task CopyDirectoryAsync ( string targetPath, string sourcePath, CancellationToken cancellationToken )
        {
            await CreateDirectoryAsync(targetPath, false, cancellationToken).ConfigureAwait(false);

            var files = await Task.Run(() => Directory.GetFiles(sourcePath)).ConfigureAwait(false);
            
            await Task.Run(() => {
                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    File.Copy(file, BuildPath(targetPath, Path.GetFileName(file)), true);
                };
            });

            var children = await Task.Run(() => Directory.GetDirectories(sourcePath)).ConfigureAwait(false);
            foreach (var child in children)
            {
                await CopyDirectoryAsync(BuildPath(targetPath, Path.GetFileName(child)), child, cancellationToken).ConfigureAwait(false);
                Directory.Delete(child);
            };
        }
        
        public static async Task CreateDirectoryAsync ( string directoryPath, bool overwrite, CancellationToken cancellationToken )
        {
            if (Directory.Exists(directoryPath))
            {
                if (!overwrite)
                    return;

                await ClearDirectoryAsync(directoryPath, cancellationToken).ConfigureAwait(false);
            };

            cancellationToken.ThrowIfCancellationRequested();
            await Task.Run(() => Directory.CreateDirectory(directoryPath)).ConfigureAwait(false);
        }

        public static Task<long> ExtractZipAsync ( Stream stream, string targetPath, CancellationToken cancellationToken )
        {
            return Task.Run(() => {
                var count = 0L;
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    count = archive.Entries.Count();
                    archive.ExtractToDirectory(targetPath);
                };

                return count;
            });
        }

        public static async Task<IEnumerable<string>> GetFilesAsync ( string targetPath, bool recurse, CancellationToken cancellationToken )
        {
            var options = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            return await Task.Run(() => Directory.GetFiles(targetPath, "*.*", options), cancellationToken).ConfigureAwait(false);
        }

        public static Task RemoveFileAsync ( string basePath, string searchPattern, bool recursive, CancellationToken cancellationToken )
        {
            var options = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            return Task.Run(() => {
                var children = Directory.GetFiles(basePath, searchPattern, options);
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var child in children)
                {
                    File.Delete(child);
                };
            });
        }

        public static async Task RemoveFilesAsync ( string basePath, IEnumerable<string> searchPattern, bool recursive, CancellationToken cancellationToken )
        {
            foreach (var pattern in searchPattern)
                await RemoveFileAsync(basePath, pattern, recursive, cancellationToken).ConfigureAwait(false);
        }

        public static async Task RemoveDirectoryAsync ( string targetPath, CancellationToken cancellationToken )
        {
            //Want to use Directory.Delete but it will fail if there are any read only files so fix that first
            await ClearReadOnlyAttributeAsync(targetPath, true, cancellationToken).ConfigureAwait(false);
            
            await Task.Run(() => Directory.Delete(targetPath, true)).ConfigureAwait(false);
        }

        public static async Task RemoveDirectoriesAsync ( string basePath, string searchPattern, bool recursive, CancellationToken cancellationToken )
        {
            var options = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            var children = await Task.Run(() => Directory.GetDirectories(basePath, searchPattern, options), cancellationToken).ConfigureAwait(false);
            foreach (var child in children)
            {
                await RemoveDirectoryAsync(child, cancellationToken).ConfigureAwait(false);            
            };
        }

        public static async Task RemoveDirectoriesAsync ( string basePath, IEnumerable<string> searchPattern, bool recursive, CancellationToken cancellationToken )
        {
            foreach (var pattern in searchPattern)
                await RemoveDirectoriesAsync(basePath, pattern, recursive, cancellationToken).ConfigureAwait(false);
        }
    }
}
