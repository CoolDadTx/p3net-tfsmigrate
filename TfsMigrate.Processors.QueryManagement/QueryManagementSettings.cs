/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfsMigrate.Processors.QueryManagement
{
    class QueryManagementSettings
    {
        public bool Overwrite { get; set; }

        public List<string> ExcludeQueries { get; set; } = new List<string>();
    }
}
