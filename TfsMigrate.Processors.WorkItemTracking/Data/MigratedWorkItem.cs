/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

using TfsMigrate.Data;

namespace TfsMigrate.Processors.WorkItemTracking.Data
{
    public class MigratedWorkItem : MigratedObject
    {        
        public int SourceId { get; set; }
        
        public WorkItem Target { get; set; }

        public int TargetId => Target?.Id ?? 0;

        public string TargetUrl => Target?.Url ?? "";
    }    
}
