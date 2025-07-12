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
using Java.Interop;
using Newtonsoft.Json;
using SanteDB.Client.Configuration.Upstream;
using SanteDB.Core;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Services;
using System;
using System.Globalization;
using System.Linq;

#nullable enable

namespace SanteDB.Client.Mobile
{
    public class MauiBrowserInterface : Java.Lang.Object
    {
        readonly MainPage _MainPage;
        private readonly Guid _Magic;
        readonly IConfigurationManager? _ConfigManager;
        private readonly ILocalizationService _LocalizationService;
        private readonly IUpstreamAvailabilityProvider _UpstreamAvailabilityProvider;
        private readonly INetworkInformationService _NetworkInformationService;
        readonly string? _DeviceId;
        readonly string? _ClientId;
        readonly string? _RealmId;
        readonly string? _FacilityId;
        readonly string? _OwnerId;

        static string? _Version;
        private static string? GetAssemblyVersion()
        {
            if (null != _Version)
            {
                return _Version;
            }
            _Version = typeof(MauiBrowserInterface)?.Assembly?.GetName()?.Version?.ToString();
            return _Version;
        }

        public MauiBrowserInterface(IApplicationServiceContext context, MainPage mainPage)
        {
            _ConfigManager = context.GetService<IConfigurationManager>();
            _LocalizationService = context.GetService<ILocalizationService>();
            _UpstreamAvailabilityProvider = context.GetService<IUpstreamAvailabilityProvider>();
            _NetworkInformationService = context.GetService<INetworkInformationService>();

            _MainPage = mainPage;
            _Magic = context.ActivityUuid;
            var upstreamconfig = _ConfigManager?.GetSection<UpstreamConfigurationSection>();

            var devicecredential = upstreamconfig?.Credentials?.FirstOrDefault(c => c.CredentialType == UpstreamCredentialType.Device);
            var appcredential = upstreamconfig?.Credentials?.FirstOrDefault(c => c.CredentialType == UpstreamCredentialType.Application);

            var securityconfig = _ConfigManager?.GetSection<SecurityConfigurationSection>();


            _DeviceId = devicecredential?.CredentialName;
            _ClientId = upstreamconfig?.Realm == null ? null : appcredential?.CredentialName;
            _RealmId = upstreamconfig?.Realm?.DomainName;

            _FacilityId = securityconfig?.GetSecurityPolicy<Guid>(Core.Configuration.SecurityPolicyIdentification.AssignedFacilityUuid).ToString();
            _OwnerId = securityconfig?.GetSecurityPolicy<Guid>(Core.Configuration.SecurityPolicyIdentification.AssignedOwnerUuid).ToString();

        }

        [Export]
        [JavascriptInterface]
        public string GetServiceState()
        {
            var state = new Shared.AppServiceStateResponse
            {
                Ami = IsAdminAvailable(),
                ClientId = _ClientId,
                DeviceId = _DeviceId,
                Hdsi = IsClinicalAvailable(),
                Magic = GetMagic(),
                Online = GetOnlineState(),
                Realm = GetRealm(),
                Version = GetAssemblyVersion(),
                FacilityId = _FacilityId,
                OwnerId = _OwnerId,
            };

            return JsonConvert.SerializeObject(state);
        }

        [Export]
        [JavascriptInterface]
        public bool GetOnlineState() => _NetworkInformationService.IsNetworkAvailable && _NetworkInformationService.IsNetworkConnected;

        [Export]
        [JavascriptInterface]
        public bool IsAdminAvailable() => _UpstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.AdministrationIntegrationService);

        [Export]
        [JavascriptInterface]
        public bool IsClinicalAvailable() => _UpstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.HealthDataService);

        [Export]
        [JavascriptInterface]
        public string? GetClientId() => _ClientId;

        [Export]
        [JavascriptInterface]
        public string? GetAssignedFacilityId() => _FacilityId;

        [Export]
        [JavascriptInterface]
        public string? GetAssignedOwnerId() => _OwnerId;

        [Export]
        [JavascriptInterface]
        public string? GetDeviceId() => _DeviceId;

        [Export]
        [JavascriptInterface]
        public string? GetRealm() => _RealmId;

        [Export]
        [JavascriptInterface]
        public string GetLocale() => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        [Export]
        [JavascriptInterface]
        public void SetLocale(string locale)
        {
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(locale);
        }

        [Export]
        [JavascriptInterface]
        public String GetString(String stringId)
        {
            try
            {
                return _LocalizationService.GetString(stringId);
            }
            catch (Exception e)
            {
                //this.m_tracer.TraceWarning("Error retreiving string {0}", stringId);
                return stringId;
            }
        }

        [Export]
        [JavascriptInterface]
        public string GetMagic() => _Magic.ToString();

        [Export]
        [JavascriptInterface]
        public string? GetVersion() => GetAssemblyVersion();

        [Export]
        [JavascriptInterface]
        public string ScanBarcode()
        {
            return Nito.AsyncEx.AsyncContext.Run(async () => await _MainPage.ScanBarcodeAsync());
        }
    }
}
