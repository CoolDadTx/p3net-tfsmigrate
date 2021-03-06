﻿/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Linq;
using P3Net.Kraken;
using TfsMigrate.Data;
using TfsMigrate.WorkItemTracking;

namespace TfsMigrate.Processors.WorkItemTracking.Data
{
    class MigratedIteration : MigratedObject
    {
        public NodePath SourcePath { get; set; }

        public NodePath TargetPath { get; set; }

        public int TargetId { get; set; }

        public DateRange Dates { get; set; }
    }
}
