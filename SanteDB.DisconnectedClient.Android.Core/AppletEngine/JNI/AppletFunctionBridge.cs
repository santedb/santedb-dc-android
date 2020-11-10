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
using System.Linq;
using SanteDB.DisconnectedClient.Interop;

using System;
using Android.Webkit;
using Java.Interop;
using Android.Widget;
using Android.Content;
using Org.Json;
using Android.App;
using Android.Content.Res;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using SanteDB.Core.Applets.Model;
using A = Android;
using System.Reflection;
using SanteDB.DisconnectedClient.Configuration;
using System.Globalization;
using System.Security.Principal;
using SanteDB.DisconnectedClient.Services;
using SanteDB.DisconnectedClient;
using SanteDB.DisconnectedClient.Services.Model;
using System.Text;
using SanteDB.Core.Applets;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Diagnostics;
using SanteDB.DisconnectedClient;
using SanteDB.Core.Services;
using SanteDB.Core.Interfaces;
using SanteDB.Core;
using SanteDB.DisconnectedClient.Security;
using System.Security.Cryptography;
using Android.Print;

namespace SanteDB.DisconnectedClient.Android.Core.AppletEngine.JNI
{

    /// <summary>
    /// Applet functions which are related to javascript
    /// </summary>
    public class AppletFunctionBridge : Java.Lang.Object
    {

        // Cached menus
        private Dictionary<IPrincipal, String> m_cachedMenus = new Dictionary<IPrincipal, String>();

        private KeyValuePair<String, float> m_applicationStatus;



        // Context
        private Context m_context;
        private AppletWebView m_view;
        private Tracer m_tracer = Tracer.GetTracer(typeof(AppletFunctionBridge));
        private Assembly m_appAssembly;
        private bool m_zxingInitialized;

        /// <summary>
        /// Gets the context of the function
        /// </summary>
        /// <param name="context">Context.</param>
        public AppletFunctionBridge(Context context, AppletWebView view)
        {
            ApplicationContext.ProgressChanged += (o, e) => this.m_applicationStatus = new KeyValuePair<string, float>(e.ProgressText, e.Progress);
            this.m_context = context;
            this.m_view = view;
        }

        /// <summary>
        /// Prints the current view
        /// </summary>
        [Export]
        [JavascriptInterface]
        public void Print()
        {
            var tickleService = ApplicationServiceContext.Current.GetService<ITickleService>();

            try
            {

                PrintManager printManager = (PrintManager)this.m_context.GetSystemService(Context.PrintService);
                PrintDocumentAdapter printAdapter = this.m_view.CreatePrintDocumentAdapter($"SanteDBPrint-{DateTime.Now.ToString("o")}");
                PrintAttributes.Builder builder = new PrintAttributes.Builder();
                builder.SetMediaSize(PrintAttributes.MediaSize.IsoA4);
                PrintJob printJob = printManager.Print("SanteDB Print Job", printAdapter, builder.Build());

                if (printJob.IsCompleted)
                {
                    tickleService.SendTickle(new Tickler.Tickle(Guid.Empty, Tickler.TickleType.Information, this.GetString("org.santedb.core.print.success")));
                }
                else if (printJob.IsFailed)
                {
                    tickleService.SendTickle(new Tickler.Tickle(Guid.Empty, Tickler.TickleType.Information, this.GetString("org.santedb.core.print.fail")));
                }
            }
            catch(Exception e) { 
                tickleService.SendTickle(new Tickler.Tickle(Guid.Empty, Tickler.TickleType.Information, this.GetString("org.santedb.core.print.fail")));
                this.m_tracer.TraceError("Error Printing: {0}", e);
            }
        }

