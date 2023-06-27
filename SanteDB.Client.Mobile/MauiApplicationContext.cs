using SanteDB.Core.Configuration;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.Mobile
{
    public class MauiApplicationContext : ClientApplicationContextBase
    {
        public MauiApplicationContext(string instanceName, IConfigurationManager configurationManager, StartupPage startupPage, string bridgeScript)
            : base(Core.SanteDBHostType.Client, instanceName, configurationManager)
        {

#if DEBUG
            //configurationManager.GetSection<ApplicationServiceContextConfigurationSection>().AllowUnsignedAssemblies = true;
#endif

            configurationManager.Configuration.AddSection<SecurityConfigurationSection>(new SecurityConfigurationSection
            {
                Signatures = new List<SecuritySignatureConfiguration>
                {
                    new SecuritySignatureConfiguration
                    {
                        Algorithm = SignatureAlgorithm.HS256,
                        HmacSecret = "@@SanteDB2021!&",
                    }
                }
            });

            DependencyServiceManager.AddServiceProvider(new MauiInteractionProvider(startupPage));
            DependencyServiceManager.AddServiceProvider(new MauiBridgeProvider(bridgeScript));
            DependencyServiceManager.AddServiceProvider(new MauiOperatingSystemInfoService());
            DependencyServiceManager.AddServiceProvider(new MauiPlatformSecurityProvider());
        }

        public override void Start()
        {
            base.Start();
        }

        protected override void OnRestartRequested(object sender)
        {

        }
    }
}
