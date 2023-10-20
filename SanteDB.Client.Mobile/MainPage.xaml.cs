using Microsoft.Maui.Handlers;
using SanteDB.Core;
using System.Diagnostics;

namespace SanteDB.Client.Mobile
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        private string _HttpMagic;
        readonly MauiApplicationContext _ApplicationContext;

        public MainPage(string sourceUrl, string httpMagicValue, MauiApplicationContext applicationContext)
        {
            _ApplicationContext = applicationContext;

            _ApplicationContext.GetInteractionProvider().SetStatusCallback = (task, message, progress) =>
            {
                Dispatcher.DispatchAsync(async () =>
                {
                    await NotificationBar.ShowOrUpdateNotificationAsync(task, message, progress);
                });
            };

            InitializeComponent();

            _HttpMagic = httpMagicValue;

            WebView.HandlerChanged+= WebView_HandlerChanged;   

            WebView.Source = sourceUrl;
        }

        private void WebView_HandlerChanged(object sender, EventArgs e)
        {
            var handler = WebView.Handler;

            if ((handler?.PlatformView) is Android.Webkit.WebView awebview)
            {
                awebview.Settings.UserAgentString = $"SanteDB-{_HttpMagic}";
                awebview.Settings.JavaScriptEnabled = true;
                awebview.Settings.SetGeolocationEnabled(true);

                var browserinterface = new MauiBrowserInterface(ApplicationServiceContext.Current, this);
                awebview.AddJavascriptInterface(browserinterface, "__sdb_bridge");
                
                //TODO: Additional platform initialization
                
                
            }
            else
            {
                throw new InvalidOperationException("Platform not supported");
            }
        }
    }
}