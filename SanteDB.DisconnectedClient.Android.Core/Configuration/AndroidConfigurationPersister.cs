/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 * 
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
 * User: fyfej
 * Date: 2017-9-1
 */
using System;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Xml.Serialization;
using SanteDB.DisconnectedClient.Configuration;
using Android.Content.Res;
using System.Collections.Generic;
using System.Security.Cryptography;
using SanteDB.DisconnectedClient.Configuration.Data;
using SanteDB.DisconnectedClient.Security;
using SanteDB.DisconnectedClient.Data;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using AndroidOS = Android.OS;
using SanteDB.DisconnectedClient.Caching;
using SanteDB.Core.Services.Impl;
using SanteDB.Core.Protocol;
using SanteDB.DisconnectedClient.Android.Core.Net;
using SanteDB.DisconnectedClient.Android.Core.Diagnostics;
using SanteDB.DisconnectedClient.Android.Core.Services;
using SanteDB.Cdss.Xml;
using SanteDB.DisconnectedClient.Data.Warehouse;
using SanteDB.DisconnectedClient.Tickler;
using SharpCompress.Compressors.LZMA;
using SharpCompress.Compressors.BZip2;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Services;
using SanteDB.Core.Configuration;
using SanteDB.DisconnectedClient.Ags;
using SanteDB.DisconnectedClient.Net;
using SanteDB.DisconnectedClient.Rules;
using SanteDB.DisconnectedClient.Threading;
using SanteDB.DisconnectedClient.Services.Local;
using SanteDB.DisconnectedClient.Services;
using SanteDB.DisconnectedClient.Diagnostics;
using SanteDB.DisconnectedClient.Http;
using SanteDB.BI.Services.Impl;
using SanteDB.Core.Applets.Services.Impl;
using SanteDB.DisconnectedClient.Synchronization;
using SanteDB.DisconnectedClient.Security.Session;
using SanteDB.Core.Security.Audit;
using SanteDB.DisconnectedClient.Security.Remote;
using SanteDB.DisconnectedClient.Android.Core.Services.Barcoding;

namespace SanteDB.DisconnectedClient.Android.Core.Configuration
{
    /// <summary>
    /// Configuration manager for the application
    /// </summary>
    public class AndroidConfigurationPersister : IConfigurationPersister
    {

        private const int PROVIDER_RSA_FULL = 1;

        // Tracer
        private Tracer m_tracer;

        // Configuration
        private SanteDBConfiguration m_configuration;

        // Configuration path
        private readonly String m_configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SanteDB.config");

        /// <summary>
        /// Returns true if SanteDB is configured
        /// </summary>
        /// <value><c>true</c> if this instance is configured; otherwise, <c>false</c>.</value>
        public bool IsConfigured
        {
            get
            {
                return File.Exists(this.m_configPath);
            }
        }

        /// <summary>
        /// Get a bare bones configuration
        /// </summary>
        public SanteDBConfiguration GetDefaultConfiguration()
        {
            // TODO: Bring up initial settings dialog and utility
            var retVal = new SanteDBConfiguration();
            
            // Initial Applet configuration
            AppletConfigurationSection appletSection = new AppletConfigurationSection()
            {
                AppletDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "applets"),
                StartupAsset = "org.santedb.uicore",
                Security = new AppletSecurityConfiguration()
                {
                    AllowUnsignedApplets = true,
                    TrustedPublishers = new List<string>() { "82C63E1E9B87578D0727E871D7613F2F0FAF683B", "4326A4421216AC254DA93DC61B93160B08925BB1" }
                }
            };

            // Initial applet style
            ApplicationConfigurationSection appSection = new ApplicationConfigurationSection()
            {
                Style = StyleSchemeType.Dark,
                UserPrefDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "userpref"),
                Cache = new CacheConfiguration()
                {
                    MaxAge = new TimeSpan(0, 5, 0).Ticks,
                    MaxSize = 1000,
                    MaxDirtyAge = new TimeSpan(0, 20, 0).Ticks,
                    MaxPressureAge = new TimeSpan(0, 2, 0).Ticks
                }
            };

