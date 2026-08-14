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
using Android.Content;
using Android.Webkit;
using Firely.Fhir.Packages;
using Javax.Security.Auth;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Layouts;
using SanteDB.Client.UserInterface;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Services;
using System.Diagnostics.Tracing;
using static Android.Provider.ContactsContract.CommonDataKinds;

namespace SanteDB.Client.Mobile
{
    internal class MauiChromeClient : WebChromeClient
    {

        /// <summary>
        /// Tracer 
        /// </summary>
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(MauiChromeClient));
        private readonly ILocalizationService m_localizationService;
        private readonly Context m_context;

        public MauiChromeClient(Context context)
        {
            this.m_localizationService = ApplicationServiceContext.Current.GetService<ILocalizationService>();
            this.m_context = context;
        }

        /// <summary>
        /// Capture javascript console messages and forward them to the tracer
        /// </summary>
        /// <param name="consoleMessage"></param>
        /// <returns></returns>
        public override bool OnConsoleMessage(ConsoleMessage consoleMessage)
        {
            var retVal = base.OnConsoleMessage(consoleMessage);

            // Start off verbose
            EventLevel eventLevel = EventLevel.Verbose;
            if (consoleMessage.InvokeMessageLevel() == Android.Webkit.ConsoleMessage.MessageLevel.Error)
            {
                //Toast.MakeText(this.m_context, "This applet reported an error", ToastLength.Long).Show();
                eventLevel = EventLevel.Error;
            }
            else if (consoleMessage.InvokeMessageLevel() == Android.Webkit.ConsoleMessage.MessageLevel.Warning)
                eventLevel = EventLevel.Warning;
            else if (consoleMessage.InvokeMessageLevel() == Android.Webkit.ConsoleMessage.MessageLevel.Log)
                eventLevel = EventLevel.Informational;

            this.m_tracer.TraceEvent(eventLevel, "[{0}:{1}] {2}", consoleMessage.SourceId(), consoleMessage.LineNumber(), consoleMessage.Message());
            return retVal;
        }

        /// <summary>
        /// We override this and take over the rendering of CONFIRM since we don't want the tablet to read : "127.0.0.1:port Says"
        /// </summary>
        public override bool OnJsConfirm(WebView view, string url, string message, JsResult result)
        {
            // JF - Use the native Android handlers as the Maui dialog builders have a cross-thread access issue setting result
            var alert = new Android.App.AlertDialog.Builder(this.m_context)
                .SetMessage(message)
                .SetTitle(this.m_localizationService.GetString("ui.alert.confirm"))
                .SetPositiveButton(this.m_localizationService.GetString("ui.action.ok"), (o, e) => result.Confirm())
                .SetNegativeButton(this.m_localizationService.GetString("ui.action.cancel"), (o, e) => result.Cancel())
                .SetCancelable(false);
            alert.Create().Show();
            return true;
        }

        /// <summary>
        /// We override this to take over rendering of ALERT since we don't want the tablet to read: "127.0.0.1:port Says"
        /// </summary>
        public override bool OnJsAlert(WebView view, string url, string message, JsResult result)
        {
            // JF - Use the native Android handlers as the Maui dialog builders have a cross-thread access issue setting result
            var alert = new Android.App.AlertDialog.Builder(this.m_context)
                .SetMessage(message)
                .SetTitle(this.m_localizationService.GetString("ui.alert.alert"))
                .SetPositiveButton(this.m_localizationService.GetString("ui.action.ok"), (o, e) => result.Confirm())
                .SetCancelable(false);
            alert.Create().Show();
            return true;
        }


    }
}