/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfsMigrate.Data
{
    public static class MigratedObjectExtensions
    {
        /// <summary>Determines how many migrated objects returned errors.</summary>
        /// <param name="source">The source.</param>
        /// <returns>The number with errors.</returns>
        public static int Errors ( this IEnumerable<IMigratedObject> source )
        {
            return source.Count(i => !i.Skipped && !i.Succeeded);
        }

        /// <summary>Determines how many migrated objects where skipped.</summary>
        /// <param name="source">The source.</param>
        /// <returns>The number that were skipped.</returns>
        public static int Skipped ( this IEnumerable<IMigratedObject> source )
        {
            return source.Count(i => i.Skipped);
        }

        /// <summary>Determines how many migrated objects succeeded (not skipped).</summary>
        /// <param name="source">The source.</param>
        /// <returns>The number that succeeded.</returns>
        public static int Succeeded ( this IEnumerable<IMigratedObject> source )
        {
            return source.Count(i => i.Succeeded && !i.Skipped);
        }
    }
}
