/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace TfsMigrate.Tfs
{
    /// <summary>Provides extensions for the <see cref="VssHttpClientBase"/> class.</summary>
    public static class VssHttpClientExtensions
    {
        /// <summary>Adds a default error handler.</summary>
        /// <param name="handler">The handler.</param>
        public static void AddDefaultErrorHandler ( Func<Exception, bool> handler )
        {
            lock(s_errorHandlers)
            {
                s_errorHandlers.Add(handler);
            };
        }

        /// <summary>Clears the list of default error handlers.</summary>
        public static void ClearDefaultErrorHandlers ( )
        {
            lock (s_errorHandlers)
            {
                s_errorHandlers.Clear();
            };
        }

        /// <summary>Resets the default error handlers to their initial values.</summary>
        public static void ResetDefaultErrorHandlers ()
        {
            lock (s_errorHandlers)
            {
                s_errorHandlers.Clear();
                s_errorHandlers.AddRange(s_defaultHandlers);
            };
        }

        /// <summary>Calls a client method and handles standard errors.</summary>
        /// <typeparam name="TClient">The type of client.</typeparam>
        /// <typeparam name="TResult">The method result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="errorHandlers">The list of error handlers to handle the exceptions.</param>        
        /// <returns>The action result.</returns>
        /// <remarks>
        /// If no handlers are specified then the <see cref="DefaultErrorHandlers"/> are used.
        /// </remarks>
        public static async Task<TResult> TryCatchAsync<TClient, TResult> ( this TClient source, Func<TClient, Task<TResult>> action,
                                params Func<Exception, bool>[] errorHandlers ) where TClient : VssHttpClientBase
        {
            try
            {
                return await action(source).ConfigureAwait(false);
            } catch (Exception e)
            {
                lock (s_errorHandlers)
                {
                    IEnumerable<Func<Exception, bool>> handlers = errorHandlers;
                    if (handlers == null)
                        handlers = s_errorHandlers;

                    foreach (var handler in handlers)
                        if (handler(e))
                            return default(TResult);
                };

                throw;
            };
        }

        public static Task<TResult> TryCatchAsync<TClient, TResult> ( this TClient source, Func<TClient, Task<TResult>> action ) where TClient : VssHttpClientBase
                                     => TryCatchAsync(source, action, s_defaultHandlers);

        public static Task<TResult> TryCatchAsync<TClient, TResult> ( this TClient source, Func<TClient, Task<TResult>> action, IEnumerable<string> messageCodesToIgnore ) where TClient : VssHttpClientBase
        {
            var handlers = new List<Func<Exception, bool>>(s_defaultHandlers);
            if (messageCodesToIgnore?.Any() ?? false)
                handlers.Add(e => HandleMessageCodes(e, messageCodesToIgnore.ToArray()));   

            return TryCatchAsync(source, action, handlers.ToArray());
        }

        #region Private Members

        private static bool HandleMessageCodes ( Exception e, string[] messageCodes )
        {
            if (e is VssServiceException vse)
                return vse.IsMessageCode(messageCodes);

            return false;
        }

        private static bool HandleNotFound ( Exception e )
        {
            if (e is VssServiceResponseException response)
            {
                if (response.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                    return true;
            } else if (e is VssServiceException error)
            {
                if (error.IsMessageCode(VssMessageCodes.NotFound))
                    return true;
            };

            return false;
        }

        private static readonly Func<Exception, bool>[] s_defaultHandlers = new Func<Exception, bool>[] {
            HandleNotFound
        };

        private static readonly List<Func<Exception, bool>> s_errorHandlers = new List<Func<Exception, bool>>();
        #endregion
    }
}
