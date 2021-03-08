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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SanteDB.DisconnectedClient.Android.Core.Configuration;
using System.IO;
using System.Threading.Tasks;
using Android.Util;
using SanteDB.DisconnectedClient.Android.Core.Diagnostics;
using System.Threading;
using SanteDB.Core.Applets.Model;
using System.Xml.Serialization;
using SanteDB.DisconnectedClient.Android.Core.Services;
using SanteDB.DisconnectedClient;
using System.IO.Compression;
using Android.Content.PM;
using SanteDB.Core.Diagnostics;
using SanteDB;
using SanteDB.DisconnectedClient;
using SanteDB.DisconnectedClient.Configuration;
using SanteDB.DisconnectedClient.Ags;
using SanteDB.DisconnectedClient.Android.Core;
using SanteDB.DisconnectedClient.Services;
using SanteDB.DisconnectedClient.Android.Core.Activities;
using SanteDB.Core.Services;

namespace SanteDBAndroid
{
    [Activity(
        Label = "@string/app_name", 
        Theme = "@style/SanteDB.Splash", 
        MainLauncher = true, 
        Icon = "@mipmap/icon", 
        NoHistory = true, 
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize | ConfigChanges.Mcc | ConfigChanges.Mnc,
        ScreenOrientation = ScreenOrientation.Locked)]
    public class SplashActivity : AndroidActivityBase
    {
        
        // Tracer
        private Tracer m_tracer;

        /// <param name="newConfig">The new device configuration.</param>
        /// <summary>
        /// Configuration changed
        /// </summary>
        public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
        }

        /// <summary>
        /// Progress has changed
        /// </summary>
        private void OnProgressUpdated(Object sender, ApplicationProgressEventArgs e)
        {
            this.RunOnUiThread(() => this.FindViewById<TextView>(Resource.Id.txt_splash_info).Text = String.Format("{0} {1}", e.ProgressText, e.Progress > 0 ? String.Format("({0:0%})", e.Progress) : null));
        }

        /// <summary>
        /// Create the activity
        /// </summary>
        /// <param name="savedInstanceState">Saved instance state.</param>
        protected override void OnCreate(Bundle savedInstanceState)
        {
            
            base.OnCreate(savedInstanceState);

            bool hasLaunched = false;
            this.SetContentView(Resource.Layout.Splash);

            SanteDB.DisconnectedClient.ApplicationContext.Current = null;

            this.FindViewById<TextView>(Resource.Id.txt_splash_version).Text = String.Format("V {0} ({1})",
                typeof(SplashActivity).Assembly.GetName().Version,
                typeof(SplashActivity).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion
            );
            CancellationTokenSource ctSource = new CancellationTokenSource();
            CancellationToken ct = ctSource.Token;
            Task startupWork = new Task(() =>
            {
                if (AndroidApplicationContext.Current == null)
                    if (!this.DoConfigure())
                        ctSource.Cancel();
            }, ct);


            startupWork.ContinueWith(t =>
            {
                if (!ct.IsCancellationRequested)
                {
                    Action doStart = () =>
                    {
                        hasLaunched = true;
                        AndroidApplicationContext.ProgressChanged -= this.OnProgressUpdated;
                        Intent viewIntent = new Intent(this, typeof(AppletActivity));
                        var appletConfig = AndroidApplicationContext.Current.Configuration.GetSection<AppletConfigurationSection>();
                        viewIntent.PutExtra("assetLink", "http://127.0.0.1:9200/");
                        this.StartActivity(viewIntent);
                    };
                    if (AndroidApplicationContext.Current.IsRunning || AndroidApplicationContext.Current.GetService<AgsService>().IsRunning)
                        doStart();
                    else
                    {
                        AndroidApplicationContext.Current.GetService<AgsService>().Started += (oo, oe) =>
                            doStart();

                        // At most wait for 30 seconds for the view to launch
                        AndroidApplicationContext.Current.GetService<IThreadPoolService>().QueueUserWorkItem((oo) =>
                        {
                            Thread.Sleep(30000);
                            if (!hasLaunched && AndroidApplicationContext.Current.Confirm(Resources.GetString(Resource.String.confirm_force_launch)))
                                this.RunOnUiThread(() => doStart());
                        }, null);
                    }
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());


            startupWork.Start();

        }

        /// <summary>
        /// Startup is complete
        /// </summary>
        private bool DoConfigure()
        {

            //this.RunOnUiThread(() => this.FindViewById<TextView>(Resource.Id.txt_splash_info).Text = Resources.GetString(Resource.String.startup));
            AndroidApplicationContext.ProgressChanged += this.OnProgressUpdated;

            try
            {

                if (AndroidApplicationContext.Current != null)
                    return true;

                if (!AndroidApplicationContext.Start(this, this.ApplicationContext, this.Application))
                {

                    CancellationTokenSource ctSource = new CancellationTokenSource();
                    CancellationToken ct = ctSource.Token;

                    Task notifyUserWork = new Task(() =>
                    {

                        try
                        {

                            if (!AndroidApplicationContext.StartTemporary(this, this.ApplicationContext))
                                throw new InvalidOperationException("Cannot start temporary context");

                        }
                        catch (Exception e)
                        {
                            this.m_tracer?.TraceError(e.ToString());
                            ctSource.Cancel();
                            this.ShowException(e);
                        }
                        finally
                        {
                            AndroidApplicationContext.ProgressChanged -= this.OnProgressUpdated;
                        }
                    }, ct);

                    // Now show the configuration screen.
                    notifyUserWork.ContinueWith(t =>
                    {
                        if (!ct.IsCancellationRequested)
                        {
                           
                            Action doStart = () =>
                            {
                                Intent viewIntent = new Intent(this, typeof(AppletActivity));
                                viewIntent.PutExtra("assetLink", "http://127.0.0.1:9200/");
                                viewIntent.PutExtra("continueTo", typeof(SplashActivity).AssemblyQualifiedName);
                                this.StartActivity(viewIntent);
                            };
                            if (AndroidApplicationContext.Current.IsRunning)
                                doStart();
                            else
                                AndroidApplicationContext.Current.Started += (oo, oe) =>
                                    doStart();
                        }
                    }, TaskScheduler.Current);

                    notifyUserWork.Start();
                    return false;
                }
                else
                {

                    this.m_tracer = Tracer.GetTracer(this.GetType());
                }


                return true;
            }
            catch (AppDomainUnloadedException)
            {
                this.Finish();
                return false;
            }
            catch (Exception e)
            {

                this.ShowException(e);
                return false;
            }

        }

        /// <summary>
        /// Shows an exception message box
        /// </summary>
        /// <param name="e">E.</param>
        private void ShowException(Exception e)
        {
            this.m_tracer?.TraceError("Error during startup: {0}", e);
            Log.Error("FATAL", e.ToString());
            while (e is TargetInvocationException)
                e = e.InnerException;

            var result = false;
            AutoResetEvent reset = new AutoResetEvent(false);
            UserInterfaceUtils.ShowConfirm(this,
                (s, a) =>
                {
                    result = true;
                    reset.Set();
                },
                (s,a) =>
                {
                    result = false;
                    reset.Set();
                },
                Resources.GetString(Resource.String.err_startup), e is TargetInvocationException ? e.InnerException.Message : e.Message);

            reset.WaitOne();

            var bksvc = AndroidApplicationContext.Current.GetService<IBackupService>();
            bksvc.Backup(result ? BackupMedia.Public : BackupMedia.Private);
            File.Delete(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "SanteDB.config"));
            Directory.Delete(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData)), true);
            this.Finish();

        }
    }
}

