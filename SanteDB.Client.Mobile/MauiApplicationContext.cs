/*
 * Portions Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2024 SanteSuite Contributors (See NOTICE)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: trevor
 * Date: 2023-4-19
 */
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

        private MauiInteractionProvider _InteractionProvider;

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
            _InteractionProvider = new MauiInteractionProvider(startupPage);
            DependencyServiceManager.AddServiceProvider(_InteractionProvider);
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
            if (Application.Current?.Dispatcher is IDispatcher dispatcher)
            {
                dispatcher.Dispatch(() =>
                {
                    Application.Current.Quit();
                });
            }
        }

        /// <summary>
        /// Gets the interaction provider used to broker communication between SanteDB and the user interface shell.
        /// </summary>
        /// <returns></returns>
        internal MauiInteractionProvider GetInteractionProvider() => _InteractionProvider;
    }
}
