/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfsMigrate.Processors.PackageManagement
{
    class PackageManagementSettings
    {
        public string SourceUrl { get; set; }
        public string SourceFeed { get; set; }

        public string TargetUrl { get; set; }
        public string TargetFeed { get; set; }
        public string TargetPackageSource { get; set; }

        public string NuGetCommandLine { get; set; }

        public bool IncludeDelistedVersions { get; set; }

        public bool LatestVersiononly { get; set; }

        public List<string> ExcludePackages { get; set; }
    }

    
}
