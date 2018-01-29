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

namespace TfsMigrate.Processors.PackageManagement.Packaging
{
    static class PackagingClientExtensions
    {                
        public static async Task SavePackageAsync ( this PackagingHttpClient source, string feed, string packageName, string packageVersion, string outputPath, CancellationToken cancellationToken )
        {
            using (var stream = await source.DownloadPackageAsync(feed, packageName, packageVersion, cancellationToken).ConfigureAwait(false))
            {
                using (var output = File.OpenWrite(outputPath))
                {
                    stream.CopyTo(output);
                };
            };
        }
    }
}
