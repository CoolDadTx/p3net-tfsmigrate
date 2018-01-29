/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Linq.Dynamic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

using TfsMigrate.WorkItemTracking;

namespace TfsMigrate.Processors.WorkItemTracking.FieldHandlers
{
    class ValueFieldHandler : FieldHandler
    {
        public string ExpressionValue
        {
            get => _expression ?? "";
            set 
            {
                ParseExpression(value);
                _expression = value;
            }
        }

        public override Task<WorkItemFieldValue> HandleAsync ( JsonPatchDocument document, WorkItemFieldValue field, CancellationToken cancellationToken )                        
        {
            if (_func != null)
                field.Value = _func.DynamicInvoke(field.Value);

            return Task.FromResult(field);
        }

        #region Private Members        

        private void ParseExpression ( string value )
        {
            var parm = System.Linq.Expressions.Expression.Parameter(typeof(object), "value");

            var expr = DynamicExpression.ParseLambda(new[] { parm }, null, value);
            _func = expr.Compile();
            
        }

        private string _expression;
        private Delegate _func;
        #endregion
    }
}
