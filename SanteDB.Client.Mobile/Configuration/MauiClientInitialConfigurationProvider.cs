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
using SanteDB.Client.Configuration;
using SanteDB.Client.Configuration.Upstream;
using SanteDB.Core;
using SanteDB.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.Mobile.Configuration
{
    public class MauiClientInitialConfigurationProvider : IInitialConfigurationProvider
    {
        public int Order => int.MinValue;

        public SanteDBConfiguration Provide(SanteDBHostType hostContextType, SanteDBConfiguration configuration)
        {
            // Security configuration
            var wlan = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(o => o.NetworkInterfaceType == NetworkInterfaceType.Ethernet || o.Description.StartsWith("wlan"));
            String macAddress = Guid.NewGuid().ToString();
            if (wlan != null)
            {
                macAddress = wlan.GetPhysicalAddress().ToString();
            }

            // Upstream default configuration
            UpstreamConfigurationSection upstreamConfiguration = new UpstreamConfigurationSection()
            {
                Credentials = new List<UpstreamCredentialConfiguration>()
                {
                    new UpstreamCredentialConfiguration()
                    {
                        CredentialName = $"Debugee-{macAddress.Replace(" ", "")}",
                        Conveyance = UpstreamCredentialConveyance.Secret,
                        CredentialType = UpstreamCredentialType.Device
                    },
                    new UpstreamCredentialConfiguration()
                    {
                        CredentialName = "org.santedb.disconnected_client.android",
                        CredentialSecret = "C5B645B7D30A4E7E81A1C3D8B0E28F4C",
                        Conveyance = UpstreamCredentialConveyance.Secret,
                        CredentialType = UpstreamCredentialType.Application
                    }
                }
            };

            configuration.AddSection(upstreamConfiguration);

            return configuration;
        }
    }
}
