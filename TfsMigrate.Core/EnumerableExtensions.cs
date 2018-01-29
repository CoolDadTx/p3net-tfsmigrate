/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfsMigrate
{
    public static class EnumerableExtensions
    {
        /// <summary>Flattens a list of items into itself.</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static IEnumerable<T> Flatten<T> ( this IEnumerable<T> source, Func<T, IEnumerable<T>> selector)
        {
            if (source == null)
                return Enumerable.Empty<T>();

            return source.Concat(source.SelectMany(i => selector(i).Flatten(selector)));
        }

        /// <summary>Determines if the enumerable items has any elements.</summary>
        /// <typeparam name="T">The type of data.</typeparam>
        /// <param name="source">The source.</param>
        /// <returns><see langword="true"/> if there are any elements or <see langword="false"/> if not.</returns>
        /// <remarks>
        /// Equivalent to <see cref="Enumerable{T}.Any" /> but also handles <see langword="null"/>.
        /// </remarks>
        public static bool HasAny<T> ( this IEnumerable<T> source ) => source?.Any() ?? false;

        /// <summary>Determines if the enumerable items has any elements.</summary>
        /// <typeparam name="T">The type of data.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="predicate">The predicate to check.</param>
        /// <returns><see langword="true"/> if there are any elements or <see langword="false"/> if not.</returns>
        /// <remarks>
        /// Equivalent to <see cref="Enumerable{T}.Any" /> but also handles <see langword="null"/>.
        /// </remarks>
        public static bool HasAny<T> ( this IEnumerable<T> source, Func<T, bool> predicate ) => source?.Any(predicate) ?? false;
    }
}
