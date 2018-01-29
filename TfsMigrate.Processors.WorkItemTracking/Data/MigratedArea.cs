/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Linq;
using TfsMigrate.Data;
using TfsMigrate.WorkItemTracking;

namespace TfsMigrate.Processors.WorkItemTracking.Data
{
    class MigratedArea : MigratedObject
    {
        public NodePath SourcePath { get; set; }

        public NodePath TargetPath { get; set; }

        public int TargetId { get; set; }
    }
}
