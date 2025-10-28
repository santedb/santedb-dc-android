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
using Acornima.Ast;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using SanteDB.Client.Configuration;
using SanteDB.Client.UserInterface;
using SanteDB.Core.Configuration;
using SanteDB.Core.Data.Backup;
using SanteDB.Core.i18n;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.Mobile
{

    /// <summary>
    /// Custom implementation of the client application context
    /// </summary>
    public class MauiApplicationContext : ClientApplicationContextBase
    {
        readonly StartupPage _StartupPage;
        readonly Application _Application;
        readonly MauiInteractionProvider _InteractionProvider;

        public MauiApplicationContext(string instanceName, IConfigurationManager configurationManager, StartupPage startupPage, string bridgeScript)
            : base(Core.SanteDBHostType.Client, instanceName, configurationManager)
        {
            _Application = Application.Current ?? throw new NullReferenceException("Application.Current is null.");
            _StartupPage = startupPage;

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


            _InteractionProvider = new MauiInteractionProvider(_Application, startupPage);
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
            //Translate the sender to a specific reason that corresponds to appropriate language.
            var reason = sender switch
            {
                SanteDB.Core.Services.Impl.FileConfigurationService => Constants.REASONKEY_FILECONFIGURATION,
                SanteDB.Client.Configuration.InitialConfigurationManager => Constants.REASONKEY_INITIALCONFIGURATION,
                SanteDB.Client.Upstream.UpstreamUpdateManagerService => Constants.REASONKEY_UPDATE,
                SanteDB.Core.Data.Backup.DefaultBackupManager => Constants.REASONKEY_RESTORE,
                _ => Constants.REASONKEY_DEFAULT
            };

            _ = MainThread.InvokeOnMainThreadAsync(() =>
            {
                //We do not use the shell so we need to replace the main page in the app.
                var restartpage = new RestartPage(this);

                _Application.MainPage = restartpage;

                //Support the routing query parameter contract by calling the reason in.
                restartpage.ApplyQueryAttributes(new Dictionary<string, object>
                {
                        { "reason", reason }
                });

                return Task.CompletedTask;
            });

        }

        /// <summary>
        /// Gets the interaction provider used to broker communication between SanteDB and the user interface shell.
        /// </summary>
        /// <returns></returns>
        internal MauiInteractionProvider GetInteractionProvider() => _InteractionProvider;

        /// <summary>
        /// Auto restore environment
        /// </summary>
        protected override void AutoRestoreEnvironment()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var backupServiceManager = this.GetService<IBackupService>();
                var configurationManager = this.GetService<IConfigurationManager>();
                var uiInteraction = this.GetService<IUserInterfaceInteractionProvider>();
                if (configurationManager is InitialConfigurationManager && uiInteraction.Confirm(UserMessages.ISOLATED_STORAGE_BACKUP_RESTORE))
                {
                    try
                    {
                        var backupStream = uiInteraction.SelectFile(UserMessages.ISOLATED_STORAGE_SELECT_BACKUP_FILE, "application/octet-stream", null);
                        if (backupStream != null)
                        {
                            var backupDescriptor = backupServiceManager.GetBackupDescriptorFromStream(backupStream);
                            string backupSecret = backupDescriptor.IsEnrypted ? uiInteraction.Prompt(UserMessages.AUTO_RESTORE_BACKUP_SECRET, true) : String.Empty;
                            backupServiceManager.RestoreFromStream(backupStream, backupSecret);
                            this.OnRestartRequested(this);
                        }
                    }
                    catch (Exception e)
                    {
                        uiInteraction.Alert(e.ToHumanReadableString());
                    }
                }

            }
        }

    }
}
