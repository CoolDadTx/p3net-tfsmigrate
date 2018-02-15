/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;

namespace TfsMigrate.Diagnostics
{
    /// <summary>Helper class for working with <see cref="ILogger"/>.</summary>
    public static class Logger
    {
        public static void BeginScope ( ILogger logger )
        {
            lock (_loggers)
            {
                _loggers.Push(logger);
            };
        }

        public static ScopedLogger BeginScope ( string scopeName, string message = null )
        {
            if (!String.IsNullOrEmpty(message))
                Default.Info(message);

            lock (_loggers)
            {                
                var logger = new ScopedLogger(scopeName, Default);

                _loggers.Push(logger);

                return logger;
            };            
        }

        public static void ClearScopes ()
        {
            lock (_loggers)
            {
                _loggers.Clear();
            };
        }

        public static ILogger EndScope ( ILogger logger = null )
        {
            lock (_loggers)
            {
                if (_loggers.Count > 0)
                {
                    if (logger == null || _loggers.Contains(logger))
                    {
                        ILogger oldLogger = null;
                        do
                        {
                            oldLogger = _loggers.Pop();
                        } while (logger != null && oldLogger != logger);
                    };
                };
            };

            return Default;
        }

        public static ILogger Default => _loggers.Peek() ?? NullLogger.Instance;
        
        public static void Debug ( string message ) => Default?.Debug(message);
        
        public static void Error ( string message ) => Default?.Error(message);

        public static void Error ( Exception error ) => Default?.Error(error.Message);

        public static void Info ( string message ) => Default?.Info(message);

        public static void StartActivity ( string message ) => Default?.Info("==> " + message);

        public static void StopActivity ( string message ) => Default?.Info("<== " + message);

        public static void Warning ( string message ) => Default?.Warning(message);              

        private static readonly Stack<ILogger> _loggers = new Stack<ILogger>();        
    }

    public sealed class NullLogger : ILogger
    {
        private NullLogger ()
        { }

        public static readonly NullLogger Instance = new NullLogger();

        public void LogEntry ( LogEntryInfo entry ) { }
    }

    public class ScopedLogger : ILogger, IDisposable
    {
        #region Construction

        public ScopedLogger ( string scopeName, ILogger parent )
        {
            _scopeName = scopeName;
            _parent = parent;            
        }

        ~ScopedLogger ()
        {
            Dispose(false);
        }
        #endregion

        public void Dispose ()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        public void LogEntry ( LogEntryInfo entry )
        {
            entry.LoggerName = _scopeName;
            entry.Message = "   " + entry.Message;
            _parent.LogEntry(entry);
        }

        #region Private Members

        private void Dispose ( bool disposing )
        {
            Logger.EndScope(this);
        }

        private readonly string _scopeName;
        private readonly ILogger _parent;
        
        #endregion
    }
}
