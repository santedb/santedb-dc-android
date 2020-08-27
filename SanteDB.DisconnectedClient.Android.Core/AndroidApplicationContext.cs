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
using Android.App;
using Android.Runtime;
using Android.Util;
using SanteDB.Core;
using SanteDB.Core.Applets;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Model.EntityLoader;
using SanteDB.Core.Protocol;
using SanteDB.Core.Services;
using SanteDB.DisconnectedClient.Android.Core.Configuration;
using SanteDB.DisconnectedClient.Configuration;
using SanteDB.DisconnectedClient.Configuration.Data;
using SanteDB.DisconnectedClient.Security;
using SanteDB.DisconnectedClient.Services;
using SanteDB.DisconnectedClient;
using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Xml.Serialization;
using System.IO.Compression;
using System.Threading;
using Android.OS;
using SanteDB.DisconnectedClient.Android.Core.Services;
using SanteDB.Core.Diagnostics;
using A = Android;
using SanteDB.DisconnectedClient;
using SanteDB.Core.Configuration;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Security;
using SanteDB.DisconnectedClient.Android.Core.Resources;
using System.Collections.Generic;
using SanteDB.DisconnectedClient.Configuration;
using SanteDB.DisconnectedClient.Services;
using SanteDB.DisconnectedClient.Configuration.Data;

namespace SanteDB.DisconnectedClient.Android.Core
{
    /// <summary>
    /// Represents an application context for Xamarin Android
    /// </summary>
    public class AndroidApplicationContext : ApplicationContext
    {

        // Current activity
        private A.Content.Context m_currentActivity;

        // The application
        private static readonly SanteDB.Core.Model.Security.SecurityApplication c_application = new SanteDB.Core.Model.Security.SecurityApplication()
        {
            ApplicationSecret = "C5B645B7D30A4E7E81A1C3D8B0E28F4C",
            Key = Guid.Parse("5248ea19-369d-4071-8947-413310872b7e"),
            Name = "org.santedb.disconnected_client.android"
        };

        /// <summary>
        /// Gets the host type
        /// </summary>
        public override SanteDBHostType HostType => SanteDBHostType.Client;

        /// <summary>
        /// Static CTOR bind to global handlers to log errors
        /// </summary>
        /// <value>The current.</value>
        static AndroidApplicationContext()
        {
            Console.WriteLine("Binding global exception handlers");
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (AndroidApplicationContext.Current != null)
                {
                    Tracer tracer = Tracer.GetTracer(typeof(AndroidApplicationContext));
                    tracer.TraceEvent(EventLevel.Critical, "Uncaught exception: {0}", e.ExceptionObject.ToString());
                }
                Console.WriteLine("AndroidApplicationContext::UncaughtException", e.ExceptionObject.ToString());
            };
            AndroidEnvironment.UnhandledExceptionRaiser += (s, e) =>
            {
                if (AndroidApplicationContext.Current != null)
                {
                    Tracer tracer = Tracer.GetTracer(typeof(AndroidApplicationContext));
                    tracer.TraceEvent(EventLevel.Critical, "Uncaught exception: {0}", e.Exception.ToString());
                }
                Console.WriteLine("AndroidApplicationContext::UncaughtException", e.Exception.ToString());
                e.Handled = true;
            };
        }

        /// <summary>
        /// Android application context
        /// </summary>
        public AndroidApplicationContext() : base(new AndroidConfigurationPersister())
        {

        }

        /// <summary>
        /// Fired when no configuration is found
        /// </summary>
        public static event EventHandler NoConfiguration;

        /// <summary>
        /// Gets or sets the current activity
        /// </summary>
        public A.Content.Context CurrentActivity {
            get
            {
                return this.m_currentActivity;
            }
            set
            {
                if (value != this.m_currentActivity && this.m_currentActivity != null)
                    this.RemoveServiceProvider(this.m_currentActivity);
                this.m_currentActivity = value;
                this.AddServiceProvider(this.m_currentActivity);
            }
        }

