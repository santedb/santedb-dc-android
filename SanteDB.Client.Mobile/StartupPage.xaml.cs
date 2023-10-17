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

namespace SanteDB.Client.Mobile;

public partial class StartupPage : ContentPage
{
    public StartupPage()
    {
        InitializeComponent();
    }

    public bool IsStarting { get; }


    public void SetStatus(string identifier, string status, float progress)
    {
        if (!string.IsNullOrEmpty(identifier) && identifier != nameof(DependencyServiceManager))
            return;

        if (progress < 0)
            progress = 0;
        else if (progress > 1)
            progress = 1;
        
        Dispatcher.Dispatch(() =>
        {
            StatusLabel.Text = status;
            StatusProgress.Progress = progress;
        });
    }

    [RequiresUnreferencedCode("Loads types from AppDomain.CurrentDomain")]
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        

        var directoryprovider = new Shared.LocalAppDirectoryProvider("dc-maui");

        if (!directoryprovider.IsConfigFilePresent())
        {
            //ShowStatusText("Preparing Default Applets");
            List<string> applets = new();
            using var appletslist = await FileSystem.OpenAppPackageFileAsync("applets.txt");
            using (var sr = new StreamReader(appletslist))
            {
                string line;
                while (!string.IsNullOrWhiteSpace((line = await sr.ReadLineAsync())))
                {
                    applets.Add(line);
                }
            }

            var pakdirectory = Path.Combine(directoryprovider.GetDataDirectory(), "pakfiles");

            Directory.CreateDirectory(pakdirectory);
            

            foreach(var applet in applets)
            {
                //ShowStatusText($"Preparing {applet}");
                using var appletstream = await FileSystem.OpenAppPackageFileAsync(applet);
                using var fs = new FileStream(Path.Combine(pakdirectory, applet), FileMode.Create, FileAccess.ReadWrite);

                await appletstream.CopyToAsync(fs);

                fs.Close();
                appletstream.Close();
            }
        }

        using var stream = await FileSystem.OpenAppPackageFileAsync("santedb-shim.js");
        using var reader = new StreamReader(stream);

        var bridgescript = await reader.ReadToEndAsync();

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

            Stack<AssemblyName> assemblies = new(typeof(StartupPage).Assembly.GetReferencedAssemblies());
            List<(AssemblyName, Assembly)> loadedassemblies = new();


            assemblies.Push(typeof(SanteDB.Persistence.Synchronization.ADO.Configuration.AdoSynchronizationFeature).Assembly.GetName());

            while (assemblies.TryPop(out var assemblyname))
            {
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
                SQLitePCL.Batteries_V2.Init();
                SqliteConnection.ClearAllPools(); //Force-load sqlite.
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

                var context = new MauiApplicationContext("DEFAULT", configmanager, this, bridgescript);


                ServiceUtil.Start(Guid.NewGuid(), context);


                var magic = context.ActivityUuid.ToByteArray().HexEncode();

                //splashwriter.TraceInfo(string.Empty, string.Empty);

                //SanteDB.Core.Diagnostics.Tracer.RemoveWriter(splashwriter);

                var starturl = configmanager switch
                {
                    InitialConfigurationManager => "http://127.0.0.1:9200/#!/config/initialSettings",
                    _ => "http://127.0.0.1:9200/#!/"
                };


                this.Dispatcher.Dispatch(() =>
                {
                    App.Current.MainPage = new MainPage(starturl, magic);
                });


            }
            catch (Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
            {
                Debugger.Break();
            }

        });


    }

   

}