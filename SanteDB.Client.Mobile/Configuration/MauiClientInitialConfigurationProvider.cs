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
using AndroidX.AppCompat.Widget;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Controls.PlatformConfiguration;
using SanteDB.BI.Services.Impl;
using SanteDB.BusinessRules.JavaScript;
using SanteDB.Caching.Memory;
using SanteDB.Caching.Memory.Session;
using SanteDB.Client.Configuration;
using SanteDB.Client.Configuration.Upstream;
using SanteDB.Client.Disconnected.Services;
using SanteDB.Client.Mobile.Diagnostics;
using SanteDB.Client.OAuth;
using SanteDB.Client.Services;
using SanteDB.Client.Tickles;
using SanteDB.Client.Upstream;
using SanteDB.Client.Upstream.Management;
using SanteDB.Client.Upstream.Repositories;
using SanteDB.Client.Upstream.Security;
using SanteDB.Client.UserInterface;
using SanteDB.Client.UserInterface.Impl;
using SanteDB.Core;
using SanteDB.Core.Applets.Services.Impl;
using SanteDB.Core.Configuration;
using SanteDB.Core.Data;
using SanteDB.Core.Data.Backup;
using SanteDB.Core.Diagnostics.Tracing;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Privacy;
using SanteDB.Core.Services;
using SanteDB.Core.Services.Impl;
using SanteDB.Security.Certs.BouncyCastle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.Mobile.Configuration
{
    public class MauiClientInitialConfigurationProvider : IInitialConfigurationProvider
    {
        public int Order => int.MaxValue;

        public SanteDBConfiguration Provide(SanteDBHostType hostContextType, SanteDBConfiguration configuration)
        {
            var appServiceSection = configuration.GetSection<ApplicationServiceContextConfigurationSection>();
            var instanceName = appServiceSection.InstanceName;
            var localDataPath = AppDomain.CurrentDomain.GetData("DataDirectory")?.ToString();

            if (null == localDataPath)
            {
                throw new ApplicationException("Application bug exists. DataDirectory was not set before configuration provider was called. Ensure the DataDirectory data variable in the app domain is set before the config provider is initialized.");
            }

            appServiceSection.RemoveService(new TypeReferenceConfiguration(typeof(BouncyCastleCertificateGenerator)));
            appServiceSection.RemoveService(new TypeReferenceConfiguration(typeof(AesSymmetricCrypographicProvider)));
            appServiceSection.RemoveAllServiceImplementations(typeof(IAppletHostBridgeProvider));
            appServiceSection.RemoveAllServiceImplementations(typeof(IUserInterfaceInteractionProvider));
            appServiceSection.RemoveAllServiceImplementations(typeof(IGeographicLocationProvider));
            appServiceSection.RemoveAllServiceImplementations(typeof(IOperatingSystemInfoService));
            appServiceSection.RemoveAllServiceImplementations(typeof(IPlatformSecurityProvider));
            appServiceSection.RemoveAllServiceImplementations(typeof(IBackupService));

            appServiceSection.AddServices(new List<TypeReferenceConfiguration>() {
                    //new TypeReferenceConfiguration(typeof(NullSymmetricCryptographicProvider)),
                    new TypeReferenceConfiguration(typeof(MauiInteractionProvider)),
                    new TypeReferenceConfiguration(typeof(MauiOperatingSystemInfoService)),
                    new TypeReferenceConfiguration(typeof(MauiPlatformSecurityProvider)),
                    new TypeReferenceConfiguration(typeof(MauiLocationProvider)),
                    new TypeReferenceConfiguration(typeof(MauiBackupProvider))
            });

            // On android the user cannot dynamically load asms
            appServiceSection.AllowUnsignedAssemblies = true;

            // Security configuration
            var wlan = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(o => o.NetworkInterfaceType == NetworkInterfaceType.Ethernet || o.Description.StartsWith("wlan"));
            String macAddress = Guid.NewGuid().ToString();
            if (wlan != null)
            {
                var mac = wlan.GetPhysicalAddress().ToString();

                if (!string.IsNullOrWhiteSpace(mac))
                {
                    macAddress = wlan.GetPhysicalAddress().ToString();
                }
            }

            // Upstream default configuration
            configuration.RemoveSection<UpstreamConfigurationSection>();
            UpstreamConfigurationSection upstreamConfiguration = new UpstreamConfigurationSection()
            {
                Credentials = new List<UpstreamCredentialConfiguration>()
                {
                    new UpstreamCredentialConfiguration()
                    {
#if DEBUG
                        CredentialName = $"Debugee-{macAddress.Replace(" ", "")}",
#else
                        CredentialName = $"{Android.OS.Build.Model}-{macAddress.Replace(" ", "")}",
#endif
                        Conveyance = UpstreamCredentialConveyance.Secret,
                        CredentialType = UpstreamCredentialType.Device
                    },
                    new UpstreamCredentialConfiguration()
                    {
                        CredentialName = "org.santedb.disconnected_client.android",
                        CredentialSecret = "C5B645B7D30A4E7E81A1C3D8B0E28F4C",
                        Conveyance = UpstreamCredentialConveyance.Secret,
                        CredentialType = UpstreamCredentialType.Application
                    }
                }
            };

            configuration.AddSection(upstreamConfiguration);

            var backupConfiguration = configuration.GetSection<BackupConfigurationSection>();
            if(backupConfiguration == null)
            {
                backupConfiguration = new BackupConfigurationSection()
                {
                    RequireEncryptedBackups = true
                };
                configuration.AddSection(backupConfiguration);
            }

            var externalDirectory = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments);
            if(!externalDirectory.Exists())
            {
                externalDirectory = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
            }

            // Fetch the backup locations
            backupConfiguration.PrivateBackupLocation = Path.Combine(localDataPath, "backup");
            backupConfiguration.PublicBackupLocation = Path.Combine(externalDirectory.AbsolutePath, "SanteDB", "Backups");

            // Externals go into the downloads folders
            externalDirectory = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
            backupConfiguration.ExternalBackupLocation= Path.Combine(externalDirectory.AbsolutePath, "SanteDB", "Backups");

            // IN RELEASE MODE OR DEBUG MODE PLACE A LOG WHERE THE USER CAN EASILY ACCESS IT
#if DEBUG || SDB_TRACE
            var diagnosticsConfigSection = configuration.GetSection<DiagnosticsConfigurationSection>();
            diagnosticsConfigSection.TraceWriter.Add(
                new TraceWriterConfiguration()
                {
                    Filter = System.Diagnostics.Tracing.EventLevel.Informational,
                    InitializationData = Path.Combine(externalDirectory.AbsolutePath, "SanteDB", "santedb.txt"),
                    TraceWriter = typeof(MauiPublicRolloverTraceWriter)
                }
            );
            diagnosticsConfigSection.Sources.ForEach(o => o.Filter = System.Diagnostics.Tracing.EventLevel.Informational);
            diagnosticsConfigSection.Sources.Add(new TraceSourceConfiguration() { SourceName = "SanteDB.Client", Filter = System.Diagnostics.Tracing.EventLevel.Informational });

#endif
            return configuration;
        }
    }
}
