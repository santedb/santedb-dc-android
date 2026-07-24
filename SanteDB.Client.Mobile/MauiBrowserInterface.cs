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
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using SanteDB.Client.Configuration.Upstream;
using SanteDB.Core;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace SanteDB.Client.Mobile
{
    public class MauiBrowserInterface : Java.Lang.Object 
    {
        readonly MainPage _MainPage;
        private readonly Guid _Magic;
        private Shared.AppServiceStateResponse m_stateObject;
        private object m_stateLock = new object();
        private bool m_isCheckingStatus = false;

        

        readonly IConfigurationManager? _ConfigManager;
        private readonly ILocalizationService _LocalizationService;
        private readonly IUpstreamAvailabilityProvider _UpstreamAvailabilityProvider;
        private readonly INetworkInformationService _NetworkInformationService;
        readonly string? _DeviceId;
        readonly string? _ClientId;
        readonly string? _RealmId;
        readonly string? _FacilityId;
        readonly string? _OwnerId;
        private readonly Timer _StateMonitorTimer;
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

            // We want to background the availability call because of the timeout
            this.BackgroundStateMonitor(null);
            _StateMonitorTimer = new Timer(this.BackgroundStateMonitor, null, 0, 5000);
            
        }

        private void BackgroundStateMonitor(object e)
        {
            if (!Interlocked.CompareExchange(ref m_isCheckingStatus, true, false))
            {
                var state = new Shared.AppServiceStateResponse
                {
                    Ami = _UpstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.AdministrationIntegrationService),
                    ClientId = _ClientId,
                    DeviceId = _DeviceId,
                    Hdsi = _UpstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.HealthDataService),
                    Magic = GetMagic(),
                    Online = _NetworkInformationService.IsNetworkAvailable && _NetworkInformationService.IsNetworkConnected,
                    Realm = GetRealm(),
                    Version = GetAssemblyVersion(),
                    FacilityId = _FacilityId,
                    OwnerId = _OwnerId,
                };


                lock (this.m_stateLock)
                {
                    this.m_stateObject = state;
                }
                Interlocked.Exchange(ref m_isCheckingStatus, false);
            }

        }

        [Export]
        [JavascriptInterface]
        public string GetServiceState()
        {
            return JsonConvert.SerializeObject(this.GetStateObject());
        }

        private Shared.AppServiceStateResponse GetStateObject()
        {
            lock(this.m_stateLock)
            {
                return this.m_stateObject;
            }
        }

        [Export]
        [JavascriptInterface]
        public bool GetOnlineState() => this.GetStateObject().Online;

        [Export]
        [JavascriptInterface]
        public bool IsAdminAvailable() => this.GetStateObject().Ami;

        [Export]
        [JavascriptInterface]
        public bool IsClinicalAvailable() => this.GetStateObject().Hdsi;

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

        [Export]
        [JavascriptInterface]
        public void CloseApp()
        {
            if (null != Application.Current)
                _ = MainThread.InvokeOnMainThreadAsync(() =>
                {
                    //We do not use the shell so we need to replace the main page in the app.
                    var restartpage = new RestartPage();

                    Application.Current!.Windows[0].Page = restartpage;

                    //Support the routing query parameter contract by calling the reason in.
                    restartpage.ApplyQueryAttributes(new Dictionary<string, object>
                    {
                            { "reason", Constants.REASONKEY_DEFAULT }
                    });

                    return Task.CompletedTask;
                });

        }


        protected override void Dispose(bool disposing)
        {
            _StateMonitorTimer.Dispose();
            base.Dispose(disposing);
        }
    }
}
