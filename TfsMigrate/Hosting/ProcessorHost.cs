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

using Fsmb.EnterpriseServices.Logging;

using TfsMigrate.Processors;
using TfsMigrate.Settings;

namespace TfsMigrate.Hosting
{
    internal class ProcessorHost : IProcessorHost
    {
        public IHostSettings Settings { get; set; }

        public ILogger Logger { get; set; }

        public ISettingsManager SettingsManager { get; set; }

        public Task<T> GetSettingsAsync<T> ( string category, CancellationToken cancellationToken ) where T : new()
        {
            return SettingsManager.GetSettingsAsync<T>(category, cancellationToken);
        }
    }
}
