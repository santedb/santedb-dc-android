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
using SanteDB.BI.Services.Impl;
using SanteDB.BusinessRules.JavaScript;
using SanteDB.Caching.Memory.Session;
using SanteDB.Caching.Memory;
using SanteDB.Client.Configuration;
using SanteDB.Client.Configuration.Upstream;
using SanteDB.Client.Disconnected.Services;
using SanteDB.Client.OAuth;
using SanteDB.Client.Tickles;
using SanteDB.Client.Upstream.Management;
using SanteDB.Client.Upstream.Repositories;
using SanteDB.Client.Upstream.Security;
using SanteDB.Client.Upstream;
using SanteDB.Client.UserInterface.Impl;
using SanteDB.Core;
using SanteDB.Core.Applets.Services.Impl;
using SanteDB.Core.Configuration;
using SanteDB.Core.Data.Backup;
using SanteDB.Core.Data;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Privacy;
using SanteDB.Core.Security;
using SanteDB.Core.Services.Impl;
using SanteDB.Security.Certs.BouncyCastle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.Mobile.Configuration
{
    public class MauiClientInitialConfigurationProvider : IInitialConfigurationProvider
    {
        public int Order => int.MinValue;

        public SanteDBConfiguration Provide(SanteDBHostType hostContextType, SanteDBConfiguration configuration)
        {
            var appServiceSection = configuration.GetSection<ApplicationServiceContextConfigurationSection>();
            var instanceName = appServiceSection.InstanceName;
            var localDataPath = AppDomain.CurrentDomain.GetData("DataDirectory")?.ToString();

            if (null == localDataPath)
            {
                throw new ApplicationException("Application bug exists. DataDirectory was not set before configuration provider was called. Ensure the DataDirectory data variable in the app domain is set before the config provider is initialized.");
            }

            appServiceSection.ServiceProviders.AddRange(new List<TypeReferenceConfiguration>() {
                    new TypeReferenceConfiguration(typeof(AesSymmetricCrypographicProvider)),
                    new TypeReferenceConfiguration(typeof(InMemoryTickleService)),
                    new TypeReferenceConfiguration(typeof(DefaultNetworkInformationService)),
                    new TypeReferenceConfiguration(typeof(SHA256PasswordHashingService)),
                    new TypeReferenceConfiguration(typeof(DefaultPolicyDecisionService)),
                    new TypeReferenceConfiguration(typeof(MemoryAdhocCacheService)),
                    new TypeReferenceConfiguration(typeof(AppletLocalizationService)),
                    new TypeReferenceConfiguration(typeof(AppletBusinessRulesDaemon)),
                    new TypeReferenceConfiguration(typeof(DefaultUpstreamManagementService)),
                    new TypeReferenceConfiguration(typeof(DefaultUpstreamIntegrationService)),
                    new TypeReferenceConfiguration(typeof(DefaultUpstreamAvailabilityProvider)),
                    new TypeReferenceConfiguration(typeof(MemoryCacheService)),
                    new TypeReferenceConfiguration(typeof(DefaultThreadPoolService)),
                    new TypeReferenceConfiguration(typeof(MauiInteractionProvider)),
                    new TypeReferenceConfiguration(typeof(MemoryQueryPersistenceService)),
                    new TypeReferenceConfiguration(typeof(FileSystemDispatcherQueueService)),
                    new TypeReferenceConfiguration(typeof(SimplePatchService)),
                    new TypeReferenceConfiguration(typeof(DefaultBackupManager)),
                    new TypeReferenceConfiguration(typeof(AppletBiRepository)),
                    new TypeReferenceConfiguration(typeof(OAuthClient)),
                    new TypeReferenceConfiguration(typeof(MemorySessionManagerService)),
                    new TypeReferenceConfiguration(typeof(UpstreamUpdateManagerService)), // AmiUpdateManager
                    new TypeReferenceConfiguration(typeof(UpstreamIdentityProvider)),
                    new TypeReferenceConfiguration(typeof(UpstreamApplicationIdentityProvider)),
                    new TypeReferenceConfiguration(typeof(UpstreamSecurityChallengeProvider)), // AmiSecurityChallengeProvider
                    new TypeReferenceConfiguration(typeof(UpstreamRoleProviderService)),
                    new TypeReferenceConfiguration(typeof(UpstreamSecurityRepository)),
                    new TypeReferenceConfiguration(typeof(UpstreamRepositoryFactory)),
                    new TypeReferenceConfiguration(typeof(UpstreamPolicyInformationService)),
                    new TypeReferenceConfiguration(typeof(DataPolicyFilterService)),
                    new TypeReferenceConfiguration(typeof(MauiOperatingSystemInfoService)),
                    new TypeReferenceConfiguration(typeof(AppletSubscriptionRepository)),
                    new TypeReferenceConfiguration(typeof(InMemoryPivotProvider)),
                    new TypeReferenceConfiguration(typeof(AuditDaemonService)),
                    new TypeReferenceConfiguration(typeof(DefaultDataSigningService)),
                    new TypeReferenceConfiguration(typeof(DefaultBarcodeProviderService)),
                    new TypeReferenceConfiguration(typeof(FileSystemDispatcherQueueService)),
                    new TypeReferenceConfiguration(typeof(BouncyCastleCertificateGenerator)),
                    new TypeReferenceConfiguration(typeof(RepositoryEntitySource)),
                    new TypeReferenceConfiguration(typeof(FileSystemCdssLibraryRepository)),
                    new TypeReferenceConfiguration(typeof(MauiPlatformSecurityProvider)),
            });

            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("input.name", "simple"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("input.address", "text"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("optional.patient.address.city", "true"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("optional.patient.address.county", "true"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("optional.patient.address.state", "false"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("optional.patient.name.family", "false"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("optional.patient.address.given", "false"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("forbid.patient.address.state", "false"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("forbid.patient.address.county", "true"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("forbid.patient.address.city", "false"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("forbid.patient.address.precinct", "true"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("forbid.patient.name.prefix", "true"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("forbid.patient.name.suffix", "true"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("forbid.patient.name.family", "false"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("forbid.patient.name.given", "false"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("allow.patient.religion", "false"));
            appServiceSection.AppSettings.Add(new AppSettingKeyValuePair("allow.patient.ethnicity", "false"));
            appServiceSection.AppSettings = appServiceSection.AppSettings.OrderBy(o => o.Key).ToList();

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
            UpstreamConfigurationSection upstreamConfiguration = new UpstreamConfigurationSection()
            {
                Credentials = new List<UpstreamCredentialConfiguration>()
                {
                    new UpstreamCredentialConfiguration()
                    {
                        CredentialName = $"Debugee-{macAddress.Replace(" ", "")}",
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

            return configuration;
        }
    }
}