        /// <summary>
        /// Start the application context
        /// </summary>
        public static bool Start(A.Content.Context launcherActivity, A.Content.Context context, A.App.Application application)
        {
            var retVal = new AndroidApplicationContext();
            retVal.Context = context;
            retVal.AndroidApplication = application;

            // Not configured
            if (!retVal.ConfigurationPersister.IsConfigured)
            {
                NoConfiguration?.Invoke(null, EventArgs.Empty);
                return false;
            }
            else
            { // load configuration
                try
                {

                    try
                    {
                        // Set master application context
                        ApplicationServiceContext.Current = ApplicationContext.Current = retVal;
                        //retVal.AddServiceProvider(typeof(ConfigurationManager));
                        retVal.CurrentActivity = launcherActivity;
                        retVal.ConfigurationPersister.Backup(retVal.Configuration);
                    }
                    catch
                    {
                        if (retVal.ConfigurationPersister.HasBackup() && retVal.Confirm(Strings.err_configuration_invalid_restore_prompt))
                        {
                            retVal.ConfigurationPersister.Restore();
                            retVal.ConfigurationManager.Reload();
                        }
                        else
                            throw;
                    }
                    retVal.AddServiceProvider(typeof(AndroidBackupService));

                    // Is there a backup, and if so, does the user want to restore from that backup?
                    var backupSvc = retVal.GetService<IBackupService>();
                    if (backupSvc.HasBackup(BackupMedia.Public) &&
                        retVal.ConfigurationManager.GetAppSetting("ignore.restore") == null &&
                        retVal.Confirm(Strings.locale_confirm_restore))
                    {
                        backupSvc.Restore(BackupMedia.Public);
                    }
                    // Ignore restoration
                    retVal.Configuration.GetSection<ApplicationServiceContextConfigurationSection>().AppSettings.Add(new AppSettingKeyValuePair()
                    {
                        Key = "ignore.restore",
                        Value = "true"
                    });

                    // Add tracers
                    retVal.m_tracer = Tracer.GetTracer(typeof(AndroidApplicationContext));
                    foreach (var tr in retVal.Configuration.GetSection<DiagnosticsConfigurationSection>().TraceWriter)
                        Tracer.AddWriter(Activator.CreateInstance(tr.TraceWriter, tr.Filter, tr.InitializationData) as TraceWriter, tr.Filter);

                    // Load configured applets
                    var configuredApplets = retVal.Configuration.GetSection<AppletConfigurationSection>().Applets;
                    retVal.SetProgress(context.GetString(Resource.String.startup_configuration), 0.2f);
                    var appletManager = retVal.GetService<IAppletManagerService>();

                    // Load all user-downloaded applets in the data directory
                    foreach (var appletInfo in configuredApplets)// Directory.GetFiles(this.m_configuration.GetSection<AppletConfigurationSection>().AppletDirectory)) {
                        try
                        {
                            retVal.m_tracer.TraceInfo("Loading applet {0}", appletInfo);
                            String appletPath = Path.Combine(retVal.Configuration.GetSection<AppletConfigurationSection>().AppletDirectory, appletInfo.Id);

                            if (!File.Exists(appletPath)) // reinstall
                            {
                                retVal.Configuration.GetSection<AppletConfigurationSection>().Applets.Clear();
                                retVal.ConfigurationPersister.Save(retVal.Configuration);
                                retVal.Alert(Strings.locale_restartRequired);
                                throw new AppDomainUnloadedException();
                            }

                            // Load
                            using (var fs = File.OpenRead(appletPath))
                            {
                                AppletManifest manifest = AppletManifest.Load(fs);
                                // Is this applet in the allowed applets

                                // public key token match?
                                if (appletInfo.PublicKeyToken != manifest.Info.PublicKeyToken)
                                {
                                    retVal.m_tracer.TraceWarning("Applet {0} failed validation", appletInfo);
                                    ; // TODO: Raise an error
                                }

                                appletManager.LoadApplet(manifest);
                            }
                        }
                        catch (AppDomainUnloadedException) { throw; }
                        catch (Exception e)
                        {
                            retVal.m_tracer.TraceError("Applet Load Error: {0}", e);
                            if (retVal.Confirm(String.Format(Strings.err_applet_corrupt_reinstall, appletInfo.Id)))
                            {
                                String appletPath = Path.Combine(retVal.Configuration.GetSection<AppletConfigurationSection>().AppletDirectory, appletInfo.Id);
                                if (File.Exists(appletPath))
                                    File.Delete(appletPath);
                            }
                            else
                            {
                                retVal.m_tracer.TraceError("Loading applet {0} failed: {1}", appletInfo, e.ToString());
                                throw;
                            }
                        }

                    AndroidApplicationContext.InstallAppletAssets(retVal);

                    // Set the entity source
                    EntitySource.Current = new EntitySource(retVal.GetService<IEntitySourceProvider>());
                    ApplicationServiceContext.Current = ApplicationContext.Current;

                    // Ensure data migration exists
                    if (retVal.ConfigurationManager.Configuration.GetSection<DcDataConfigurationSection>().ConnectionString.Count > 0)
                        try
                        {
                            // If the DB File doesn't exist we have to clear the migrations
                            var dbPath = retVal.ConfigurationManager.GetConnectionString("santeDbData").GetComponent("dbfile");
                            if (!File.Exists(dbPath))
                            {
                                retVal.m_tracer.TraceWarning("Can't find the SanteDB database at {0}, will re-install all migrations",dbPath);
                                retVal.Configuration.GetSection<DcDataConfigurationSection>().MigrationLog.Entry.Clear();
                            }
                            retVal.SetProgress(context.GetString(Resource.String.startup_data), 0.6f);

                            ConfigurationMigrator migrator = new ConfigurationMigrator();
                            migrator.Ensure(true);

                        }
                        catch (Exception e)
                        {
                            retVal.m_tracer.TraceError(e.ToString());
                            throw;
                        }
                        finally
                        {
                            retVal.ConfigurationPersister.Save(retVal.Configuration);
                        }

                    // Start daemons
                    ApplicationContext.Current.GetService<IUpdateManager>().AutoUpdate();
                    retVal.GetService<IThreadPoolService>().QueueNonPooledWorkItem(o => { retVal.Start(); }, null);

                }
                catch (Exception e)
                {
                    retVal.m_tracer?.TraceError(e.ToString());
                    //ApplicationContext.Current = null;
                    AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
                    throw;
                }

                return true;
            }
        }

