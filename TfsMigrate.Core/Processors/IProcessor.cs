/*
 * Copyright © 2017 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Threading;
using System.Threading.Tasks;

using TfsMigrate.Hosting;

namespace TfsMigrate.Processors
{
    /// <summary></summary>
    public interface IProcessor
    {
        Task InitializeAsync ( IProcessorHost host, CancellationToken canbCancellation );

        Task RunAsync ( CancellationToken cancellationToken );
    }
}
