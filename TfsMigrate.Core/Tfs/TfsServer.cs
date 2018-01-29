/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using TfsMigrate.Diagnostics;

using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;

namespace TfsMigrate.Tfs
{
    public class TfsServer
    {
        #region construction

        public TfsServer ( string url, string accessToken )
        {
            _uri = new Uri(url);
            _accessToken = accessToken;

            _connection = new Lazy<VssConnection>(CreateConnection);
        }
        #endregion        

        public T GetClient<T> () where T : VssHttpClientBase
        {
            if (_clients.TryGetValue(typeof(T), out var client))
                return (T)client;

            var instance = Connection.GetClient<T>();
            _clients[typeof(T)] = instance;

            return instance;
        }

        public async Task<TeamProject> FindProjectAsync ( string projectName, CancellationToken cancellationToken )
        {
            var project = await Task.Run(() => ProjectClient.GetProject(projectName)).ConfigureAwait(false);
            if (project != null && project.Id != Guid.Empty)
                Logger.Debug($"Project '{projectName}' found, Id = {project.Id}");
            else
            {
                project = null;
                Logger.Debug($"Project '{projectName}' not found");
            };

            return project;
        }
        
        #region Private Members

        private VssConnection Connection => _connection.Value;

        private ProjectHttpClient ProjectClient => GetClient<ProjectHttpClient>();

        private VssConnection CreateConnection ()
        {
            var credentials = new VssBasicCredential("", _accessToken);

            Logger.Debug($"Creating connection: Uri = '{_uri}'");
            var conn = new VssConnection(_uri, credentials);
            Logger.Debug($"Created connection: Uri = '{_uri}'");

            return conn;
        }        

        private readonly Uri _uri;
        private readonly string _accessToken;

        private readonly Lazy<VssConnection> _connection;

        private Dictionary<Type, object> _clients = new Dictionary<Type, object>();
        #endregion
    }
}
