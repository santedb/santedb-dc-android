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
using CommunityToolkit.Maui.Views;
using Hl7.Fhir.ElementModel.Types;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Handlers;
using SanteDB.Core;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using static Android.Webkit.WebSettings;

namespace SanteDB.Client.Mobile
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        private string _HttpMagic;
        readonly MauiApplicationContext _ApplicationContext;

        public MainPage(string sourceUrl, string httpMagicValue, MauiApplicationContext applicationContext)
        {
            _ApplicationContext = applicationContext;

            _ApplicationContext.GetInteractionProvider().SetStatusCallback = (task, message, progress) =>
            {
                Nito.AsyncEx.AsyncContext.Run(() => NotificationBar.ShowOrUpdateNotificationAsync(task, message, progress));
            };

            InitializeComponent();

            _HttpMagic = httpMagicValue;

            WebView.HandlerChanged += WebView_HandlerChanged;

            WebView.Source = sourceUrl;
        }

        private void WebView_HandlerChanged(object sender, EventArgs e)
        {
            var handler = WebView.Handler;

            if ((handler?.PlatformView) is Android.Webkit.WebView awebview)
            {
                awebview.Settings.UserAgentString = $"SanteDB-{_HttpMagic}";
                awebview.Settings.JavaScriptEnabled = true;
                awebview.Settings.SetGeolocationEnabled(true);
                awebview.Settings.BuiltInZoomControls = false;
                awebview.Settings.DisplayZoomControls = false;
                //awebview.Settings.PluginsEnabled = false;
                awebview.Settings.JavaScriptCanOpenWindowsAutomatically = false;
                //awebview.Settings.SetRenderPriority(RenderPriority.High);
                awebview.Settings.SetSupportMultipleWindows(false);
                //awebview.Settings.SetAppCacheEnabled(true);
                awebview.SetScrollContainer(true);
                awebview.ScrollBarStyle = Android.Views.ScrollbarStyles.InsideOverlay;
                var browserinterface = new MauiBrowserInterface(ApplicationServiceContext.Current, this);
                awebview.AddJavascriptInterface(browserinterface, "__sdb_bridge");

                if (Android.OS.Build.VERSION.SdkInt > Android.OS.BuildVersionCodes.Kitkat)
                {
                    awebview.SetLayerType(Android.Views.LayerType.Hardware, null);
                }

                awebview.SetWebChromeClient(new MauiChromeClient());
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

        private void UIRefreshView_Refreshing(object sender, EventArgs e)
        {
            try
            {
                if (WebView.Handler.PlatformView is Android.Webkit.WebView awebview &&
                    awebview.ScrollY == 0)
                {
                    WebView.Reload();
                }
            }
            finally
            {
                UIRefreshView.IsRefreshing = false;
            }
        }
    }
}