        /// <summary>
        /// Install applets contained in the APK
        /// </summary>
        private static void InstallAppletAssets(AndroidApplicationContext context)
        {
            var appletManager = context.GetService<IAppletManagerService>();

            // Are we going to deploy applets
            // Upgrade applets from our app manifest
            foreach (var itm in context.Context.Assets.List("Applets"))
            {
                try
                {
                    context.m_tracer.TraceVerbose("Loading {0}", itm);
                    AppletPackage pkg = AppletPackage.Load(context.Context.Assets.Open(String.Format("Applets/{0}", itm)));

                    // Write data to assets directory
#if !DEBUG
                            if (appletManager.GetApplet(pkg.Meta.Id) == null || new Version(appletManager.GetApplet(pkg.Meta.Id).Info.Version) < new Version(pkg.Meta.Version))
#endif
                    appletManager.Install(pkg, true);
                }
                catch (Exception e)
                {
                    context.m_tracer?.TraceError(e.ToString());
                    context.Alert(String.Format(Strings.err_apk_applet_error, e.Message));
                }
            }
        }

        /// <summary>
        /// Starts the application context using in-memory default configuration for the purposes of
        /// configuring the software
        /// </summary>
        /// <returns><c>true</c>, if temporary was started, <c>false</c> otherwise.</returns>
        public static bool StartTemporary(A.Content.Context launcherActivity, A.Content.Context context)
        {
            try
            {
                var retVal = new AndroidApplicationContext();

                retVal.Context = context;
                retVal.SetProgress(context.GetString(Resource.String.startup_setup), 0);
                //retVal.ThreadDefaultPrincipal = AuthenticationContext.SystemPrincipal;
                ApplicationServiceContext.Current = ApplicationContext.Current = retVal ;
                retVal.CurrentActivity = launcherActivity;

                // Add tracers
                retVal.m_tracer = Tracer.GetTracer(typeof(AndroidApplicationContext));
                foreach (var tr in retVal.Configuration.GetSection<DiagnosticsConfigurationSection>().TraceWriter)
                    Tracer.AddWriter(Activator.CreateInstance(tr.TraceWriter, tr.Filter, tr.InitializationData) as TraceWriter, tr.Filter);

                //retVal.ThreadDefaultPrincipal = AuthenticationContext.SystemPrincipal;

                AndroidApplicationContext.InstallAppletAssets(retVal);
                retVal.Start();
                return true;
            }
            catch (Exception e)
            {
                Log.Error("SanteDB 0118 999 881 999 119 7253", e.ToString());
                return false;
            }
        }


        #region implemented abstract members of ApplicationContext

        /// <summary>
        /// Gets or sets the android application
        /// </summary>
        public Application AndroidApplication { get; private set; }

