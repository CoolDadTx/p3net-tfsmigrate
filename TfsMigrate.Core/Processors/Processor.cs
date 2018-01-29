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

using TfsMigrate.Hosting;

namespace TfsMigrate.Processors
{
    public abstract class Processor : IProcessor
    {
        public string Name
        {
            get => !String.IsNullOrEmpty(_name) ? _name : GetType().Name;
            protected set => _name = value;
        }

        public Task InitializeAsync ( IProcessorHost host, CancellationToken cancellationToken )
        {
            Host = host;

            return InitializeCoreAsync(cancellationToken);
        }

        public async Task RunAsync ( CancellationToken cancellationToken )
        {
            try
            {
                await RunCoreAsync(cancellationToken).ConfigureAwait(false);
            } finally
            {
                Host = null;
                _settings.Clear();
            };            
        }        

        #region Protected Members

        protected IProcessorHost Host { get; private set; }
                   
        protected async Task<T> GetSettingsAsync<T> ( string category, CancellationToken cancellationToken ) where T: new()
        {
            if (Host == null)
                return default(T);

            if (_settings.TryGetValue(category, out var value))
                return (T)value;

            var settings = await Host.GetSettingsAsync<T>(category, cancellationToken).ConfigureAwait(false);
            _settings[category] = settings;

            return settings;
        }

        protected virtual Task InitializeCoreAsync ( CancellationToken cancellationToken ) => Task.CompletedTask;

        protected abstract Task RunCoreAsync ( CancellationToken cancellationToken );

        #endregion

        #region Private Members

        private readonly Dictionary<string, object> _settings = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private string _name;
        #endregion
    }
}
