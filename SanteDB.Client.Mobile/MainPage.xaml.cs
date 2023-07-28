using Microsoft.Maui.Handlers;
using SanteDB.Core;
using System.Diagnostics;

namespace SanteDB.Client.Mobile
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        private string? _HttpMagic;

        public MainPage(string sourceUrl, string httpMagicValue)
        {
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

                var browserinterface = new MauiBrowserInterface(ApplicationServiceContext.Current, this);
                awebview.AddJavascriptInterface(browserinterface, "__sdb_bridge");
                
                //TODO: Additional platform initialization
            }
            else
            {
                throw new InvalidOperationException("Platform not supported");
            }

        }

        //private void WebView_HandlerChanging(object sender, HandlerChangingEventArgs e)
        //{
        //    var handler = e.NewHandler;
            
        //    var platformview = handler?.PlatformView;

        //    Debugger.Break();
        //}



        //private void OnCounterClicked(object sender, EventArgs e)
        //{
        //    count++;

        //    if (count == 1)
        //        CounterBtn.Text = $"Clicked {count} time";
        //    else
        //        CounterBtn.Text = $"Clicked {count} times";

        //    SemanticScreenReader.Announce(CounterBtn.Text);
        //}
    }
}