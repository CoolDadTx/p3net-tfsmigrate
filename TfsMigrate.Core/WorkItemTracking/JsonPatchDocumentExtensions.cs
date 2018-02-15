/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using P3Net.Kraken;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace TfsMigrate.WorkItemTracking
{
    public static class JsonPatchDocumentExtensions
    {
        public static JsonPatchOperation AddField ( this JsonPatchDocument source, string fieldName, object value )
        {
            var op = new JsonPatchOperation() { Operation = Operation.Add, Path = ToFieldPath(fieldName), Value = value };
            
            source.Add(op);

            return op;
        }        

        public static JsonPatchOperation AddLink ( this JsonPatchDocument source, WorkItemRelation relation )
        {
            var op = new JsonPatchOperation() { Operation = Operation.Add, Path = "/relations/-", Value = relation };

            source.Add(op);

            return op;
        }

        public static JsonPatchOperation AddLink ( this JsonPatchDocument source, string linkType, string targetUrl )
        {
            var relation = new WorkItemRelation() { Rel = linkType, Url = targetUrl };
            return AddLink(source, relation);
        }

        public static string ToFieldPath ( string basePath ) => basePath.EnsureStartsWith("/fields/", StringComparison.OrdinalIgnoreCase);

        public static JsonPatchOperation UpdateField ( this JsonPatchDocument source, string fieldName, object newValue )
        {            
            var op = new JsonPatchOperation() { Operation = Operation.Replace, Path = ToFieldPath(fieldName), Value = newValue };
            
            source.Add(op);

            return op;
        }
    }
}
