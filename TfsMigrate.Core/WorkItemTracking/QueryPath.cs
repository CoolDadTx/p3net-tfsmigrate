/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using P3Net.Kraken;
using P3Net.Kraken.Diagnostics;

namespace TfsMigrate.WorkItemTracking
{
    public class QueryPath
    {
        #region Construction
        
        public QueryPath ( string queryPath )
        {
            Verify.Argument(nameof(queryPath)).WithValue(queryPath).IsNotNullOrEmpty();

            FullPath = queryPath.Trim(s_nodePathDelimiters);
            _normalizedPath = new Lazy<(string Name, QueryPath Parent)>(NormalizePath);
        }
        #endregion

        public string FullPath { get; }

        public bool IsRoot => Parent == null;

        public bool IsShared => FullPath.StartsWith("Shared Queries");
        
        public string Name => _normalizedPath.Value.Name;

        public QueryPath Parent => _normalizedPath.Value.Parent;

        public static string BuildPath ( string leftPath, string rightPath ) => StringExtensions.Combine("/", leftPath, rightPath);

        public override string ToString () => FullPath;

        #region Private Members        

        private (string Name, QueryPath Parent) NormalizePath ()
        {
            var name = "";
            QueryPath parent = null;
                        
            //Format is: [{parent}]/{Name}
            var index = FullPath.LastIndexOfAny(s_nodePathDelimiters);
            if (index > 0)
            {                
                name = FullPath.Substring(index + 1);
                parent = new QueryPath(FullPath.Left(index));
            } else
            {
                name = FullPath;
            };            
            
            return (name, parent);
        }
        
        private Lazy<(string Name, QueryPath Parent)> _normalizedPath;

        private static readonly char[] s_nodePathDelimiters = new char [] { '/' , '\\' };
        #endregion
    }
}
