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
    public class MigratedObject : IMigratedObject
    {
        public Exception Error { get; set; }

        public bool Skipped { get; set; }

        public bool Succeeded => Error == null;
    }
}
