/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.IO;

using TfsMigrate.Data;

namespace TfsMigrate.Processors.PackageManagement.Data
{
    class MigratedPackageVersion : MigratedObject
    {
        public string PackageName { get; set; }

        public string Version { get; set; }

        public string FilePath { get; set; }

        public string FileName => String.IsNullOrEmpty(FilePath) ? "" : Path.GetFileName(FilePath);
    }
}
