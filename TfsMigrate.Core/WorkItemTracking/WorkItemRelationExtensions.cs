/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace TfsMigrate.WorkItemTracking
{
    public static class WorkItemRelationExtensions
    {
        /// <summary>Gets the ID of the related item.</summary>
        /// <param name="source">The source.</param>
        /// <returns>The ID of the related item or 0 if it cannot be found.</returns>
        /// <remarks>
        /// The related ID isn't available directly but has to be determined by parsing the URL. Only work items are supported right now.
        /// </remarks>
        public static int GetRelatedId ( this WorkItemRelation source )
        {
            //Parse the URL, the ID is the last value
            var url = source.Url;
            if (!String.IsNullOrEmpty(url))
            {
                //Skip to the last value
                var idStringIndex = url.LastIndexOf('/');
                if (idStringIndex >= 0)
                {
                    var idString = url.Substring(idStringIndex + 1);
                    if (Int32.TryParse(idString, out var id))
                        return id;
                };
            };

            return 0;
        }

        /// <summary>Determines if this is a child relation.</summary>
        /// <param name="source">The source.</param>
        /// <returns><see langword="true"/> if the relationship matches or <see langword="false"/> otherwise.</returns>
        public static bool IsChild ( this WorkItemRelation source ) => String.Compare(source.Rel ?? "", WorkItemRelations.Child, true) == 0;

        /// <summary>Determines if this is a parent relation.</summary>
        /// <param name="source">The source.</param>
        /// <returns><see langword="true"/> if the relationship matches or <see langword="false"/> otherwise.</returns>
        public static bool IsParent ( this WorkItemRelation source ) => String.Compare(source.Rel ?? "", WorkItemRelations.Parent, true) == 0;

        /// <summary>Determines if this is a related relation.</summary>
        /// <param name="source">The source.</param>
        /// <returns><see langword="true"/> if the relationship matches or <see langword="false"/> otherwise.</returns>
        public static bool IsRelated ( this WorkItemRelation source ) => String.Compare(source.Rel ?? "", WorkItemRelations.Related, true) == 0;
    }
}
