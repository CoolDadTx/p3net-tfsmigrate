/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;

using Fsmb.Apollo;
using Fsmb.Apollo.Diagnostics;

namespace TfsMigrate.WorkItemTracking
{
    public class NodePath
    {
        #region Construction

        public NodePath ( string project, string relativePath ) : this(BuildPath(project, relativePath))
        {
        }

        public NodePath ( string nodePath )
        {
            Verify.Argument(nameof(nodePath)).WithValue(nodePath).IsNotNullOrEmpty();

            FullPath = nodePath;
            _normalizedPath = new Lazy<(string Project, string Name, NodePath Parent)>(NormalizePath);
        }
        #endregion

        public string FullPath { get; }

        public bool IsRoot => String.IsNullOrEmpty(Name);

        public string Name => _normalizedPath.Value.Name;

        public string Project => _normalizedPath.Value.Project;
        
        /// <summary>Gets the relative path excluding the project.</summary>
        public string RelativePath => IsRoot ? "" : FullPath.Substring(Project.Length + 1);

        public NodePath Parent => _normalizedPath.Value.Parent;

        public static string BuildPath ( string leftPath, string rightPath ) => StringExtensions.Combine("/", leftPath, rightPath);

        public override string ToString () => FullPath;

        #region Private Members        

        private (string Project, string Name, NodePath Parent) NormalizePath ()
        {
            var project = "";
            var relativePath = FullPath;
            var name = "";
            NodePath parent = null;
                        
            //Format is: {project}[/{parent}][/{Name}]
            var index = FullPath.IndexOfAny(s_nodePathDelimiters);
            if (index > 0)
            {                
                project = FullPath.Left(index);
                relativePath = FullPath.Substring(index + 1);

                index = relativePath.LastIndexOfAny(s_nodePathDelimiters);

                name = (index < 0) ? relativePath : relativePath.Substring(index + 1);
                parent = (index <= 0) ? null : new NodePath(project, relativePath.Left(index));
            } else
            {
                //Only the project is specified
                project = FullPath;
            };            
            
            return (project, name, parent);
        }
        
        private Lazy<(string Project, string Name, NodePath Parent)> _normalizedPath;

        private static readonly char[] s_nodePathDelimiters = new char [] { '/' , '\\' };
        #endregion
    }
}
