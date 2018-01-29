/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfsMigrate.Hosting
{
    public interface IHostSettings
    {
        bool Debug { get; }

        string OutputPath { get; }

        string SourceCollectionUrl { get; }
        string SourceUser { get; }
        string SourceAccessToken { get; }

        string SourceProject { get; }

        string TargetCollectionUrl { get; }
        string TargetAccessToken { get; }

        string TargetProject { get; }
    }
}
