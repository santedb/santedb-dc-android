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
using Microsoft.Data.Sqlite;
using SanteDB.Client.Configuration;
using SanteDB.Client.Rest;
using SanteDB.Core.Model.Security;
using SanteDB.Core;
using System.Diagnostics;
using System.Runtime.Loader;
using SanteDB.Rest.HDSI;
using SanteDB.Rest.AMI;
using SanteDB.Rest.BIS;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using SanteDB.Client.Shared;
using SanteDB.Core.Services.Impl;
using SanteDB.Core.Services;
using SanteDB.Rest.WWW;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.IO;
using Microsoft.Maui.Storage;
using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.Maui.ApplicationModel;

namespace SanteDB.Client.Mobile;

public partial class StartupPage : ContentPage
{

    // JF - Reduce the number of dispatching to the UI thread
    private string m_lastStatusText = string.Empty;
    private float m_lastStatusProgress = 0.0f;

    public StartupPage()
    {
        InitializeComponent();
    }

    public bool IsStarting { get; private set; }

    public void SetStatus(string identifier, string status, float progress)
    {
        if (!string.IsNullOrEmpty(identifier) && identifier != nameof(DependencyServiceManager))
            return;

        if (progress < 0)
            progress = 0;
        else if (progress > 1)
            progress = 1;

        if (this.m_lastStatusText != status || this.m_lastStatusProgress != progress)
        {
            this.m_lastStatusText = status;
            this.m_lastStatusProgress = progress;
            Dispatcher.Dispatch(() =>
            {
                StatusLabel.Text = status;
                StatusProgress.Progress = progress;
            });
        }
    }

