/*
 * Copyright © 2017 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TfsMigrate.Hosting
{
    /// <summary></summary>
    public interface IProcessorHost
    {        
        IHostSettings Settings { get; }

        Task<T> GetSettingsAsync<T> ( string category, CancellationToken cancellationToken ) where T : new();
    }
}
