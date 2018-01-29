/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;

namespace TfsMigrate.Hosting
{
    public class HostSettings : IHostSettings
    {
        public bool Debug { get; set; }

        public string OutputPath { get; set; }

        public string SourceCollectionUrl { get; set; }
        public string SourceUser { get; set; }
        public string SourceAccessToken { get; set; }

        public string SourceProject { get; set; }

        public string TargetCollectionUrl { get; set; }
        public string TargetAccessToken { get; set; }

        public string TargetProject { get;set; }
    }
}
