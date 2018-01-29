/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TfsMigrate.Processors.VersionControl
{
    public class VersionControlSettings
    {
        public string GitCommandLine { get; set; }

        public List<ProjectSettings> Projects { get; set; } = new List<ProjectSettings>();

        public string BaselineBranch { get; set; }
        public string DevelopmentBranch { get; set; }
        public string ReleaseBranch { get; set; }
        
        public string GitMasterBranch { get; set; }
        public string GitReleaseBranch { get; set; }

        public string TemplatePath { get; set; }
        public string MetadataFile { get; set; }
        
        public List<string> CleanFolders
        {
            get => _cleanFolders;
            set => _cleanFolders = value ?? new List<string>();
        }

        public List<string> CleanFiles
        {
            get => _cleanFiles;
            set => _cleanFiles = value ?? new List<string>();
        }

        public bool CleanAfterCommit { get; set; }

        #region Private Members

        private List<string> _cleanFolders = new List<string>();
        private List<string> _cleanFiles = new List<string>();
            
        #endregion
    }

    public class ProjectSettings
    {       
        public string SourcePath { get; set; }

        public bool HasBranches { get; set; }

        public string DestinationPath { get; set; }

        public string DestinationProject { get; set; }
    }
}
