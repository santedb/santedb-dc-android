using Microsoft.Extensions.Logging;
using SanteDB.Rest.HDSI;
using System.Diagnostics;
using System.Runtime.Loader;
using System.Security.Cryptography.X509Certificates;

namespace SanteDB.Client.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            try
            {
                SanteDB.Core.Security.SecurityExtensions.InstallCertsForChain();
            }
            catch (Exception ex) {
                Debugger.Break();
            }

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
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                ;

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}