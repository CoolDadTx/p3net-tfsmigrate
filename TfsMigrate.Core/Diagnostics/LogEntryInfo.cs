/*
 * Copyright © 2012 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace TfsMigrate.Diagnostics
{
    /// <summary>Represents a log entry item.</summary>
    public class LogEntryInfo
    {
        #region Construction

        /// <summary>Initializes an instance of the <see cref="LogEntryInfo"/> class.</summary>
        public LogEntryInfo ()
        {
            Id = Guid.NewGuid();

            TimeStampUtc = DateTime.UtcNow;
        }
        #endregion

        #region Public Members

        /// <summary>Gets or sets a unique identifier for the log entry.</summary>
        public Guid Id { get; set;  }
        
        /// <summary>Gets or sets the optional data associated with the entry.</summary>
        /// <remarks>
        /// When formatting the data for logging the data is formatted according to its type.  If the type implements 
        /// <see cref="IFormattable"/> then the interface is used.  If the type is a key-value pair then the pairs are logged.
        /// Otherwise the public properties are converted to key-value pairs and then the pairs are logged.
        /// All other types are converted to a key-value pair of the public property values.
        /// </remarks>
        public object Data { get; set; }

        /// <summary>Gets or sets the exception associated with the entry.</summary>
        public Exception Exception { get; set; }

        /// <summary>Gets or sets the level of the entry.</summary>
        public LogLevel Level { get; set; }

        /// <summary>Gets or sets the logger that is generating the entry.</summary>
        public string LoggerName
        {
            get { return m_loggerName ?? ""; }
            set { m_loggerName = value; }
        }

        /// <summary>Gets or sets the message for the entry.</summary>
        public string Message 
        {
            get { return m_message ?? ""; }
            set { m_message = value; }
        }

        /// <summary>Gets or sets the stack trace that generated the entry.</summary>
        /// <value>This property is not set by default.</value>
        public StackTrace StackTrace { get; set;  }

        /// <summary>Gets or sets when the entry was generated.</summary>
        public DateTime TimeStamp 
        {
            get { return TimeStampUtc.ToLocalTime(); }
            set
            {
                TimeStampUtc = value.ToUniversalTime();
            }
        }

        /// <summary>Gets or sets when the entry was generated, in UTC.</summary>
        public DateTime TimeStampUtc { get; set; }

        /// <summary>Gets the formatted version of <see cref="Data"/>.</summary>
        /// <returns>The formatted data or <see langword="null"/> if there was no data.</returns>
        public string GetFormattedData ()
        {
            if (Data == null)
                return null;

            return FormatData(Data);
        }
        #endregion                

        #region Private Members

        private static string FormatData ( object data )
        {
            if (data is string)
                return (string)data;
            
            var formatter = data as IFormattable;
            if (formatter != null)
                return formatter.ToString();
            
            IDictionary<string, object> values = null;

            //Try as a dictionary first
            values = data as IDictionary<string, object>;
            if (values == null)
            {                   
                var dictionary = data as System.Collections.IDictionary;
                if (dictionary != null)
                {
                    values = new Dictionary<string, object>();

                    foreach (System.Collections.DictionaryEntry pair in dictionary)
                        values[pair.Key.ToString()] = pair.Value;
                };
            };

            //Use reflection
            if (values == null)
            {
                values = new Dictionary<string, object>();
                foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(data))
                    values[descriptor.Name] = descriptor.GetValue(data);
            };

            //Build the list
            var builder = new System.Text.StringBuilder();
            var useSeparator = false;
            builder.Append("{ ");
            foreach (var pair in values)
            {
                if (useSeparator)
                    builder.Append(", ");
                else
                    useSeparator = true;

                builder.AppendFormat("{0}={1}", pair.Key, pair.Value);
            };

            builder.Append(" }");
            return builder.ToString();
        }

        private string m_loggerName;
        private string m_message;
        #endregion
    }

    /// <summary>Defines the different types of logs.</summary>
    public enum LogLevel
    {
        /// <summary>Informational.</summary>
        Info = 0,

        /// <summary>Debug.</summary>
        Debug,

        /// <summary>Error.</summary>
        Error,

        /// <summary>Critical error.</summary>
        Critical,

        /// <summary>Warning.</summary>
        Warning,
    }
}