        /// <summary>
        /// Gets the application information for the currently running application.
        /// </summary>
        /// <value>The application.</value>
        public override SanteDB.Core.Model.Security.SecurityApplication Application
        {
            get
            {
                return c_application;
            }
        }

        // <summary>
        /// Gets the current context
        /// </summary>
        public A.Content.Context Context { get; set; }

        /// <summary>
        /// Gets the device information for the currently running device
        /// </summary>
        /// <value>The device.</value>
        public override SanteDB.Core.Model.Security.SecurityDevice Device
        {
            get
            {
                // TODO: Load this from configuration
                return new SanteDB.Core.Model.Security.SecurityDevice()
                {
                    Name = this.Configuration.GetSection<SecurityConfigurationSection>().DeviceName,
                    DeviceSecret = this.Configuration.GetSection<SecurityConfigurationSection>().DeviceSecret
                };
            }
        }

        /// <summary>
        /// Gets the allowed synchronization modes
        /// </summary>
        public override SynchronizationMode Modes
        {
            get
            {
                return SynchronizationMode.Online | SynchronizationMode.Sync;
            }
        }

        /// <summary>
        /// Close 
        /// </summary>
        public override void Exit()
        {
            A.App.Application.SynchronizationContext.Post(_ =>
            {
                this.m_tracer.TraceWarning("Restarting application context");
                ApplicationContext.Current.Stop();
                (this.CurrentActivity as Activity).Finish();
            }, null);
        }

        /// <summary>
        /// Confirm the alert
        /// </summary>
        public override bool Confirm(string confirmText)
        {
            ManualResetEventSlim evt = new ManualResetEventSlim(false);
            bool result = false;
            
            A.App.Application.SynchronizationContext.Post(_ =>
            {
                var alertDialogBuilder = new AlertDialog.Builder(this.CurrentActivity)
                        .SetMessage(confirmText)
                        .SetCancelable(false)
                        .SetPositiveButton(Strings.locale_confirm, (sender, args) =>
                        {
                            result = true;
                            evt.Set();
                        })
                        .SetNegativeButton(Strings.locale_cancel, (sender, args) =>
                        {
                            result = false;
                            evt.Set();
                        });

                alertDialogBuilder.Create().Show();
            }, null);

            evt.Wait();
            return result;
        }


        /// <summary>
        /// show toast
        /// </summary>
        public override void ShowToast(String message)
        {
            //if (Looper.MyLooper() == null)
            //{
            //    Looper.Prepare();
            //}

            
            A.Widget.Toast.MakeText(this.CurrentActivity, message, A.Widget.ToastLength.Long);
        }

        /// <summary>
        /// Show an alert
        /// </summary>
        public override void Alert(string alertText)
        {
            //AutoResetEvent evt = new AutoResetEvent(false);

            //A.App.Application.SynchronizationContext.Post(_ =>
            //{

            //    var alertDialogBuilder = new AlertDialog.Builder(this.CurrentActivity)
            //             .SetMessage(alertText)
            //            .SetCancelable(false)
            //            .SetPositiveButton(Strings.locale_confirm, (sender, args) =>
            //            {
            //                evt.Set();
            //            });

            //    alertDialogBuilder.Create().Show();
            //}, null);

            //evt.WaitOne();
        }

        /// <summary>
        /// Output performanc log info
        /// </summary>
        public override void PerformanceLog(string className, string methodName, string tagName, TimeSpan counter)
        {
            Log.Info("SanteDB_PERF", $"{className}.{methodName}@{tagName} - {counter}");
        }

        #endregion implemented abstract members of ApplicationContext

        /// <summary>
        /// Provides a security key which is unique to the device
        /// </summary>
        public override byte[] GetCurrentContextSecurityKey()
        {
#if NOCRYPT
            return null;
#else
            var androidId = A.Provider.Settings.Secure.GetString(this.Context.ContentResolver, A.Provider.Settings.Secure.AndroidId);
            if (String.IsNullOrEmpty(androidId))
            {
                this.m_tracer.TraceWarning("Android ID cannot be found, databases will not be encrypted");
                return null; // can't encrypt
            }
            else
                return System.Text.Encoding.UTF8.GetBytes(androidId);
#endif
        }

        /// <summary>
        /// Get all types from the app domain
        /// </summary>
        public override IEnumerable<Type> GetAllTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => a.ExportedTypes);
        }
    }
}