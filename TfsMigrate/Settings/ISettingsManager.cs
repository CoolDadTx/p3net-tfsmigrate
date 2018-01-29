/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TfsMigrate.Settings
{
    public interface ISettingsManager
    {
        Task<T> GetSettingsAsync<T> ( string category, CancellationToken cancellationToken ) where T : new();
    }
}
