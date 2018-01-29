/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

using TfsMigrate.Diagnostics;
using TfsMigrate.WorkItemTracking;

namespace TfsMigrate.Processors.WorkItemTracking.FieldHandlers
{
    public class UserFieldHandler : FieldHandler
    {
        public static UserFieldHandler Instance = new UserFieldHandler();

        public override Task<WorkItemFieldValue> HandleAsync ( JsonPatchDocument document, WorkItemFieldValue field, CancellationToken cancellationToken )
        {
            var match = s_re.Match(field.Value?.ToString() ?? "");
            if (match.Success)
            {
                var username = match.Groups["username"];
                if (username != null)
                {
                    //Find the user in our list
                    if (!Context.TargetUsers.TryGetValue(username.Value, out var user))
                    {
                        Logger.Warning($"User '{username.Value}' not found, ignoring");

                        //Add them so we don't report them again
                        user = username.Value;
                        Context.TargetUsers[username.Value] = user;
                    };

                    field.Value = user;
                };
            };

            return Task.FromResult(field);
        }

        private static readonly Regex s_re = new Regex("(?<displayname>[^<]+)<(?<username>[^>]*)>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    }
}
