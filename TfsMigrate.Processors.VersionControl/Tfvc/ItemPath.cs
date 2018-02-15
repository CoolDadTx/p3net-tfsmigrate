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

namespace TfsMigrate.Processors.VersionControl.Tfvc
{
    public static class ItemPath
    {
        public static string BuildPath ( string basePath, string childPath )
        {
            return StringExtensions.Combine("/", basePath, childPath);
        }

        public static string GetProject ( string itemPath )
        {
            if (!itemPath.StartsWith("$/"))
                throw new ArgumentException("Item path is not a full path", nameof(itemPath));

            var index = itemPath.IndexOf('/', 2);
            if (index <= 0)
                return "";

            return itemPath.Substring(2, index - 2);
        }
    }
}
