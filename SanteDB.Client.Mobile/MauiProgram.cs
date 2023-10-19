using BarcodeScanner.Mobile;
using Microsoft.Extensions.Logging;
using SanteDB.Core.Applets.Services.Impl;
using SanteDB.Rest.HDSI;
using System.Diagnostics;
using System.Runtime.Loader;
using System.Security.Cryptography.X509Certificates;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;

namespace SanteDB.Client.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            //try
            //{
            //    SQLitePCL.Batteries_V2.Init();
            //    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            //}
            //catch
            //{

            //    Debugger.Break();
            //}

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitMarkup()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .ConfigureMauiHandlers(handlers =>
                {
                    handlers.AddBarcodeScannerHandler();
                })
                ;

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}