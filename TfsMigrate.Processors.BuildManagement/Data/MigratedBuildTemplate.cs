/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Build.WebApi;

using TfsMigrate.Data;

namespace TfsMigrate.Processors.BuildManagement.Data
{
    class MigratedBuildTemplate : MigratedObject
    {
        #region Construction

        public MigratedBuildTemplate ()
        { }

        public MigratedBuildTemplate ( BuildDefinitionTemplate template )
        {
            Name = template.Name;
        }
        #endregion

        public string Name {get;set; }        
    }
}
