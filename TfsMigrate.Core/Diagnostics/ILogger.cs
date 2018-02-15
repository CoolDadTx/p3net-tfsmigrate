/*
 * Copyright © 2012 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;

namespace TfsMigrate.Diagnostics
{
    /// <summary>Represents a logger.</summary>
    /// <remarks>
    /// An application should, in general, create a single, shared logger to be used for the entire application.  Additional
    /// loggers can be created if per-component logging is desired.
    /// </remarks>
    public interface ILogger
    {
        /// <summary>Sends an entry to the logger.</summary>
        /// <param name="entry">The entry to log.</param>
        void LogEntry ( LogEntryInfo entry );
    }
}
