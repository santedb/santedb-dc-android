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