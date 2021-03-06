﻿using Owin;
using SelfHost.Config;
using Thinktecture.IdentityServer.Core.Configuration;
using Thinktecture.IdentityServer.Core.Logging;

namespace SelfHost
{
    internal class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            LogProvider.SetCurrentLogProvider(new DiagnosticsTraceLogProvider());

            var options = new IdentityServerOptions
            {
                IssuerUri = "https://idsrv3.com",
                SiteName = "Thinktecture IdentityServer v3 - beta3 (EntityFramework)",
                
                SigningCertificate = Certificate.Get(),
                Factory = Factory.Configure("IdSvr3Config"),
                CorsPolicy = CorsPolicy.AllowAll
            };

            appBuilder.UseIdentityServer(options);
        }
    }
}