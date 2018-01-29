/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

using TfsMigrate.WorkItemTracking;

namespace TfsMigrate.Processors.WorkItemTracking.FieldHandlers
{
    class RenameFieldHandler : FieldHandler
    {
        public string NewName { get; set; }

        public override Task<WorkItemFieldValue> HandleAsync ( JsonPatchDocument document, WorkItemFieldValue field, CancellationToken cancellationToken )
        {
            field.Name = NewName;

            return Task.FromResult(field);
        }
    }
}
