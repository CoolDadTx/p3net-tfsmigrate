/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using TfsMigrate.Data;

namespace TfsMigrate.Processors.QueryManagement.Data
{
    class MigratedQuery : MigratedObject
    {
        #region Construction

        public MigratedQuery()
        { }

        public MigratedQuery ( QueryHierarchyItem query )
        {
            Name = query.Path;
        }
        #endregion

        public string Name { get; set; }
    }
}
