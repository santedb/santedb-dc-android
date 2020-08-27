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

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SanteDB.Core.Applets.Model;
using System.IO;
using SanteDB.DisconnectedClient.Configuration;
using SanteDB.DisconnectedClient.Services;
using SanteDB.DisconnectedClient;
using SanteDB.Core.Applets;

namespace SanteDB.DisconnectedClient.Android.Core.Services
{
    /// <summary>
    /// Represents an android asset manager
    /// </summary>
    public class AndroidAppletManagerService : LocalAppletManagerService
    {
        /// <summary>
        /// Ctor
        /// </summary>
        public AndroidAppletManagerService()
        {
            this.m_appletCollection.Resolver = this.ResolveAppletAsset;
            this.m_appletCollection.CachePages = true;
        }

        /// <summary>
        /// Resolve asset
        /// </summary>
        public object ResolveAppletAsset(AppletAsset navigateAsset)
        {
            String itmPath = System.IO.Path.Combine(
                                        ApplicationContext.Current.Configuration.GetSection<AppletConfigurationSection>().AppletDirectory,
                                        "assets",
                                        navigateAsset.Manifest.Info.Id,
                                        navigateAsset.Name);

            if (navigateAsset.MimeType == "text/javascript" ||
                        navigateAsset.MimeType == "text/css" ||
                        navigateAsset.MimeType == "application/json" ||
                        navigateAsset.MimeType == "text/xml")
            {
                var script = File.ReadAllText(itmPath);
                return script;
            }
            else
                using (MemoryStream response = new MemoryStream())
                using (var fs = File.OpenRead(itmPath))
                {
                    int br = 8096;
                    byte[] buffer = new byte[8096];
                    while (br == 8096)
                    {
                        br = fs.Read(buffer, 0, 8096);
                        response.Write(buffer, 0, br);
                    }

                    return response.ToArray();
                }
        }

    }
}