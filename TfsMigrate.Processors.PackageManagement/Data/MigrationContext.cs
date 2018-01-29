/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;

using TfsMigrate.Processors.PackageManagement.Packaging;
using TfsMigrate.Processors.PackageManagement.Packaging.NuGet;
using TfsMigrate.Tfs;

namespace TfsMigrate.Processors.PackageManagement.Data
{
    class MigrationContext
    {
        public string OutputPath { get; set; }

        public List<MigratedPackage> Packages { get; } = new List<MigratedPackage>();

        public TfsServer SourceServer { get; set; }

        public PackagingHttpClient SourceService => SourceServer.GetClient<PackagingHttpClient>();

        public TfsServer TargetServer { get; set; }

        public PackagingHttpClient TargetService => TargetServer.GetClient<PackagingHttpClient>();

        public NuGetCommand Command { get; set; }
    }
}
