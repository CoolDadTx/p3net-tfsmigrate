/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace TfsMigrate.WorkItemTracking
{
    public static class WorkItemClassificationNodeExtensions
    {
        public static DateTime? GetFinishDate ( this WorkItemClassificationNode source )
        {
            return GetValue<DateTime?>(source, "finishDate");
        }

        public static DateTime? GetStartDate ( this WorkItemClassificationNode source )
        {
            return GetValue<DateTime?>(source, "startDate");
        }

        public static void SetFinishDate ( this WorkItemClassificationNode source, DateTime? value )
        {
            if (value != null)
                SetValue(source, "finishDate", value);
        }

        public static void SetStartDate ( this WorkItemClassificationNode source, DateTime? value )
        {
            if (value != null)
                SetValue(source, "startDate", value);
        }

        #region Private Members

        private static T GetValue<T> ( WorkItemClassificationNode node, string key )
        {
            if (node.Attributes != null)
                return node.Attributes.TryGetValue<T>(key);

            return default(T);
        }

        private static void SetValue<T> ( WorkItemClassificationNode node, string key, T value )
        {
            if (node.Attributes == null)
                node.Attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            node.Attributes[key] = value;
        }
        #endregion
    }
}
