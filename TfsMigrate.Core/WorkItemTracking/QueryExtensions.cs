/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fsmb.Apollo;

namespace TfsMigrate.WorkItemTracking
{
    public static class QueryExtensions
    {
        public const string SharedQueriesPath = "Shared Queries";

        public static string ToSharedQueryPath ( this string relativePath )
        {
            if (String.IsNullOrEmpty(relativePath))
                return SharedQueriesPath;

            if (relativePath.StartsWith(SharedQueriesPath, StringComparison.OrdinalIgnoreCase))
                return relativePath;

            return StringExtensions.Combine("/", SharedQueriesPath, relativePath);
        }

        public static string ToRelativeSharedQueryPath ( this string fullPath )
        {
            if (String.IsNullOrEmpty(fullPath))
                return "";

            if (fullPath.StartsWith(SharedQueriesPath, StringComparison.OrdinalIgnoreCase))
                return fullPath.Substring(SharedQueriesPath.Length).TrimStart('/', '\\');

            return fullPath;
        }
    }
}
