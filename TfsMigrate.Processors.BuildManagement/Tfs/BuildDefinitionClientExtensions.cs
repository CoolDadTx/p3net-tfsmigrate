/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;

namespace TfsMigrate.Processors.BuildManagement.Tfs
{
    static class BuildDefinitionClientExtensions
    {
        public static Task<BuildDefinition> AddDefinitionAsync ( this BuildHttpClient source, BuildDefinition definition, CancellationToken cancellationToken )
                        => source.CreateDefinitionAsync(definition, cancellationToken: cancellationToken);

        public static Task<BuildDefinitionTemplate> AddTemplateAsync ( this BuildHttpClient source, TeamProject project, BuildDefinitionTemplate template, CancellationToken cancellationToken )
                                => source.SaveTemplateAsync(template, project.Id, template.Name, cancellationToken: cancellationToken);

        public static Task DeleteDefinitionAsync ( this BuildHttpClient source, BuildDefinition definition, CancellationToken cancellationToken )
                        => source.DeleteDefinitionAsync(definition.Project.Id, definition.Id, cancellationToken: cancellationToken);

        public static async Task<IEnumerable<BuildDefinitionTemplate>> GetBuildTemplatesAsync ( this BuildHttpClient source, TeamProject project, CancellationToken cancellationToken )
                                    => await source.GetTemplatesAsync(project.Id, cancellationToken: cancellationToken).ConfigureAwait(false);

        public static async Task<IEnumerable<BuildDefinition>> GetFullDefinitionsAsync ( this BuildHttpClient source, TeamProject project, CancellationToken cancellationToken )
                            => await source.GetFullDefinitionsAsync(project.Id, cancellationToken: cancellationToken).ConfigureAwait(false);

        public static async Task<BuildDefinition> GetDefinitionAsync ( this BuildHttpClient source, TeamProject project, string definitionName, CancellationToken cancellationToken )
        {
            //TODO: Use GetDefinitionsAsync to get by name a ref and then get full definition
            //HACK: Cannot get a definition by its name so have to retrieve everything and then search
            var definitions = await source.GetFullDefinitionsAsync(project, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            return definitions.FirstOrDefault(d => String.Compare(d.Name, definitionName, true) == 0);
        }        
    }
}
