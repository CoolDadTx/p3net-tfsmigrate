/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfsMigrate.Processors.BuildManagement
{
    class BuildManagementSettings
    {
        public string TargetAgentQueue { get; set; }

        public bool Overwrite { get; set; }

        public List<string> ExcludeDefinitions { get; set; } = new List<string>();

        public bool CopyTemplates { get; set; }

        public List<TaskGroupSettings> TaskGroups { get; set; } = new List<TaskGroupSettings>();
    }

    class TaskGroupSettings
    {
        public Guid SourceGroupId { get; set; }
        public Guid TargetGroupId { get; set; }
    }
}
