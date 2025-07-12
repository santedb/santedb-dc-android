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
using Android.Webkit;
using CommunityToolkit.Maui.Views;
using Hl7.Fhir.ElementModel.Types;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Handlers;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using static Android.Webkit.WebSettings;

namespace SanteDB.Client.Mobile
{
    public partial class MainPage : ContentPage
    {
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(MainPage));

        private long m_logicalScroll;
        int count = 0;
        private string _HttpMagic;
        readonly MauiApplicationContext _ApplicationContext;

        public MainPage(string sourceUrl, string httpMagicValue, MauiApplicationContext applicationContext)
        {
            try
            {
                _ApplicationContext = applicationContext;

                this.m_tracer.TraceInfo("Setting Status Callbacks");
                _ApplicationContext.GetInteractionProvider().SetStatusCallback = (task, message, progress) =>
                {
                    Nito.AsyncEx.AsyncContext.Run(() => NotificationBar.ShowOrUpdateNotificationAsync(task, message, progress));
                };

                this.m_tracer.TraceInfo("Initializing Main View...");
                InitializeComponent();

                _HttpMagic = httpMagicValue;
                WebView.HandlerChanged += WebView_HandlerChanged;
                WebView.Source = sourceUrl;
            }
            catch(Exception ex)
            {
                this.m_tracer.TraceError("Error initializing main view {0}", ex);
            }
        }

        /// <summary>
        /// Handles when the MAUI web view changes its handler to android
        /// </summary>
        private void WebView_HandlerChanged(object sender, EventArgs e)
        {
            try
            {
                var handler = WebView.Handler;

                if ((handler?.PlatformView) is Android.Webkit.WebView awebview)
                {
                    this.m_tracer.TraceInfo("Initializing the trace handler web view");
                    awebview.Settings.UserAgentString = $"SanteDB-{_HttpMagic}"; // MAGIC is required so the API will not work when another app attempts to access 127.0.0.1
                    awebview.Settings.JavaScriptEnabled = true; 
                    awebview.Settings.SetGeolocationEnabled(true);
                    awebview.Settings.BuiltInZoomControls = false; // We don't allow pinch and zoom
                    awebview.Settings.DisplayZoomControls = false;

                    awebview.Settings.PluginsEnabled = false; // When this is commented out - sometimes the web view takes upwards of 30 seconds to initialize on Android versions < 30 - 
                    awebview.Settings.JavaScriptCanOpenWindowsAutomatically = false; 
                    awebview.Settings.SetRenderPriority(RenderPriority.High); // When commented out - sometimes on web views on Android Versions < 28 the scrolling experience is jittery
                    awebview.Settings.SetSupportMultipleWindows(false); 
                    awebview.Settings.SetAppCacheEnabled(true); 
                    awebview.SetScrollContainer(true);
                    awebview.ScrollBarStyle = Android.Views.ScrollbarStyles.InsideOverlay;

                    var browserinterface = new MauiBrowserInterface(ApplicationServiceContext.Current, this); 
                    awebview.AddJavascriptInterface(browserinterface, "__sdb_bridge"); // Adds the Javascript bridge service (allows JS to interact with the C#)
                    awebview.SetWebChromeClient(typeof(MauiChromeClient).CreateInjected() as WebChromeClient);// Redirects the CONSOLE logs from the browser to our tracer system
#if !DISABLE_WEBVIEW_DEBUGGING
                    //TODO: Additional platform initialization
                    Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
#endif
                }
                else
                {
                    throw new InvalidOperationException("Platform not supported");
                }
            }
            catch(Exception ex)
            {
                this.m_tracer.TraceError("Error initializing the web view - {0}", ex);
            }
        }

        /// <summary>
        /// Invokes the barcode scanning function of the app. This method will automatically dispatch the call to the main thread.
        /// </summary>
        /// <returns>A task whos eventual result is the result from scanning the barcode.</returns>
        internal Task<string> ScanBarcodeAsync()
        {
            if (!MainThread.IsMainThread)
            {
                return MainThread.InvokeOnMainThreadAsync(ScanBarcodeInternalAsync);
            }
            else
            {
                return ScanBarcodeInternalAsync();
            }
        }

        /// <summary>
        /// Internal implementation of the barcode scanning. This method checks the permission status and will request permission if possible.
        /// </summary>
        /// <returns></returns>
        private async Task<string> ScanBarcodeInternalAsync()
        {
            var permission = await Permissions.CheckStatusAsync<Permissions.Camera>();

            if (!(permission == PermissionStatus.Granted))
            {
                permission = await Permissions.RequestAsync<Permissions.Camera>();
            }


            if (permission == PermissionStatus.Granted)
            {
                Controls.BarcodeScannerPopup popup = new Controls.BarcodeScannerPopup();

                var scanresult = (await this.ShowPopupAsync(popup))?.ToString();

                return scanresult;
            }
            else
            {
                return string.Empty;
            }
        }

    }
}