/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System.Collections.Generic;
using System.Linq;

using TfsMigrate.Data;
using TfsMigrate.Processors.PackageManagement.Packaging;

namespace TfsMigrate.Processors.PackageManagement.Data
{
    class MigratedPackage : MigratedObject
    {
        #region Construction

        public MigratedPackage ()
        { }

        public MigratedPackage ( Package item )
        {
            Name = item?.Name;
        }
        #endregion

        public string Name { get; set; }

        public List<MigratedPackageVersion> Versions { get; } = new List<MigratedPackageVersion>();
    }
}
