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
 * Date: 2023-8-24
 */
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.Audit;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using System;
using System.Threading.Tasks;

namespace SanteDB.Client.Mobile
{
    public partial class App : Application
    {

        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(App));

        public App()
        {
            InitializeComponent();
            RegisterGlobalExceptionHandlers();
        }

        private void RegisterGlobalExceptionHandlers()
        {
            // Handle unhandled exceptions on the main thread
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => HandleException((Exception)e.ExceptionObject);
            // Handle unobserved task exceptions (async code)
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                HandleException(e.Exception);
                e.SetObserved();
            };
        }

        private async void HandleException(Exception ex)
        {
            // Log the exception
            this.m_tracer.TraceError("---------- FATAL ERROR -----------\r\n{0}", ex);
        }

        protected override void OnStart()
        {
            base.OnStart();
        }


        protected override void OnResume()
        {
            base.OnResume();
                this.m_tracer.TraceInfo("Application resumed");
        }

        protected override void OnSleep()
        {
            base.OnSleep();
            this.m_tracer.TraceInfo("Application slept/paused");

        }

        /// <summary>
        /// App shell
        /// </summary>
        protected override Window CreateWindow(IActivationState activationState)
        {
            return new Window(new AppShell());
        }
    }
}