/*
 * Copyright © 2012 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TfsMigrate.Diagnostics
{
    /// <summary>Builder for log entries.</summary>
    public sealed class LogEntryBuilder
    {
        #region Construction

        /// <summary>Initializes an instance of the <see cref="LogEntryBuilder"/> class.</summary>
        public LogEntryBuilder ( )
        {
        }

        /// <summary>Initializes an instance of the <see cref="LogEntryBuilder"/> class.</summary>
        /// <param name="logger">The logger to use.</param>
        public LogEntryBuilder ( ILogger logger )
        {
            m_logger = logger;
        }
        #endregion

        #region Public Members

        /// <summary>Determines if the entry has a stack trace.</summary>
        public bool HasStackTrace
        {
            get { return m_entry.StackTrace != null; }
        }

        /// <summary>Sets the data.</summary>
        /// <param name="value">The value.</param>
        /// <returns>The builder.</returns>
        public LogEntryBuilder Data ( object value )
        {
            m_entry.Data = value;
            
            return this;
        }

        /// <summary>Sets the exception.</summary>
        /// <param name="value">The value.</param>
        /// <returns>The builder.</returns>
        /// <remarks>
        /// The stack trace of the exception is also set.
        /// </remarks>
        public LogEntryBuilder Exception ( Exception value )
        {
            m_entry.Exception = value;
            m_entry.StackTrace = new System.Diagnostics.StackTrace(value, true);

            return this;
        }

        /// <summary>Sets the ID.</summary>
        /// <param name="value">The value.</param>
        /// <returns>The builder.</returns>
        public LogEntryBuilder Id ( Guid value )
        {
            m_entry.Id = value;

            return this;
        }

        /// <summary>Sets the level.</summary>
        /// <param name="value">The value.</param>
        /// <returns>The builder.</returns>
        public LogEntryBuilder Level ( LogLevel value )
        {
            m_entry.Level = value;

            return this;
        }               

        /// <summary>Sets the message.</summary>
        /// <param name="value">The value.</param>
        /// <returns>The builder.</returns>
        public LogEntryBuilder Message ( string value )
        {
            m_entry.Message = value;
                        
            return this;
        }

        /// <summary>Sets the message.</summary>
        /// <param name="format">The value.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>The builder.</returns>
        public LogEntryBuilder Message ( string format, params object[] arguments )
        {
            m_entry.Message = String.Format(format, arguments);

            return this;
        }

        /// <summary>Sets the message.</summary>
        /// <param name="formatProvider">The format provider.</param>
        /// <param name="format">The value.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>The builder.</returns>
        public LogEntryBuilder Message ( IFormatProvider formatProvider, string format, params object[] arguments )
        {
            m_entry.Message = String.Format(formatProvider, format, arguments);

            return this;
        }

        /// <summary>Gets the log entry.</summary>
        /// <returns>The log entry.</returns>
        public LogEntryInfo ToLogEntry ()
        {
            return m_entry;
        }       

        /// <summary>Adds the stack trace to the entry.</summary>
        /// <param name="trace">The trace.</param>
        /// <returns>The builder.</returns>
        public LogEntryBuilder WithStackTrace ( StackTrace trace )
        {
            m_entry.StackTrace = trace;

            return this;
        }        
        #endregion

        #region Private Members

        private readonly LogEntryInfo m_entry = new LogEntryInfo();
        private readonly ILogger m_logger;
        #endregion
    }
}
