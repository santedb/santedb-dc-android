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
