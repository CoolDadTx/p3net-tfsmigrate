/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;

namespace TfsMigrate.Processors.WorkItemTracking
{
    public class WorkItemTrackingSettings
    {
        public List<AreaSettings> Areas { get; set; } = new List<AreaSettings>();

        public List<IterationSettings> Iterations { get; set; } = new List<IterationSettings>();

        public bool IncludeAllFields { get; set; } = true;

        public List<FieldSettings> Fields { get; set; } = new List<FieldSettings>();

        public List<QuerySettings> Queries { get; set; } = new List<QuerySettings>();

        public List<UserSettings> Users { get; set; } = new List<UserSettings>();

        public TranslateSettings Translate { get; set; } = new TranslateSettings();

        public List<RepositorySettings> Repositories { get; set; } = new List<RepositorySettings>();

        public bool IncludeRelatedLinksOnClosed { get; set; }
        public bool IncludeChildLinksOnClosed { get; set; }
        public bool IncludeParentLinksOnClosed { get; set; }
        public bool IncludeAttachmentFiles { get; set; }
        public bool IncludeGitCommit { get; set; }

        public string MigrationTag { get; set; }
    }

    public class QuerySettings
    {
        public string Name { get; set; }
    }

    public class AreaSettings
    {
        public string SourcePath { get; set; }

        public string DestinationPath { get; set; }

        public string ActualDestinationPath => String.IsNullOrEmpty(DestinationPath) ? SourcePath : DestinationPath;
    }

    public class IterationSettings
    {
        public string SourcePath { get; set; }

        public string DestinationPath { get; set; }

        public string ActualDestinationPath => String.IsNullOrEmpty(DestinationPath) ? SourcePath : DestinationPath;
    }

    public class FieldSettings
    {
        public string Name { get; set; }

        public string TargetName { get; set; }

        public bool Ignore { get; set; }

        public string Value { get; set; }

        public bool IsUser { get; set; }

        public string Handler { get; set; }
    }

    public class UserSettings
    {
        public string Source { get; set; }
        public string Target { get; set; }
    }

    public class TypeSettings
    {
        public string Source { get; set; }

        public string Target { get; set; }
    }

    public class StateSettings
    {
        public string Source { get; set; }

        public string Target { get; set; }
    }

    public class RelationSettings
    {
        public string Source { get; set; }

        public string Target { get; set; }
    }

    public class SeveritySettings
    {
        public string Source { get; set; }

        public string Target { get; set; }
    }

    public class TranslateSettings
    {
        public List<TypeSettings> Types { get; set; }

        public List<RelationSettings> Relations { get; set; }

        public List<StateSettings> States { get; set; }

        public List<SeveritySettings> Severity { get; set; }
    }

    public class RepositorySettings
    {
        public string Source { get; set; }

        public string Target { get; set; }
    }
}
