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
using Microsoft.Maui.Handlers;
using SanteDB.Core;
using System.Diagnostics;

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
                Dispatcher.DispatchAsync(async () =>
                {
                    await NotificationBar.ShowOrUpdateNotificationAsync(task, message, progress);
                });
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

                var browserinterface = new MauiBrowserInterface(ApplicationServiceContext.Current, this);
                awebview.AddJavascriptInterface(browserinterface, "__sdb_bridge");

                //TODO: Additional platform initialization


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
    }
}