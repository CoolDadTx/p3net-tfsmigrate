/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fsmb.Apollo;

namespace TfsMigrate.WorkItemTracking
{
    public static class WorkItemFields
    {
        public const string Area = "System.AreaPath";

        public const string ChangedBy = "System.ChangedBy";

        public const string ChangedDate = "System.ChangedDate";

        public const string History = "System.History";

        public const string Id = "System.ID";

        public const string Iteration = "System.IterationPath";

        public const string State = "System.State";

        public const string Tag = "System.Tags";

        public const string Title = "System.Title";

        public const string WorkItemType = "System.WorkItemType";
    }
}