    [RequiresUnreferencedCode("Loads types from AppDomain.CurrentDomain")]
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        this.VersionLabel.Text = $"v.{this.GetType().Assembly.GetName().Version}";
        var task = Task.Run(async () =>
        {
            await Task.Yield(); //Yield back to move off the main thread.

            //var splashwriter = new SplashScreenTraceWriter(m_window);
            //SanteDB.Core.Diagnostics.Tracer.AddWriter(splashwriter, System.Diagnostics.Tracing.EventLevel.Verbose);
            //m_window.ShowSplashStatusText("Starting SanteDB");

            //Directory.GetFiles(Path.GetDirectoryName(typeof(Program).Assembly.Location)!, "Sante*.dll").ToList().ForEach(itm =>
            //{
            //    try
            //    {
            //        m_window.ShowSplashStatusText(string.Format("Loading reference assembly {0}...", itm));
            //        AssemblyLoadContext.Default.LoadFromAssemblyPath(itm);

            //    }
            //    catch (Exception e)
            //    {
            //        m_window.ShowSplashStatusText(string.Format("Error loading assembly {0}: {1}", itm, e));
            //    }
            //});

            // JF - Allow startup to set status on the startup page
            try
            {
                var directoryprovider = new Shared.LocalAppDirectoryProvider("dc-maui");

                if (!directoryprovider.IsConfigFilePresent())
                {
                    this.StatusLabel.Text = "Preparing Initial Configuration";
                    //ShowStatusText("Preparing Default Applets");
                    List<string> applets = new();
                    using var appletslist = await FileSystem.OpenAppPackageFileAsync("applets.txt");
                    using (var sr = new StreamReader(appletslist))
                    {
                        while (!sr.EndOfStream)
                        {
                            // JF - Receiving an AssetStreamIsClosed exception when using async read line
                            var line = sr.ReadLine();

                            var commentmarker = line.IndexOf('#');

                            if (commentmarker != -1)
                            {
                                line = line.Substring(0, commentmarker)?.Trim();
                            }

                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                applets.Add(line);
                            }
                        }
                    }

                    var pakdirectory = Path.Combine(directoryprovider.GetDataDirectory(), "pakfiles");

                    Directory.CreateDirectory(pakdirectory);

                    var appletsPrepared = 0;
                    foreach (var applet in applets)
                    {
                        SetStatus(null, $"Preparing Initial Configuration", (float)appletsPrepared++ / (float)applets.Count);
                        using (var appletstream = await FileSystem.OpenAppPackageFileAsync(applet))
                        {
                            using (var fs = new FileStream(Path.Combine(pakdirectory, applet), FileMode.Create, FileAccess.ReadWrite))
                            {
                                appletstream.CopyTo(fs);
                            }
                        }
                    }
                }

                this.IsStarting = true;
                // Allow set status
                Stack<AssemblyName> assemblies = new(typeof(StartupPage).Assembly.GetReferencedAssemblies());
                List<(AssemblyName, Assembly)> loadedassemblies = new();
                assemblies.Push(typeof(Persistence.Synchronization.ADO.Configuration.AdoSynchronizationFeature).Assembly.GetName());

                // JF - Keep track for showing progress to the user 
                int totalAssemblies = assemblies.Count(), processedAssemblies = 0;

                while (assemblies.TryPop(out var assemblyname))
                {
                    SetStatus(null, "Loading Core Modules", (float)processedAssemblies++ / (float)totalAssemblies);

                    if (loadedassemblies.Any(tuple => assemblyname.FullName.Equals(tuple.Item1.FullName, StringComparison.Ordinal)))
                    {
                        continue;
                    }


                    try
                    {
                        var assembly = Assembly.Load(assemblyname);
                        loadedassemblies.Add((assemblyname, assembly));

                        if (assemblyname.Name.StartsWith("SanteDB"))
                        {
                            foreach (var refassembly in assembly.GetReferencedAssemblies())
                            {
                                assemblies.Push(refassembly);
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }


                try
                {
                    SetStatus(null, "Initializing SQLite Provider", 0f);
                    SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3mc());
                    SQLitePCL.raw.FreezeProvider(true);
                    SqliteConnection.ClearAllPools(); //Force-load sqlite.
                    SanteDB.OrmLite.Providers.Sqlite.SqliteSpellfixExtensionLoader.SetLibraryInformation("libe_sqlite3mc", "sqlite3_spellfix_init");
                }
                catch
                {

                }

                try
                {
                    var applicationidentity = new SecurityApplication
                    {
                        Key = Guid.Parse("a0fdceb2-a2d3-11ea-ae5e-00155d4f0905"),
                        //ApplicationSecret = Parameters.ApplicationSecret ?? "FE78825ADB56401380DBB406411221FD"
                        //Name = Parameters.ApplicationName ?? "org.santedb.disconnected_client.win32"
                        ApplicationSecret = "C5B645B7D30A4E7E81A1C3D8B0E28F4C",
                        Name = "org.santedb.disconnected_client.android"
                    };



                    SanteDB.Client.Batteries.ClientBatteries.Initialize(directoryprovider.GetDataDirectory(), directoryprovider.GetConfigDirectory(), new Client.Configuration.Upstream.UpstreamCredentialConfiguration()
                    {
                        CredentialType = SanteDB.Client.Configuration.Upstream.UpstreamCredentialType.Application,
                        CredentialName = applicationidentity.Name,
                        CredentialSecret = applicationidentity.ApplicationSecret
                    });

                    AppDomain.CurrentDomain.SetData(RestServiceInitialConfigurationProvider.BINDING_BASE_DATA, "http://127.0.0.1:9200");

                    IConfigurationManager configmanager = null;

                    if (directoryprovider.IsConfigFilePresent())
                    {
                        configmanager = new FileConfigurationService(directoryprovider.GetConfigFilePath(), isReadonly: true);
                    }
                    else
                    {
                        configmanager = new InitialConfigurationManager(SanteDBHostType.Client, "DEFAULT", directoryprovider.GetConfigFilePath());
                    }


                    //var configmanager = new SanteDB.Client.Batteries.Configuration.DefaultDcdrConfigurationProvider();

                    string bridgescript = null;

                    using (var bridgestream = await FileSystem.OpenAppPackageFileAsync("santedb_shim.js"))
                    {
                        using (var sr = new StreamReader(bridgestream))
                        {
                            bridgescript = await sr.ReadToEndAsync();
                        }
                    }

                    var context = new MauiApplicationContext("DEFAULT", configmanager, this, bridgescript);

                    SetStatus(null, "Starting SanteDB Service Context", 0f);

                    ServiceUtil.Start(Guid.NewGuid(), context);


                    var magic = context.ActivityUuid.ToByteArray().HexEncode();

                    //splashwriter.TraceInfo(string.Empty, string.Empty);

                    //SanteDB.Core.Diagnostics.Tracer.RemoveWriter(splashwriter);

                    // Install the packages to the local applet manager

                    var starturl = configmanager switch
                    {
                        InitialConfigurationManager => "http://127.0.0.1:9200/#!/config/initialSettings",
                        _ => "http://127.0.0.1:9200/#!/"
                    };


                    this.Dispatcher.Dispatch(() =>
                    {
                        var shell = Shell.Current;
                        App.Current.MainPage = new MainPage(starturl, magic, context);
                    });


                }
                catch (Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
                {
                    // JF- Throws exception if no debugger attached
                    if (Debugger.IsAttached)
                    {
                        Debugger.Break();
                    }

                    Dispatcher.Dispatch(() =>
                    {
                        this.ErrorLabel.IsVisible = true;
                        this.ErrorLabel.Text = ex.ToHumanReadableString();
                    });
                }
            }
            finally
            {
                this.IsStarting = false;
            }
        });


    }
}