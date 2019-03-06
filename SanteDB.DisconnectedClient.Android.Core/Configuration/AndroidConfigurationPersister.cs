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
using SanteDB.DisconnectedClient.Core.Configuration;
using Android.Content.Res;
using System.Collections.Generic;
using System.Security.Cryptography;
using SanteDB.DisconnectedClient.Core.Configuration.Data;
using SanteDB.DisconnectedClient.Core.Security;
using SanteDB.DisconnectedClient.Core.Data;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using AndroidOS = Android.OS;
using SanteDB.DisconnectedClient.Core.Caching;
using SanteDB.Core.Services.Impl;
using SanteDB.Core.Protocol;
using SanteDB.DisconnectedClient.Android.Core.Net;
using SanteDB.DisconnectedClient.Android.Core.Diagnostics;
using SanteDB.DisconnectedClient.Android.Core.Services;
using SanteDB.Cdss.Xml;
using SanteDB.ReportR;
using SanteDB.DisconnectedClient.Core.Data.Warehouse;
using SanteDB.DisconnectedClient.Core.Tickler;
using SharpCompress.Compressors.LZMA;
using SharpCompress.Compressors.BZip2;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Services;
using SanteDB.Core.Configuration;
using SanteDB.DisconnectedClient.Ags;
using SanteDB.DisconnectedClient.Xamarin.Security;
using SanteDB.DisconnectedClient.Xamarin.Net;
using SanteDB.DisconnectedClient.Xamarin.Rules;
using SanteDB.DisconnectedClient.Xamarin.Threading;
using SanteDB.DisconnectedClient.Xamarin.Configuration;
using SanteDB.DisconnectedClient.Core.Services.Local;
using SanteDB.DisconnectedClient.Xamarin.Services;
using SanteDB.DisconnectedClient.Xamarin.Diagnostics;
using SanteDB.DisconnectedClient.Xamarin.Http;
using SanteDB.DisconnectedClient.Core;

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
                    TrustedPublishers = new List<string>() { "82C63E1E9B87578D0727E871D7613F2F0FAF683B" }
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
                    new TypeReferenceConfiguration(typeof(AndroidBackupService)),
                    new TypeReferenceConfiguration(typeof(AndroidAppletManagerService)),
                    new TypeReferenceConfiguration(typeof(ReportExecutor)),
                    new TypeReferenceConfiguration(typeof(AppletReportRepository)),
                    new TypeReferenceConfiguration(typeof(AndroidGeoLocationService))
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
                AuditRetention = new TimeSpan(30, 0, 0, 0, 0)
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
			DiagnosticsConfigurationSection diagSection = new DiagnosticsConfigurationSection () {
				TraceWriter = new System.Collections.Generic.List<TraceWriterConfiguration> () {
					new TraceWriterConfiguration () { 
						Filter = System.Diagnostics.Tracing.EventLevel.LogAlways,
						InitializationData = "SanteDB",
						TraceWriter = new LogTraceWriter (System.Diagnostics.Tracing.EventLevel.LogAlways, "SanteDB")
					},
					new TraceWriterConfiguration() {
						Filter = System.Diagnostics.Tracing.EventLevel.LogAlways,
						InitializationData = "SanteDB",
						TraceWriter = new FileTraceWriter(System.Diagnostics.Tracing.EventLevel.LogAlways, "SanteDB")
					},
                    new TraceWriterConfiguration() {
                        Filter = System.Diagnostics.Tracing.EventLevel.LogAlways,
                        InitializationData = "SanteDB",
                        TraceWriter = new AndroidLogTraceWriter(System.Diagnostics.Tracing.EventLevel.LogAlways, "SanteDB")
                    }
                }
			};
#else
            DiagnosticsConfigurationSection diagSection = new DiagnosticsConfigurationSection()
            {
                TraceWriter = new List<TraceWriterConfiguration>() {
                    new TraceWriterConfiguration() {
                        Filter = System.Diagnostics.Tracing.EventLevel.Warning,
                        InitializationData = "SanteDB",
                        TraceWriter = new FileTraceWriter(System.Diagnostics.Tracing.EventLevel.LogAlways, "SanteDB")
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
            retVal.Sections.Add(new SynchronizationConfigurationSection()
            {
                PollInterval = new TimeSpan(0, 5, 0)
            });

            foreach (var t in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).SelectMany(a => a.ExportedTypes).Where(t => typeof(IInitialConfigurationProvider).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface))
                retVal = (Activator.CreateInstance(t) as IInitialConfigurationProvider).Provide(retVal);

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
            using (var lzs = new BZip2Stream(File.Create(Path.ChangeExtension(this.m_configPath, "bak.bz2")), SharpCompress.Compressors.CompressionMode.Compress))
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
            using (var lzs = new BZip2Stream(File.OpenRead(Path.ChangeExtension(this.m_configPath, "bak.bz2")), SharpCompress.Compressors.CompressionMode.Decompress))
            {
                var retVal = SanteDBConfiguration.Load(lzs);
                this.Save(retVal);
                ApplicationContext.Current.ConfigurationManager.Reload();
                return retVal;
            }
        }

    }
}

