using Android.Webkit;
using Java.Interop;
using Newtonsoft.Json;
using SanteDB.Client.Configuration.Upstream;
using SanteDB.Core;
using SanteDB.Core.Services;

#nullable enable

namespace SanteDB.Client.Mobile
{
    public class MauiBrowserInterface : Java.Lang.Object
    {
        readonly IApplicationServiceContext _Context;
        readonly MainPage _MainPage;
        readonly IConfigurationManager? _ConfigManager;
        readonly string? _DeviceId;
        readonly string? _ClientId;
        readonly string? _RealmId;

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
            _Context = context;
            _ConfigManager = SanteDB.Core.ApplicationServiceContext.GetService<IConfigurationManager>(context);
            _MainPage = mainPage;

            var upstreamconfig = _ConfigManager?.GetSection<UpstreamConfigurationSection>();

            var devicecredential = upstreamconfig?.Credentials?.FirstOrDefault(c => c.CredentialType == UpstreamCredentialType.Device);
            var appcredential = upstreamconfig?.Credentials?.FirstOrDefault(c => c.CredentialType == UpstreamCredentialType.Application);

            _DeviceId = devicecredential?.CredentialName;
            _ClientId = appcredential?.CredentialName;
            _RealmId = upstreamconfig?.Realm?.DomainName;
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
                Version = GetAssemblyVersion()
            };

            return JsonConvert.SerializeObject(state);
        }

        [Export]
        [JavascriptInterface]
        public bool GetOnlineState()
        {
            return true;
        }

        [Export]
        [JavascriptInterface]
        public bool IsAdminAvailable()
        {
            return true;
        }

        [Export]
        [JavascriptInterface]
        public bool IsClinicalAvailable()
        {
            return true;
        }

        [Export]
        [JavascriptInterface]
        public string? GetClientId()
        {
            return _ClientId;
        }

        [Export]
        [JavascriptInterface]
        public string? GetDeviceId()
        {
            return _DeviceId;
        }

        [Export]
        [JavascriptInterface]
        public string? GetRealm()
        {
            return _RealmId;
        }

        [Export]
        [JavascriptInterface]
        public string GetLocale()
        {
            return "en"; //TODO
        }

        [Export]
        [JavascriptInterface]
        public void SetLocale(string locale)
        {
            //TODO: Fix this.
        }

        [Export]
        [JavascriptInterface]
        public String GetString(String stringId)
        {
            try
            {
                var appletResource = SanteDB.Core.ApplicationServiceContext.GetService<ILocalizationService>(_Context).GetStrings(this.GetLocale()).FirstOrDefault(o => o.Key == stringId).Value;
                if (appletResource != null)
                    return appletResource;
                else
                {
                    //var androidStringId = this.m_context.Resources.GetIdentifier(stringId, "string", this.m_context.PackageName);
                    //if (androidStringId > 0)
                    //    return this.m_context.Resources.GetString(androidStringId);
                    //else
                        return stringId;
                }
            }
            catch (Exception e)
            {
                //this.m_tracer.TraceWarning("Error retreiving string {0}", stringId);
                return stringId;
            }
        }

        [Export]
        [JavascriptInterface]
        public string GetMagic()
        {
            return _Context.ActivityUuid.ToString();
        }

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
