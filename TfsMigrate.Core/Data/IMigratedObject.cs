/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfsMigrate.Data
{
    /// <summary>Used to identify common properties of migrated objects.</summary>
    public interface IMigratedObject
    {
        /// <summary>Determines if the object was skipped.</summary>
        bool Skipped { get; }

        /// <summary>Determines if the object was successfully migrated.</summary>
        bool Succeeded { get; }
    }    
}
