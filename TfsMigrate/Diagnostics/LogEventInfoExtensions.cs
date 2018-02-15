using System;
using System.Collections.Generic;
using System.Linq;
using RealNLog = global::NLog;

namespace TfsMigrate.Diagnostics
{
    internal static class LogEventInfoExtensions
    {
        #region Public Members

        public static RealNLog.LogEventInfo FromLogEntry ( LogEntryInfo entry )
        {
            var info = new RealNLog.LogEventInfo() {
                Exception = entry.Exception,
                Level = ToNLogLevel(entry.Level),
                Message = entry.Message,
                TimeStamp = entry.TimeStamp
            };

            info.LoggerName = entry.LoggerName;

            if (entry.Data != null)
                info.Data(entry.GetFormattedData());

            if (entry.StackTrace != null)
                info.SetStackTrace(entry.StackTrace, 0);

            info.UniqueLogId(entry.Id);

            return info;
        }

        #region Attributes

        public static object Data ( this RealNLog.LogEventInfo source )
        {
            if (source.Properties.ContainsKey("Data"))
                return source.Properties["Data"];

            return null;
        }

        public static void Data ( this RealNLog.LogEventInfo source, object value )
        {
            source.Properties["Data"] = value;
        }

        public static Guid UniqueLogId ( this RealNLog.LogEventInfo source )
        {
            if (source.Properties.ContainsKey("UniqueLogId"))
                return (Guid)source.Properties["UniqueLogId"];

            return Guid.Empty;
        }

        public static void UniqueLogId ( this RealNLog.LogEventInfo source, Guid value )
        {
            source.Properties["UniqueLogId"] = value;
        }
        #endregion

        #endregion

        #region Private Members

        private static RealNLog.LogLevel ToNLogLevel ( LogLevel level )
        {
            switch (level)
            {
                case LogLevel.Debug: return RealNLog.LogLevel.Debug;
                case LogLevel.Error: return RealNLog.LogLevel.Error;
                case LogLevel.Critical: return RealNLog.LogLevel.Fatal;
                case LogLevel.Info: return RealNLog.LogLevel.Info;
                case LogLevel.Warning: return RealNLog.LogLevel.Warn;
            };

            //Just in case
            return RealNLog.LogLevel.Info;
        }
        #endregion
    }
}
