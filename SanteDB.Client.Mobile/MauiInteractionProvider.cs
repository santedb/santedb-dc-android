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
using Microsoft.Maui.Controls;
using SanteDB.Client.UserInterface;
using System;
using System.Threading;

namespace SanteDB.Client.Mobile
{
    internal class MauiInteractionProvider : IUserInterfaceInteractionProvider
    {
        public string ServiceName => "SanteDB Multiplatform Interaction Provider";

        // Interaction event callback
        private readonly ManualResetEventSlim m_interactionResetEvent = new ManualResetEventSlim(false);

        /// <summary>
        /// JF- Allows the Maui application to push the currently visible content page
        /// </summary>
        internal Page CurrentPage => _Application.MainPage;

        readonly Application _Application;

        public MauiInteractionProvider(Application application, StartupPage startupPage)
        {
            _Application = application;
        }

        public void Alert(string message)
        {
            // JF- TODO: Fix this to look up from the i18n 
            this.m_interactionResetEvent.Reset();
            Application.Current!.MainPage!.Dispatcher.Dispatch(async () =>
            {
                await Application.Current!.MainPage!.DisplayAlert("Alert", message, "OK");
                this.m_interactionResetEvent.Set();
            });
            this.m_interactionResetEvent.Wait();
        }

        public bool Confirm(string message)
        {
            
            // JF - TODO: Fix this to look up from the i18n
            bool result = false;
            this.m_interactionResetEvent.Reset();
            Application.Current!.MainPage!.Dispatcher.Dispatch(async () =>
            {
                result = await Application.Current!.MainPage!.DisplayAlert("Confirm", message, "OK", "Cancel");
                this.m_interactionResetEvent.Set();
            });
            this.m_interactionResetEvent.Wait();
            return result;
        }

        public string Prompt(string message, bool maskEntry = false)
        {
            string result = String.Empty;
            this.m_interactionResetEvent.Reset();
            Application.Current!.MainPage!.Dispatcher.Dispatch(async () =>
            {
                result = await Application.Current!.MainPage!.DisplayPromptAsync("Prompt", message);
                // JF - TODO: Fix this to look up from the i18n
                this.m_interactionResetEvent.Set();
            });
            this.m_interactionResetEvent.Wait();
            return result;

        }

        public void SetStatus(string statusText, float progressIndicator)
            => SetStatus(string.Empty, statusText, progressIndicator);

        public void SetStatus(string taskIdentifier, string statusText, float progressIndicator)
        {
            
            if (this.CurrentPage is StartupPage sp && sp.IsStarting)
            {
                sp.SetStatus(taskIdentifier, statusText, progressIndicator);
            }
            else if (null != SetStatusCallback)
            {
                SetStatusCallback(taskIdentifier, statusText, progressIndicator);
            }

        }

        public Action<string, string, float> SetStatusCallback { get; set; }
    }
}
