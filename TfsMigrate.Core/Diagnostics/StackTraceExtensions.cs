/*
 * Copyright © 2014 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TfsMigrate.Diagnostics
{
    /// <summary>Provides extensions for <see cref="StackTrace"/>.</summary>
    public static class StackTraceExtensions
    {   
        /// <summary>Gets the callstack of the caller.</summary>
        /// <returns>The caller's stack.</returns>
        public static StackTrace GetCallerStack ()
        {
            return new StackTrace(2, true);
        }
    }
}
