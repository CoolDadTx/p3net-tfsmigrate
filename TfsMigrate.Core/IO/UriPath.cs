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

namespace TfsMigrate.IO
{
    public static class UriPath
    {
        public static string AddQueryString ( string uri, string queryString )
        {
            if (uri.Contains("?"))
            {
                if (uri.EndsWith("?"))
                    return uri + queryString;

                return StringExtensions.Combine("&", uri, queryString);
            };

            return StringExtensions.Combine("?", uri, queryString);            
        }

        public static string Combine ( string baseUri, string relativeUri ) => StringExtensions.Combine("/", baseUri, relativeUri);
    }
}
