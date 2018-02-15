/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using P3Net.Kraken;
using Microsoft.VisualStudio.Services.Common;

namespace TfsMigrate.Tfs
{
    /// <summary>Provides extensions for <see cref="VssServiceException"/> instances.</summary>
    public static class VssExceptions
    {
        /// <summary>Gets the error code from the message, if any.</summary>
        /// <param name="source">The source.</param>
        /// <returns>The message code, if any.</returns>
        public static string GetMessageCode ( this VssServiceException source )
        {
            if (String.IsNullOrEmpty(source.Message))
                return "";

            var index = source.Message.IndexOf(':');
            if (index < 0)
                index = source.Message.IndexOf(' ');

            if (index < 0)
                return "";

            return source.Message.Left(index);
        }

        /// <summary>Determines if this is an error or not.</summary>
        /// <param name="source">The source.</param>
        /// <returns><see langword="true"/> if it is an error.</returns>
        public static bool IsError ( this VssServiceException source )
        {
            return source?.LogLevel == System.Diagnostics.EventLogEntryType.Error;
        }

        /// <summary>Determines if the message code is a specific value.</summary>
        /// <param name="source">The source.</param>
        /// <param name="messageCode">The expected message code.</param>
        /// <returns><see langword="true"/> if the error has the given message code.</returns>
        public static bool IsMessageCode ( this VssServiceException source, string messageCode )
        {
            var message = source.Message;
            if (String.IsNullOrEmpty(message))
                return false;

            return String.Compare(source.GetMessageCode(), messageCode, true) == 0;
        }

        /// <summary>Determines if the message code is one of several specific values.</summary>
        /// <param name="source">The source.</param>
        /// <param name="messageCodes">The expected message codes.</param>
        /// <returns><see langword="true"/> if the message code is in the provided list.</returns>
        public static bool IsMessageCode ( this VssServiceException source, params string[] messageCodes )
        {
            var message = source.Message;
            if (String.IsNullOrEmpty(message))
                return false;

            var actualCode = source.GetMessageCode();

            return messageCodes.Contains(actualCode, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Determines if this is a warning or not.</summary>
        /// <param name="source">The source.</param>
        /// <returns><see langword="true"/> if it is a warning.</returns>
        public static bool IsWarning ( this VssServiceException source )
        {
            return source?.LogLevel == System.Diagnostics.EventLogEntryType.Warning;
        }
    }
}
