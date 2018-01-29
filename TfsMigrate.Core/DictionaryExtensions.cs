/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfsMigrate
{
    public static class DictionaryExtensions
    {
        public static T TryGetValue<T> ( this IDictionary<string, object> source, string key, T defaultValue = default(T) )
        {
            if (source == null)
                return defaultValue;

            if (source.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                    return typedValue;

                if (value != null)
                {
                    var str = value.ToString();

                    try
                    {
                        var converter = TypeDescriptor.GetConverter(typeof(T));
                        return (T)converter.ConvertFromString(str);
                    } catch (FormatException)
                    { /* Ignore */ };
                };
            };

            return defaultValue;
        }
    }
}
