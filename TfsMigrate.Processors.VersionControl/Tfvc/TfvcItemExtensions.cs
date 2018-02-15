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
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace TfsMigrate.Processors.VersionControl.Tfvc
{
    public static class TfvcItemExtensions
    {
        public static string GetName ( this TfvcItem source )
        {
            var index = source.Path.LastIndexOf('/');
            if (index <= 0)
                return source.Path;

            return source.Path.Substring(index + 1);
        }
    }
}
