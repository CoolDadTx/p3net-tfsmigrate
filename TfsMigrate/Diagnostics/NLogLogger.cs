/*
 * Copyright © 2012 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;

using RealNLog = global::NLog;

namespace TfsMigrate.Diagnostics
{
    /// <summary>Adapter for NLog's Logger class.</summary>
    internal sealed class NLogLogger : ILogger
    {
        #region Construction

        /// <summary>Initializes an instance of the <see cref="NLogLogger"/> class.</summary>
        public NLogLogger ( RealNLog.Logger logger )
        {
            m_logger = logger;
        }
        #endregion
        
        #region ILogger Members

        public void LogEntry ( LogEntryInfo entry )
        {            
            entry.LoggerName = m_logger.Name;
            m_logger.Log(LogEventInfoExtensions.FromLogEntry(entry));
        }

        private readonly RealNLog.Logger m_logger;
        #endregion
    }
}