using SanteDB.Core.Configuration;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.Mobile
{
    public class MauiApplicationContext : ClientApplicationContextBase
    {
        readonly StartupPage _StartupPage;

        public MauiApplicationContext(string instanceName, IConfigurationManager configurationManager, StartupPage startupPage, string bridgeScript)
            : base(Core.SanteDBHostType.Client, instanceName, configurationManager)
        {

            _StartupPage = startupPage;

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

            SanteDB.Core.Model.Map.ModelMapper.UseReflectionOnly = true; //This is a hack for now until we can rewrite the model mapper to use source generators.
        }

        public override void Start()
        {
            DependencyServiceManager.ProgressChanged += DependencyServiceManager_ProgressChanged;
            base.Start();
            DependencyServiceManager.ProgressChanged -= DependencyServiceManager_ProgressChanged;
        }

        private void DependencyServiceManager_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            _StartupPage?.SetStatus(e.TaskIdentifier, e.State, e.Progress);
        }

        protected override void OnRestartRequested(object sender)
        {
            Debugger.Break();
        }
    }
}
