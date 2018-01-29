/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace TfsMigrate.WorkItemTracking
{
    public static class WorkItemExtensions
    {        
        public static string GetWorkItemType ( this WorkItem source ) => TryGetField(source, WorkItemFields.WorkItemType)?.ValueAsString() ?? "";

        public static bool IsClosed ( this WorkItem source )
        {
            var state = source.TryGetField(WorkItemFields.State);
            if (state == null)
                throw new Exception("State not found");

            return String.Compare(state.ValueAsString(), "Closed", true) == 0;
        }

        public static WorkItemFieldValue TryGetField ( this WorkItem source, string fieldName )
        {
            //TODO: Is this case sensitive?
            object value = null;
            if (source?.Fields?.TryGetValue(fieldName, out value) ?? false)
                return new WorkItemFieldValue(fieldName, value);

            return null;
        }

        public static void EnsureFieldSet ( this IDictionary<string, WorkItemFieldUpdate> source, string fieldName, object value )
        {
            var fieldPath = JsonPatchDocumentExtensions.ToFieldPath(fieldName);
            if (!source.TryGetValue(fieldName, out var updateValue))
                source[fieldName] = new WorkItemFieldUpdate() { NewValue = value, OldValue = value };
        }
    }
}