            // App service 
            var appServiceSection = new ApplicationServiceContextConfigurationSection()
            {
                ThreadPoolSize = Environment.ProcessorCount * 2,
                ServiceProviders = new List<TypeReferenceConfiguration>() {
                    new TypeReferenceConfiguration(typeof(AesSymmetricCrypographicProvider)),
                    new TypeReferenceConfiguration(typeof(MemoryTickleService)),
                    new TypeReferenceConfiguration(typeof(DefaultPolicyDecisionService)),
                    new TypeReferenceConfiguration(typeof(AndroidNetworkInformationService)),
                    new TypeReferenceConfiguration(typeof(BusinessRulesDaemonService)),
                    new TypeReferenceConfiguration(typeof(AgsService)),
                    new TypeReferenceConfiguration(typeof(MemoryCacheService)),
                    new TypeReferenceConfiguration(typeof(SanteDBThreadPool)),
                    new TypeReferenceConfiguration(typeof(SimpleCarePlanService)),
                    new TypeReferenceConfiguration(typeof(MemorySessionManagerService)),
                    new TypeReferenceConfiguration(typeof(AmiUpdateManager)),
                    new TypeReferenceConfiguration(typeof(AppletClinicalProtocolRepository)),
                    new TypeReferenceConfiguration(typeof(MemoryQueryPersistenceService)),
                    new TypeReferenceConfiguration(typeof(SimpleQueueFileProvider)),
                    new TypeReferenceConfiguration(typeof(SimplePatchService)),
                    new TypeReferenceConfiguration(typeof(AuditDaemonService)),
                    new TypeReferenceConfiguration(typeof(AndroidBackupService)),
                    new TypeReferenceConfiguration(typeof(AndroidAppletManagerService)),
                    new TypeReferenceConfiguration(typeof(AppletBiRepository)),
                    new TypeReferenceConfiguration(typeof(AndroidOperatingSystemInfoService)),
                    new TypeReferenceConfiguration(typeof(AppletSubscriptionRepository)),
                    new TypeReferenceConfiguration(typeof(InMemoryPivotProvider)),
                    new TypeReferenceConfiguration(typeof(AndroidGeoLocationService)),
                    new TypeReferenceConfiguration(typeof(AmiSecurityChallengeProvider)),
                    new TypeReferenceConfiguration(typeof(InMemoryPivotProvider)),
                    new TypeReferenceConfiguration(typeof(DefaultDataSigningService)),
                    new TypeReferenceConfiguration(typeof(QrBarcodeGenerator))
                }
            };

            // Security configuration
            var wlan = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(o => o.NetworkInterfaceType == NetworkInterfaceType.Ethernet && o.Description.StartsWith("wlan"));
            String macAddress = Guid.NewGuid().ToString().Substring(0, 6);
            if (wlan != null)
                macAddress = wlan.GetPhysicalAddress().ToString();
            //else 

            SecurityConfigurationSection secSection = new SecurityConfigurationSection()
            {
                DeviceName = String.Format("{0}-{1}", AndroidOS.Build.Model, macAddress).Replace(" ", ""),
                AuditRetention = new TimeSpan(30, 0, 0, 0, 0),
                DomainAuthentication = DomainClientAuthentication.Inline
            };

            // Device key
            var certificate = X509CertificateUtils.FindCertificate(X509FindType.FindBySubjectName, StoreLocation.LocalMachine, StoreName.My, String.Format("DN={0}.mobile.santedb.org", macAddress));
            secSection.DeviceSecret = certificate?.Thumbprint;

            // Rest Client Configuration
            ServiceClientConfigurationSection serviceSection = new ServiceClientConfigurationSection()
            {
                RestClientType = typeof(RestClient)
            };

            // Trace writer
#if DEBUG
            DiagnosticsConfigurationSection diagSection = new DiagnosticsConfigurationSection()
            {
                TraceWriter = new System.Collections.Generic.List<TraceWriterConfiguration>() {
                    new TraceWriterConfiguration () {
                        Filter = System.Diagnostics.Tracing.EventLevel.LogAlways,
                        InitializationData = "SanteDB",
                        TraceWriter = typeof(AndroidLogTraceWriter)
                    },
                    new TraceWriterConfiguration() {
                        Filter = System.Diagnostics.Tracing.EventLevel.Informational,
                        InitializationData = "SanteDB",
                        TraceWriter = typeof(FileTraceWriter)
                    }
                }
            };
#else
            DiagnosticsConfigurationSection diagSection = new DiagnosticsConfigurationSection()
            {
                TraceWriter = new List<TraceWriterConfiguration>() {
                    new TraceWriterConfiguration () {
                        Filter = System.Diagnostics.Tracing.EventLevel.Warning,
                        InitializationData = "SanteDB",
                        TraceWriter = typeof(FileTraceWriter)
                    }
                }
            };
#endif

