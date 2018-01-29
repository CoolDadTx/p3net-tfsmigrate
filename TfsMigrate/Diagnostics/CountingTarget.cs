/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Threading;

using NLog;

namespace TfsMigrate.Diagnostics
{
    internal class CountingTarget : NLog.Targets.Target
    {
        public CountingTarget ( string name )
        {
            Name = name;                 
        }

        public int ErrorCount => _errors;
        public int WarningCount => _warnings;

        protected override void Write ( LogEventInfo logEvent )
        {
            if (logEvent.Level == NLog.LogLevel.Error || logEvent.Level == NLog.LogLevel.Fatal)
                Interlocked.Increment(ref _errors);
            else if (logEvent.Level == NLog.LogLevel.Warn)
                Interlocked.Increment(ref _warnings);
        }

        #region Private Members

        private int _errors;
        private int _warnings;
        #endregion
    }
}
