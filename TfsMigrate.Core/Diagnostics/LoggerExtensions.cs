/*
 * Copyright © 2012 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace TfsMigrate.Diagnostics
{
    /// <summary>Provides extension methods for <see cref="ILogger"/>.</summary>
    public static class LoggerExtensions
    {
        #region Critical

        /// <summary>Logs an error message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="action">The action to configure the log entry.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Critical ( this ILogger source, Action<LogEntryBuilder> action )
        {
            var builder = new LogEntryBuilder().Level(LogLevel.Critical);

            action(builder);

            if (!builder.HasStackTrace)
            {
                var trace = StackTraceExtensions.GetCallerStack();
                builder.WithStackTrace(trace);
            };

            var entry = builder.ToLogEntry();
            source.LogEntry(entry);

            return entry.Id;
        }

        /// <summary>Logs an error message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="message">The message to log.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Critical ( this ILogger source, string message )
        {
            var trace = StackTraceExtensions.GetCallerStack();
            return Critical(source, c => c.Message(message).WithStackTrace(trace));
        }

        /// <summary>Logs an error message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="format">The message to log.</param>
        /// <param name="arguments">The message arguments.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Critical ( this ILogger source, string format, params object[] arguments )
        {
            var trace = StackTraceExtensions.GetCallerStack();
            return Critical(source, c => c.Message(format, arguments)
                                   .WithStackTrace(trace));
        }

        /// <summary>Logs an error message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <param name="format">The message to log.</param>
        /// <param name="arguments">The message arguments.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Critical ( this ILogger source, IFormatProvider formatProvider, string format, params object[] arguments )
        {
            var trace = StackTraceExtensions.GetCallerStack();
            return Critical(source, c=> c.Message(formatProvider, format, arguments)
                                  .WithStackTrace(trace));
        }

        /// <summary>Logs an error message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="exception">The exception to log.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Critical ( this ILogger source, Exception exception )
        {
            return Critical(source, c => c.Message(exception.Message)
                                   .Exception(exception));
        }

        /// <summary>Logs an error message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message to log.</param>
        /// <returns>The unique log entry ID.</returns> 
        public static Guid Critical ( this ILogger source, Exception exception, string message )
        {
            return Critical(source, c => c.Message(message)
                                   .Exception(exception));
        }

        /// <summary>Logs an error message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="format">The message to log.</param>
        /// <param name="arguments">The message arguments.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Critical ( this ILogger source, Exception exception, string format, params object[] arguments )
        {
            return Critical(source, c => c.Message(format, arguments)
                                   .Exception(exception));
        }

        /// <summary>Logs an error message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <param name="format">The message to log.</param>
        /// <param name="arguments">The message arguments.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Critical ( this ILogger source, Exception exception, IFormatProvider formatProvider, string format, params object[] arguments )
        {
            return Critical(source, c => c.Message(formatProvider, format, arguments)
                                   .Exception(exception));
        }
        #endregion

        #region Debug

        /// <summary>Logs a debug message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="action">The action to configure the log entry.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Debug ( this ILogger source, Action<LogEntryBuilder> action )
        {
            var builder = new LogEntryBuilder().Level(LogLevel.Debug);

            action(builder);

            var entry = builder.ToLogEntry();
            source.LogEntry(entry);

            return entry.Id;
        }

        /// <summary>Logs a debug message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="message">The message to log.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Debug ( this ILogger source, string message )
        {
            return Debug(source, c=> c.Message(message));
        }

        /// <summary>Logs a debug message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="format">The message to log.</param>
        /// <param name="arguments">The message arguments.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Debug ( this ILogger source, string format, params object[] arguments )
        {
            return Debug(source, c => c.Message(format, arguments));
        }

        /// <summary>Logs a debug message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <param name="format">The message to log.</param>
        /// <param name="arguments">The message arguments.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Debug ( this ILogger source, IFormatProvider formatProvider, string format, params object[] arguments )
        {
            return Debug(source, c => c.Message(formatProvider, format, arguments));
        }
        #endregion

        #region Error

        /// <summary>Logs an error message.</summary>        
        /// <param name="source">The source value.</param>
        /// <param name="action">The action to configure the log entry.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Error ( this ILogger source, Action<LogEntryBuilder> action )
        {
            var builder = new LogEntryBuilder().Level(LogLevel.Error);

            action(builder);

            if (builder.ToLogEntry().StackTrace == null)
            {
                var trace = StackTraceExtensions.GetCallerStack();
                builder.WithStackTrace(trace);
            };

            var entry = builder.ToLogEntry();
            source.LogEntry(entry);

            return entry.Id;
        }

        /// <summary>Logs an error message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="message">The message to log.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Error ( this ILogger source, string message )
        {
            var trace = StackTraceExtensions.GetCallerStack();

            return Error(source, c=> c.Message(message).WithStackTrace(trace));
        }

        /// <summary>Logs an error message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="format">The message to log.</param>
        /// <param name="arguments">The message arguments.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Error ( this ILogger source, string format, params object[] arguments )
        {
            var trace = StackTraceExtensions.GetCallerStack();
            return Error(source, c=> c.Message(format, arguments)
                               .WithStackTrace(trace));
        }

        /// <summary>Logs an error message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <param name="format">The message to log.</param>
        /// <param name="arguments">The message arguments.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Error ( this ILogger source, IFormatProvider formatProvider, string format, params object[] arguments )
        {
            var trace = StackTraceExtensions.GetCallerStack();

            return Error(source, c=> c.Message(formatProvider, format, arguments)
                               .WithStackTrace(trace));
        }

        /// <summary>Logs an error message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="exception">The exception to log.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Error ( this ILogger source, Exception exception )
        {
            return Error(source, c => c.Message(exception.Message)
                                .Exception(exception));   
        }

        /// <summary>Logs an error message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The message to log.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Error ( this ILogger source, Exception exception, string message )
        {
            return Error(source, c => c.Message(message)
                                .Exception(exception));   
        }

        /// <summary>Logs an error message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="format">The message to log.</param>
        /// <param name="arguments">The message arguments.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Error ( this ILogger source, Exception exception, string format, params object[] arguments )
        {
            return Error(source, c => c.Exception(exception)
                                .Message(format, arguments));
        }

        /// <summary>Logs an error message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <param name="format">The message to log.</param>
        /// <param name="arguments">The message arguments.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Error ( this ILogger source, Exception exception, IFormatProvider formatProvider, string format, params object[] arguments )
        {
            return Error(source, c => c.Exception(exception)
                                .Message(formatProvider, format, arguments));
        }
        #endregion

        #region Info

        /// <summary>Logs an informational message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="action">The action to configure the log.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Info ( this ILogger source, Action<LogEntryBuilder> action )
        {
            var builder = new LogEntryBuilder().Level(LogLevel.Info);

            action(builder);

            var entry = builder.ToLogEntry();
            source.LogEntry(entry);

            return entry.Id;
        }

        /// <summary>Logs an informational message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="message">The message to log.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Info ( this ILogger source, string message )
        {
            return Info(source, c => c.Message(message));
        }

        /// <summary>Logs an informational message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="format">The message to log.</param>
        /// <param name="arguments">The message arguments.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Info ( this ILogger source, string format, params object[] arguments )
        {
            return Info(source, c => c.Message(format, arguments));
        }

        /// <summary>Logs an informational message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <param name="format">The message to log.</param>
        /// <param name="arguments">The message arguments.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Info ( this ILogger source, IFormatProvider formatProvider, string format, params object[] arguments )
        {
            return Info(source, c => c.Message(formatProvider, format, arguments));
        }
        #endregion

        #region Warning

        /// <summary>Logs a warning message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="action">The action to configure the builder.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Warning ( this ILogger source, Action<LogEntryBuilder> action )
        {
            var builder = new LogEntryBuilder().Level(LogLevel.Warning);

            action(builder);

            var entry = builder.ToLogEntry();
            source.LogEntry(entry);

            return entry.Id;
        }

        /// <summary>Logs a warning message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="message">The message to log.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Warning ( this ILogger source, string message )
        {
            return Warning(source, c => c.Message(message));
        }

        /// <summary>Logs a warning message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="format">The message to log.</param>
        /// <param name="arguments">The message arguments.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Warning ( this ILogger source, string format, params object[] arguments )
        {
            return Warning(source, c => c.Message(format, arguments));
        }

        /// <summary>Logs a warning message.</summary>
        /// <param name="source">The source value.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <param name="format">The message to log.</param>
        /// <param name="arguments">The message arguments.</param>
        /// <returns>The unique log entry ID.</returns>
        public static Guid Warning ( this ILogger source, IFormatProvider formatProvider, string format, params object[] arguments )
        {
            return Warning(source, c => c.Message(formatProvider, format, arguments));
        }
        #endregion
    }
}
