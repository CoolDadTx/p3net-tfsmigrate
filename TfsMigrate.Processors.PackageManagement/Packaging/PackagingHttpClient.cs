/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace TfsMigrate.Processors.PackageManagement.Packaging
{
    class PackagingHttpClient : VssHttpClientBase
    {
        #region Construction

        public PackagingHttpClient ( Uri url, VssCredentials credentials ) : base(url, credentials)
        {
            _uri = url;
        }

        public PackagingHttpClient ( Uri url, VssCredentials credentials, VssHttpRequestSettings settings ) : base(url, credentials, settings)
        {
            _uri = url;
        }

        public PackagingHttpClient ( Uri url, VssCredentials credentials, params DelegatingHandler[] handlers ) : base(url, credentials, handlers)
        {
            _uri = url;
        }

        public PackagingHttpClient ( Uri url, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers ) : base(url, credentials, settings, handlers)
        {
            _uri = url;
        }

        public PackagingHttpClient ( Uri url, HttpMessageHandler pipeline, bool disposeHandler ) : base(url, pipeline, disposeHandler)
        {
            _uri = url;
        }
        #endregion
       
        public async Task DelistPackageAsync ( string feed, string packageName, string packageVersion, CancellationToken cancellationToken )
        {            
            //Would like to use the resource ID here...            
            var routeValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) {
                { "feed", feed },
                { "package", packageName },
                { "version", packageVersion }
            };
            
            var uri = BuildApiUrl("packaging/feeds/{feed}/nuget/packages/{package}/versions/{version}", routeValues);
            uri = ConvertToPackageUri(uri);
            var message = new HttpRequestMessage(new HttpMethod("PATCH"), uri);

            cancellationToken.ThrowIfCancellationRequested();
            var response = await SendAsync(message, cancellationToken: cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        public async Task<Stream> DownloadPackageAsync ( string feed, string packageName, string packageVersion, CancellationToken cancellationToken )
        {
            //Would like to use the resource ID here...            
            var routeValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) {
                { "feed", feed },
                { "package", packageName },
                { "version", packageVersion }
            };

            var uri = BuildApiUrl("packaging/feeds/{feed}/nuget/packages/{package}/versions/{version}/content", routeValues);
            uri = ConvertToPackageUri(uri);

            var message = new HttpRequestMessage(HttpMethod.Get, uri);

            cancellationToken.ThrowIfCancellationRequested();
            var response = await SendAsync(message, cancellationToken: cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync();
        }

        public async Task<Package> GetPackageAsync ( string feed, string packageName, bool includeAllVersions, CancellationToken cancellationToken )
        {
            //HACK: We want ot use the standard Package API but it doesn't seem to like package names even if they are normalized
            //TODO: Should cache this data but we'll be adding packages to it later so we'll take the hit of calling it each time
            var packages = await GetPackagesAsync(feed, true, includeAllVersions, cancellationToken).ConfigureAwait(false);

            return packages?.FirstOrDefault(p => String.Compare(p.Name, packageName, true) == 0);
        }

        public async Task<List<Package>> GetPackagesAsync ( string feed, bool includeDelisted, bool includeAllVersions, CancellationToken cancellationToken )
        {
            //Would like to use the resource ID here...            
            var routeValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) {
                { "feed", feed },
                { "includeUrls", false },
                { "includeAllversions", true },
                { "$top", 1000 },
                { "isListed", includeDelisted ? (bool?)null : true }
            };

            var uri = BuildApiUrl("packaging/feeds/{feed}/packages", routeValues);

            var message = new HttpRequestMessage(HttpMethod.Get, uri);

            //For some reason not all the package versions are coming across so use the packages to fetch all their versions
            var packages = await SendAsync<List<Package>>(message, cancellationToken: cancellationToken).ConfigureAwait(false) ?? new List<Package>();

            var routeVersionValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) {
                { "feed", feed },
                { "includeUrls", false },
                { "packageId", Guid.Empty },                
                { "isListed", includeDelisted ? (bool?)null : true }
            };

            foreach (var package in packages)
            {
                //Now fetch all the versions
                routeVersionValues["packageId"] = package.Id;
                uri = BuildApiUrl("packaging/feeds/{feed}/packages/{packageId}/versions", routeVersionValues);
                message = new HttpRequestMessage(HttpMethod.Get, uri);

                package.Versions = await SendAsync<List<PackageVersion>>(message, cancellationToken: cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            };

            return packages;
        }

        #region Private Members        

        private Uri BuildApiUrl ( string resourceName, Dictionary<string, object> routeValues )
        {            
            if (!routeValues.ContainsKey("api-version"))
                routeValues["api-version"] = _apiVersion;

            var relativeUrl = VssHttpUriUtility.ReplaceRouteValues($"_apis/{resourceName}", routeValues, appendUnusedAsQueryParams: true);

            return VssHttpUriUtility.ConcatUri(_uri, relativeUrl);
        }

        private static Uri ConvertToPackageUri ( Uri baseUri )
        {
            //Feeds are of the form: {account}.feeds.{host}/{collection}/_apis/{resourceName}
            //Packages are of the form: {account}.pkgs.{host}/{collection}/_apis/{resourceName}

            var uri = baseUri.ToString();
            uri = uri.Replace(".feeds.", ".pkgs.");

            return new Uri(uri);
        }

        private const string _apiVersion = "2.0-preview.1";
        private readonly Uri _uri;
        #endregion
    }
}