            retVal.Sections.Add(appServiceSection);
            retVal.Sections.Add(appletSection);
            retVal.Sections.Add(diagSection);
            retVal.Sections.Add(appSection);
            retVal.Sections.Add(secSection);
            retVal.Sections.Add(serviceSection);
            retVal.AddSection(AgsService.GetDefaultConfiguration());
            retVal.Sections.Add(new AuditAccountabilityConfigurationSection()
            {
                AuditFilters = new List<AuditFilterConfiguration>()
                {
                    // Audit any failure - No matter which event
                    new AuditFilterConfiguration(null, null, SanteDB.Core.Auditing.OutcomeIndicator.EpicFail | SanteDB.Core.Auditing.OutcomeIndicator.MinorFail | SanteDB.Core.Auditing.OutcomeIndicator.SeriousFail, true, true),
                    // Audit anything that creates, reads, or updates data
                    new AuditFilterConfiguration(SanteDB.Core.Auditing.ActionType.Create | SanteDB.Core.Auditing.ActionType.Read | SanteDB.Core.Auditing.ActionType.Update | SanteDB.Core.Auditing.ActionType.Delete, null, null, true, true)
                }
            });

            retVal.Sections.Add(new DcDataConfigurationSection()
            {
                MainDataSourceConnectionStringName = "santeDbData",
                MessageQueueConnectionStringName = "santeDbQueue"
            });

            retVal.Sections.Add(new SynchronizationConfigurationSection()
            {
                PollInterval = new TimeSpan(0, 5, 0),
                ForbiddenResouces = new List<SynchronizationForbidConfiguration>()
                {
                    new SynchronizationForbidConfiguration(SynchronizationOperationType.All, "DeviceEntity"),
                    new SynchronizationForbidConfiguration(SynchronizationOperationType.All, "ApplicationEntity"),
                    new SynchronizationForbidConfiguration(SynchronizationOperationType.All, "Concept"),
                    new SynchronizationForbidConfiguration(SynchronizationOperationType.All, "ConceptSet"),
                    new SynchronizationForbidConfiguration(SynchronizationOperationType.All, "Place"),
                    new SynchronizationForbidConfiguration(SynchronizationOperationType.All, "ReferenceTerm"),
                    new SynchronizationForbidConfiguration(SynchronizationOperationType.All, "AssigningAuthority"),
                    new SynchronizationForbidConfiguration(SynchronizationOperationType.Obsolete, "UserEntity")
                }
            });

            return retVal;
        }


        /// <summary>
        /// Creates a new instance of the configuration manager with the specified configuration file
        /// </summary>
        public AndroidConfigurationPersister()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SanteDB.DisconnectedClient.Android.Core.Configuration.ConfigurationManager"/> class.
        /// </summary>
        /// <param name="config">Config.</param>
        public AndroidConfigurationPersister(SanteDBConfiguration config)
        {
            this.m_configuration = config;
        }

        /// <summary>
        /// Load the configuration
        /// </summary>
        public SanteDBConfiguration Load()
        {
            // Configuration exists?
            if (this.IsConfigured)
                using (var fs = File.OpenRead(this.m_configPath))
                {
                    return SanteDBConfiguration.Load(fs);
                }
            else
                return this.GetDefaultConfiguration();
        }


        /// <summary>
        /// Save the specified configuration
        /// </summary>
        /// <param name="config">Config.</param>
        public void Save(SanteDBConfiguration config)
        {
            try
            {
                this.m_tracer?.TraceInfo("Saving configuration to {0}...", this.m_configPath);
                if (!Directory.Exists(Path.GetDirectoryName(this.m_configPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(this.m_configPath));

                using (FileStream fs = File.Create(this.m_configPath))
                {
                    config.Save(fs);
                    fs.Flush();
                }
            }
            catch (Exception e)
            {
                this.m_tracer?.TraceError(e.ToString());
                throw;
            }
        }


        /// <summary>
        /// Application data directory
        /// </summary>
        public string ApplicationDataDirectory
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            }
        }


        /// <summary>
        /// Backup the configuration
        /// </summary>
        public void Backup(SanteDBConfiguration configuration)
        {
            using (var lzs = new BZip2Stream(File.Create(Path.ChangeExtension(this.m_configPath, "bak.bz2")), SharpCompress.Compressors.CompressionMode.Compress, false))
                configuration.Save(lzs);
        }

        /// <summary>
        /// True if the configuration has a backup
        /// </summary>
        public bool HasBackup()
        {
            return File.Exists(Path.ChangeExtension(this.m_configPath, "bak.bz2"));
        }

        /// <summary>
        /// Restore the configuration
        /// </summary>
        public SanteDBConfiguration Restore()
        {
            using (var lzs = new BZip2Stream(File.OpenRead(Path.ChangeExtension(this.m_configPath, "bak.bz2")), SharpCompress.Compressors.CompressionMode.Decompress, false))
            {
                var retVal = SanteDBConfiguration.Load(lzs);
                this.Save(retVal);
                ApplicationContext.Current.ConfigurationManager.Reload();
                return retVal;
            }
        }

    }
}