        /// <summary>
        /// Get the specified reference set
        /// </summary>
        [Export]
        [JavascriptInterface]
        public String GetDataAsset(String dataId)
        {
            try
            {
                dataId = String.Format("data/{0}", dataId);
                return Convert.ToBase64String(ApplicationContext.Current.GetService<IAppletManagerService>().Applets.RenderAssetContent(ApplicationContext.Current.GetService<IAppletManagerService>().Applets.SelectMany(o => o.Assets).FirstOrDefault(o => o.Name == dataId), CultureInfo.CurrentUICulture.TwoLetterISOLanguageName));
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error getting asset {0} : {1}", dataId, e);
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the online status
        /// </summary>
        [Export]
        [JavascriptInterface]
        public bool GetOnlineState()
        {
            try
            {
                return ApplicationContext.Current?.GetService<INetworkInformationService>()?.IsNetworkAvailable == true;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error online state: {0}", e);
                return true;
            }
        }


        /// <summary>
        /// Gets the online status
        /// </summary>
        [Export]
        [JavascriptInterface]
        public bool IsClinicalAvailable()
        {
            try
            {
                return ApplicationContext.Current.GetService<IClinicalIntegrationService>().IsAvailable();
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error clinical state: {0}", e);
                return true;
            }
        }

        /// <summary>
        /// Gets the online status
        /// </summary>
        [Export]
        [JavascriptInterface]
        public bool IsAdminAvailable()
        {
            try
            {
                return ApplicationContext.Current.GetService<IAdministrationIntegrationService>().IsAvailable();
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error admin state: {0}", e);
                return true;
            }
        }

        /// <summary>
        /// Send log file
        /// </summary>
        [Export]
        [JavascriptInterface]
        public String GetStatus()
        {
            try
            {
                return String.Format("[\"{0}\",{1}]", this.m_applicationStatus.Key, this.m_applicationStatus.Value);
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Error getting app status: {0}", e);
                return String.Empty;
            }
        }


        /// <summary>
        /// Send log file
        /// </summary>
        /// <returns></returns>
        [Export]
        [JavascriptInterface]
        public void SendLog(String logId)
        {
            try
            {
                String logFileBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "log", logId);
                var email = new Intent(A.Content.Intent.ActionSend);
                email.PutExtra(A.Content.Intent.ExtraEmail, new string[] { "medic@mohawkcollege.ca" });
                email.PutExtra(A.Content.Intent.ExtraSubject, "Mobile Application Log File");
                email.SetType("message/rfc822");

                email.PutExtra(A.Content.Intent.ExtraText, File.ReadAllText(logFileBase));

                this.m_context.StartActivity(email);
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error sending log file: {0}", e);
            }
        }

        /// <summary>
        /// Gets the registered template form
        /// </summary>
        /// <param name="templateId"></param>
        [JavascriptInterface]
        [Export]
        public String GetTemplateForm(String templateId)
        {
            try
            {
                return AndroidApplicationContext.Current.GetService<IAppletManagerService>().Applets.GetTemplateDefinition(templateId)?.Form;
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Error getting template {0}: {1}", templateId, e);
                return String.Empty;
            }
        }


        /// <summary>
        /// Gets the registered template form
        /// </summary>
        /// <param name="templateId"></param>
        [JavascriptInterface]
        [Export]
        public String GetTemplateView(String templateId)
        {
            try
            {
                return AndroidApplicationContext.Current.GetService<IAppletManagerService>().Applets.GetTemplateDefinition(templateId)?.View;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error getting template view {0}: {1}", templateId, e);
                return String.Empty;
            }
        }

        /// <summary>
        /// Get all templates
        /// </summary>
        [Export]
        [JavascriptInterface]
        public String GetTemplates()
        {
            try
            {
                return $"[{String.Join(",", ApplicationContext.Current.GetService<IAppletManagerService>().Applets.SelectMany(o => o.Templates).Where(o => o.Public).Select(o => $"\"{o.Mnemonic}\""))}]";
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Error getting template list : {0}", e);
                return String.Empty;
            }
}


        /// <summary>
        /// Get log files
        /// </summary>
        /// <returns></returns>
        [Export]
        [JavascriptInterface]
        public String GetLogFiles()
        {
            try
            {
                String logFileBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "log");
                List<String> files = new List<string>();
                foreach (var f in Directory.GetFiles(logFileBase))
                    files.Add(Path.GetFileName(f));
                return JniUtil.ToJson(files);
            }
            catch (Exception ex)
            {
                this.m_tracer.TraceError("Error getting log files: {0}", ex);
                return null;
            }
        }

        /// <summary>
        /// Shows the specified message as a toast
        /// </summary>
        [Export]
        [JavascriptInterface]
        public void ShowToast(String toastText)
        {
            try
            {
                Toast.MakeText(this.m_context, toastText, ToastLength.Short).Show();
            }
            catch { }
        }

        /// <summary>
        /// Gets the current asset title
        /// </summary>
        [Export]
        [JavascriptInterface]
        public string GetCurrentAssetTitle()
        {
            try
            {
                return (this.m_view as AppletWebView).ThreadSafeTitle;
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Error getting asset title: {0}", e);
                return String.Empty;
            }
        }

        /// <summary>
        /// Get version name
        /// </summary>
        [JavascriptInterface]
        [Export]
        public String GetVersion()
        {
            try
            {
                if (this.m_appAssembly == null)
                    this.m_appAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(o => o.DefinedTypes.Any(t => t.Name == "SplashActivity")) ?? typeof(SanteDB.DisconnectedClient.Services.IAdministrationIntegrationService).Assembly;

                return String.Format("{0} ({1})", this.m_appAssembly.GetName().Version,
                    this.m_appAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Error getting application version: {0}", e);
                return String.Empty;
            }
        }

        /// <summary>
        /// Create new UUID
        /// </summary>
        [Export]
        [JavascriptInterface]
        public String NewGuid()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Get version name
        /// </summary>
        [JavascriptInterface]
        [Export]
        public String GetService(String serviceName)
        {
            try
            {
                Type serviceType = Type.GetType(serviceName);
                if (serviceType == null)
                    return ApplicationContext.Current.GetService<IServiceManager>().GetServices().FirstOrDefault(
                        o => o.GetType().GetInterfaces().Any(i => i.Name == serviceName) ||
                        o.GetType().Name == serviceName || o.GetType().BaseType.Name == serviceName
                    )?.GetType().Name;
                else
                    return ApplicationContext.Current.GetService(serviceType)?.GetType().Name;
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Error getting service {0} : {1}", serviceName, e);
                return String.Empty;
            }
        }


        [Export]
        [JavascriptInterface]
        public string GetNetworkState()
        {
            try
            {
                var networkInformationService = ApplicationContext.Current.GetService<INetworkInformationService>();

                return networkInformationService.IsNetworkAvailable.ToString();
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Error getting network status: {0}", e);
                return String.Empty;
            }
        }

        /// <summary>
        /// Navigate the specified appletId and context.
        /// </summary>
        /// <param name="appletId">Applet identifier.</param>
        /// <param name="context">Context.</param>
        [Export]
        [JavascriptInterface]
        public void Navigate(String appletId, String context)
        {
            try
            {
                // TODO: Parameters
                Application.SynchronizationContext.Post(_ =>
                {
                    this.m_view.LoadUrl(String.Format("app://santedb.org/applet/{0}/", appletId));
                }, null);
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Error navigating to {0} : {1}", appletId, e);
                
            }
        }

        /// <summary>
        /// Get the specified string
        /// </summary>
        /// <returns>The string.</returns>
        /// <param name="stringId">String identifier.</param>
        [Export]
        [JavascriptInterface]
        public String GetString(String stringId)
        {
            try
            {
                var appletResource = AndroidApplicationContext.Current.GetService<IAppletManagerService>().Applets.GetStrings(this.GetLocale()).FirstOrDefault(o => o.Key == stringId).Value;
                if (appletResource != null)
                    return appletResource;
                else
                {
                    var androidStringId = this.m_context.Resources.GetIdentifier(stringId, "string", this.m_context.PackageName);
                    if (androidStringId > 0)
                        return this.m_context.Resources.GetString(androidStringId);
                    else
                        return stringId;
                }
            }
            catch (Exception e)
            {
                //this.m_tracer.TraceWarning("Error retreiving string {0}", stringId);
                return stringId;
            }
        }

        /// <summary>
        /// Go back home
        /// </summary>
        [Export]
        [JavascriptInterface]
        public void Back()
        {
            Application.SynchronizationContext.Post(_ =>
            {
                if (this.m_view.CanGoBack())
                    this.m_view.GoBack();
                else
                    (this.m_context as Activity).Finish();
            }, null);
        }


        /// <summary>Close the applet</summary>
        [Export]
        [JavascriptInterface]
        public void Close()
        {
            try
            {
                Application.SynchronizationContext.Post(_ =>
                {
                    this.m_tracer.TraceInfo("Received close() request");
                    ApplicationContext.Current.Stop();
                    AppletCollection.ClearCaches();
                    ApplicationContext.Current.Exit();
                //(this.m_context as Activity).Finish();
            }, null);
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Error closing application: {0}", e);
            }
        }

        /// <summary>
        /// Performs a barcode scan
        /// </summary>
        [Export]
        [JavascriptInterface]
        public String BarcodeScan()
        {
            try
            {

                var permService = ApplicationServiceContext.Current.GetService<IOperatingSystemSecurityService>();
                if (!permService.HasPermission(PermissionType.Camera) && !permService.RequestPermission(PermissionType.Camera))
                    return String.Empty;
                if (!this.m_zxingInitialized && (ApplicationContext.Current as AndroidApplicationContext).AndroidApplication != null)
                {
                    ZXing.Mobile.MobileBarcodeScanner.Initialize((ApplicationContext.Current as AndroidApplicationContext).AndroidApplication);
                    this.m_zxingInitialized = true;
                }
                var scanner = new ZXing.Mobile.MobileBarcodeScanner();
                String retVal = String.Empty;
                var result = scanner.Scan().ContinueWith((o) => retVal = o.Result?.Text);
                result.Wait();

                // TODO: Send this to a barcode validator / parser
                return retVal;
            }
            catch (Exception e)
            {
                this.ShowToast(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Get the locale of the user interface
        /// </summary>
        [Export]
        [JavascriptInterface]
        public String GetLocale()
        {
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;//this.m_context.Resources.Configuration.Locale.Language;
        }

        /// <summary>
        /// Get the applet strings
        /// </summary>
        [Export]
        [JavascriptInterface]
        public String GetStrings(String locale)
        {
            try
            {
                var strings = AndroidApplicationContext.Current.GetService<IAppletManagerService>().Applets.GetStrings(locale);

                using (StringWriter sw = new StringWriter())
                {
                    sw.Write("{");
                    foreach (var itm in strings)
                    {
                        sw.Write("\"{0}\":\"{1}\",", itm.Key, itm.Value);
                    }
                    sw.Write("\"locale\":\"{0}\" }}", locale);
                    return sw.ToString();
                }
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("error getting strings for {0}: {1}", locale, e);
                return String.Empty;
            }
        }

        /// <summary>
        /// Set the locale of the application
        /// </summary>
        [Export]
        [JavascriptInterface]
        public String SetLocale(String locale)
        {
            try
            {
                this.m_tracer.TraceVerbose("Setting locale to {0}", locale);
                this.m_context.Resources.Configuration.SetLocale(new Java.Util.Locale(locale));
                CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(locale);
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error setting locale to {0}: {1}", locale, e);
            }
            return this.m_context.Resources.Configuration.Locale.Language;
        }

        /// <summary>
        /// Get magic
        /// </summary>
        [Export]
        [JavascriptInterface]
        public String GetMagic()
        {
            return ApplicationContext.Current.ExecutionUuid.ToString();
        }
    }
}

