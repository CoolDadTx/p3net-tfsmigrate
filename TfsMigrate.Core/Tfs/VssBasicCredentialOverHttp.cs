using System;
using System.Linq;
using System.Net;
using Microsoft.VisualStudio.Services.Common;

namespace TfsMigrate.Tfs
{
    class VssBasicCredentialOverHttp : FederatedCredential
    {
        public VssBasicCredentialOverHttp()
            : this((VssBasicToken)null)
        {
        }

        public VssBasicCredentialOverHttp(string userName, string password)
            : this(new VssBasicToken(new NetworkCredential(userName, password)))
        {
        }

        public VssBasicCredentialOverHttp(ICredentials initialToken)
            : this(new VssBasicToken(initialToken))
        {
        }

        public VssBasicCredentialOverHttp(VssBasicToken initialToken)
            : base(initialToken)
        {
        }

        public override VssCredentialsType CredentialType => VssCredentialsType.Basic;

        public override bool IsAuthenticationChallenge(IHttpResponse webResponse)
        {
            if (webResponse == null ||
                webResponse.StatusCode != HttpStatusCode.Found &&
                webResponse.StatusCode != HttpStatusCode.Unauthorized)
            {
                return false;
            }

            return webResponse.Headers.GetValues("WWW-Authenticate").Any(x => x.StartsWith("Basic", StringComparison.OrdinalIgnoreCase));
        }

        protected override IssuedTokenProvider OnCreateTokenProvider(Uri serverUrl, IHttpResponse response)
        {
            return new BasicAuthTokenProvider(this, serverUrl);
        }

        private sealed class BasicAuthTokenProvider : IssuedTokenProvider
        {
            public BasicAuthTokenProvider(IssuedTokenCredential credential, Uri serverUrl)
                : base(credential, serverUrl, serverUrl)
            {
            }
            protected override string AuthenticationScheme => "Basic";
            public override bool GetTokenIsInteractive => this.CurrentToken == null;
        }
    }
}
