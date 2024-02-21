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
using SanteDB.Client.UserInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.Mobile
{
    internal class MauiInteractionProvider : IUserInterfaceInteractionProvider
    {
        public string ServiceName => "SanteDB Multiplatform Interaction Provider";

        StartupPage _StartupPage;

        public MauiInteractionProvider(StartupPage startupPage)
        {
            _StartupPage = startupPage;
        }

        public void Alert(string message)
        {
            
        }

        public bool Confirm(string message)
        {
            return false;
        }

        public string Prompt(string message, bool maskEntry = false)
        {
            return null;
        }

        public void SetStatus(string statusText, float progressIndicator)
            => SetStatus(string.Empty, statusText, progressIndicator);

        public void SetStatus(string taskIdentifier, string statusText, float progressIndicator)
        {
            if (_StartupPage.IsStarting)
            {
                _StartupPage.SetStatus(taskIdentifier, statusText, progressIndicator);
            }
            else if (null != SetStatusCallback)
            {
                SetStatusCallback(taskIdentifier, statusText, progressIndicator);
            }

        }

        public Action<string, string, float> SetStatusCallback { get; set; }
    }
}
