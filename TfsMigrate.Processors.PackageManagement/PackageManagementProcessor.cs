/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using TfsMigrate.Data;
using TfsMigrate.Diagnostics;
using TfsMigrate.IO;
using TfsMigrate.Processors.PackageManagement.Data;
using TfsMigrate.Processors.PackageManagement.Packaging;
using TfsMigrate.Processors.PackageManagement.Packaging.NuGet;
using TfsMigrate.Tfs;

namespace TfsMigrate.Processors.PackageManagement
{
    public class PackageManagementProcessor : Processor
    {
        protected override async Task InitializeCoreAsync ( CancellationToken cancellationToken )
        {
            await base.InitializeCoreAsync(cancellationToken).ConfigureAwait(false);

            Settings = await Host.GetSettingsAsync<PackageManagementSettings>("PackageManagement", cancellationToken).ConfigureAwait(false) ?? new PackageManagementSettings();
        }

        protected override async Task RunCoreAsync ( CancellationToken cancellationToken )
        {
            var context = new MigrationContext() {
                SourceServer = new TfsServer(Settings.SourceUrl, Host.Settings.SourceAccessToken),
                TargetServer = new TfsServer(Settings.TargetUrl, Host.Settings.TargetAccessToken),
                Command = new NuGetCommand(Settings.NuGetCommandLine),
                OutputPath = FileSystem.BuildPath(Host.Settings.OutputPath, "packages")
            };

            //Ensure output exists
            await FileSystem.CreateDirectoryAsync(context.OutputPath, true, cancellationToken).ConfigureAwait(false);

            //Migrate
            await MigratePackagesAsync(context, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

        #region Private Members
        
        private async Task MigratePackagesAsync ( MigrationContext context, CancellationToken cancellationToken )
        {
            Logger.StartActivity($"Migrating Packages");
            using (var logger = Logger.BeginScope("MigratePackages"))
            {                
                //Get the list of packages available in the source
                var packages = await context.SourceService.GetPackagesAsync(Settings.SourceFeed, false, Settings.IncludeDelistedVersions, cancellationToken).ConfigureAwait(false);
                Logger.Debug($"Found {packages.Count} packages from feed '{Settings.SourceFeed}'");

                //Enumerate the items to be migrated                
                foreach (var package in packages)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    context.Packages.Add(await MigratePackageAsync(context, package, cancellationToken).ConfigureAwait(false));                                           
                };
            };
            Logger.StopActivity($"Migrated Packages: {context.Packages.Succeeded()} Succeeded, {context.Packages.Skipped()} Skipped, {context.Packages.Errors()} Failed");
        }

        private async Task<MigratedPackage> MigratePackageAsync ( MigrationContext context, Package package, CancellationToken cancellationToken )
        {
            Logger.StartActivity($"Migrating package '{package.Name}'");

            var migratedPackage = new MigratedPackage(package);

            using (var logger = Logger.BeginScope("MigratePackage"))
            {
                try
                {
                    //Is this blacklisted
                    if (Settings.ExcludePackages.Contains(package.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        Logger.Warning($"Package is excluded, skipping");
                        migratedPackage.Skipped = true;
                        return migratedPackage;
                    };
                    
                    //Does the package already exist                    
                    var existingPackage = await context.TargetService.GetPackageAsync(Settings.TargetFeed, migratedPackage.Name, true, cancellationToken).ConfigureAwait(false);
                    
                    //For each version of the package to migrate (sorted by version)                     
                    foreach (var version in package.Versions.OrderBy(v => v.Version))
                    {
                        var migratedVersion = new MigratedPackageVersion() { PackageName = migratedPackage.Name, Version = version.Version };
                        migratedPackage.Versions.Add(migratedVersion);

                        //If it is delisted and we aren't migrating them then skip it
                        if (!version.IsListed && !Settings.IncludeDelistedVersions)
                        {
                            Logger.Warning($"Version {version.Version} is delisted, skipping");
                            migratedVersion.Skipped = true;
                            continue;
                        } else if (existingPackage?.ContainsVersion(version) ?? false)
                        {
                            Logger.Warning($"Version {version.Version} already in target, skipping");
                            migratedVersion.Skipped = true;
                            continue;
                        };

                        //Download it     
                        migratedVersion.FilePath = FileSystem.BuildPath(context.OutputPath, $"{package.Name}.{version.Version}.nupkg");                        

                        await context.SourceService.SavePackageAsync(Settings.SourceFeed, migratedVersion.PackageName, migratedVersion.Version, migratedVersion.FilePath, cancellationToken).ConfigureAwait(false);
                        Logger.Info($"Downloaded package '{migratedVersion.FileName}'");

                        cancellationToken.ThrowIfCancellationRequested();                        
                        var newPackage = await PublishPackageAsync(context, migratedVersion, cancellationToken).ConfigureAwait(false);

                        //If this version is delisted
                        if (!version.IsListed)
                        {
                            cancellationToken.ThrowIfCancellationRequested();                            
                            await context.TargetService.DelistPackageAsync(Settings.TargetFeed, package.Name, version.Version, cancellationToken).ConfigureAwait(false);
                            Logger.Debug($"Delisted version '{version.Version}'");
                        };

                        Logger.Info($"Migrated version '{version.NormalizedVersion}', Id = {newPackage.Id}");
                    };
                } catch (Exception e)
                {
                    migratedPackage.Error = e;

                    Logger.Error(e);
                };
            };

            return migratedPackage;
        }

        private async Task<Package> PublishPackageAsync ( MigrationContext context, MigratedPackageVersion version, CancellationToken cancellationToken )
        {
            //REST API doesn't currently expose way to publish packages so we have to use the command line

            //Push to target server using NuGet
            await context.Command.PushPackageAsync(Settings.TargetPackageSource, version.FilePath, cancellationToken).ConfigureAwait(false);

            //Now get the package that was just pushed
            var package = await context.TargetService.GetPackageAsync(Settings.TargetFeed, version.PackageName, true, cancellationToken).ConfigureAwait(false);
            if (package == null)
                throw new Exception($"Publish failed for package '{version.PackageName}");

            return package;
        }

        private PackageManagementSettings Settings { get; set; }
        #endregion
    }
}
