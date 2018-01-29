/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TfsMigrate.Settings
{
    public class JsonSettingsManager : ISettingsManager
    {
        #region Construction

        public JsonSettingsManager ( string filename )
        {
            _filename = filename;            
        }
        #endregion

        public async Task<T> GetSettingsAsync<T> ( string category, CancellationToken cancellationToken ) where T : new()
        {            
            if (_settings.ContainsKey(category))
                return (T)_settings[category];

            await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

            //Find the root object
            return await Task.Run(() => {
                if (_jsonData.TryGetValue(category, StringComparison.OrdinalIgnoreCase, out var rootToken))
                {
                    return rootToken.ToObject<T>();
                };

                return new T();
            }).ConfigureAwait(false);
        }
        
        #region Private Members

        private async Task EnsureInitializedAsync ( CancellationToken cancellationToken )
        {
            if (_jsonData == null)
            {
                using (var textReader = new StreamReader(_filename))
                {
                    using (var reader = new JsonTextReader(textReader))
                    {
                        _jsonData = await JObject.LoadAsync(reader, cancellationToken) ?? new JObject();
                    };
                };
            };
        }

        private IEnumerable<PropertyInfo> GetProperties<T> ()
        {
            return from p in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                   where p.CanRead && p.CanWrite
                   select p;
        }

        private void TrySetProperty<T> ( T instance, PropertyInfo property, JToken value )
        {
            try
            {
                var typedValue = value.ToObject(property.PropertyType);
                if (typedValue != null)
                    property.SetValue(instance, typedValue);
            } catch
            { /* Silently ignore */ };
        }

        private readonly Dictionary<string, object> _settings = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private readonly string _filename;
        private JObject _jsonData;
        #endregion
    }
}
