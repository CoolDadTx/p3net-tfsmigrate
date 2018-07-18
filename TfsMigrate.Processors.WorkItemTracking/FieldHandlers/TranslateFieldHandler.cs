using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using TfsMigrate.WorkItemTracking;

namespace TfsMigrate.Processors.WorkItemTracking.FieldHandlers
{
    class TranslateFieldHandler : FieldHandler
    {
        public override Task<WorkItemFieldValue> HandleAsync(JsonPatchDocument document, WorkItemFieldValue field, CancellationToken cancellationToken)
        {
            Dictionary<string, string> targetTranslate = new Dictionary<string, string>();

            switch (field.Name)
            {
                case WorkItemFields.WorkItemType:
                    targetTranslate = Settings.Translate.Types.Where(x => !string.IsNullOrEmpty(x.Source)).ToDictionary(x => x.Source, x => x.Target);
                    break;
                case WorkItemFields.State:
                    targetTranslate = Settings.Translate.States.Where(x => !string.IsNullOrEmpty(x.Source)).ToDictionary(x => x.Source, x => x.Target);
                    break;
                case WorkItemFields.Severity:
                    targetTranslate = Settings.Translate.Severity.Where(x => !string.IsNullOrEmpty(x.Source)).ToDictionary(x => x.Source, x => x.Target);
                    break;
            }

            if (targetTranslate.TryGetValue(field.Value.ToString(), out string newValue))
            {
                field.Value = newValue;
            }

            return Task.FromResult(field);
        }
    }
}
