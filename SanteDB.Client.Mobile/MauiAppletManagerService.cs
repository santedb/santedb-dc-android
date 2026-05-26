/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 */
using Acornima.Ast;
using Org.BouncyCastle.Crypto.Agreement.JPake;
using SanteDB.Client.Batteries.Services;
using SanteDB.Client.Configuration;
using SanteDB.Client.Services;
using SanteDB.Client.UserInterface;
using SanteDB.Core;
using SanteDB.Core.Applets;
using SanteDB.Core.Applets.Configuration;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Applets.Services.Impl;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.i18n;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Services;
using SharpCompress.Compressors;
using SharpCompress.Compressors.Deflate;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Tracing;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;

namespace SanteDB.Client.Mobile
{ 
    /// <summary>
    /// Represents a <see cref="IAppletManagerService"/> which unpacks applet static files for faster access
    /// </summary>
    public class MauiAppletManagerService : ClientAppletManagerService
    {
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(MauiAppletManagerService));

        /// <summary>
        /// DI constructor
        /// </summary>
        public MauiAppletManagerService(IConfigurationManager configurationManager, IAppletHostBridgeProvider bridgeProvider, IUserInterfaceInteractionProvider userInterfaceInteractionProvider, IPlatformSecurityProvider platformSecurityProvider)
            : base(configurationManager, bridgeProvider, userInterfaceInteractionProvider, platformSecurityProvider)
        {
        }

        protected override IEnumerable<AppletManifest> GetInstalledAppletManifests()
        {

            // Load packages from applets/ filesystem directory
            var appletDir = this.p_configuration.AppletDirectory;
            if (!Path.IsPathRooted(appletDir))
            {
                var location = Assembly.GetEntryAssembly()?.Location ?? Assembly.GetExecutingAssembly().Location;
                appletDir = Path.Combine(Path.GetDirectoryName(location), this.p_configuration.AppletDirectory);
            }

            if (!Directory.Exists(appletDir))
            {
                Directory.CreateDirectory(appletDir);
                this.m_tracer.TraceWarning("Applet directory {0} doesn't exist, no applets will be loaded", appletDir);
            }
            else
            {
                this.m_tracer.TraceInfo("Scanning {0} for manifests...", appletDir);
                var appletFiles = Directory.GetFiles(appletDir, "*.manifest").ToArray();
                int loadedApplets = 0;
                foreach (var packageFile in appletFiles)
                {
                    AppletManifest loadedManifest = null;
                    try
                    {
                        this.m_tracer.TraceInfo("Loading {0}...", packageFile);
                        this.p_userInterfaceInteractionProvider.SetStatus(null, $"Loading applet {Path.GetFileNameWithoutExtension(packageFile)}", (float)loadedApplets++ / (float)appletFiles.Length);
                        using (var fs = File.OpenRead(packageFile))
                        {
                            loadedManifest = AppletManifest.Load(fs);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (this.p_userInterfaceInteractionProvider.Confirm($"Error loading {Path.GetFileName(packageFile)}, would you like to ignore this error?"))
                        {
                            File.Delete(packageFile);
                        }
                        else
                        {
                            throw new InvalidOperationException($"Error loading {Path.GetFileName(packageFile)}", ex);
                        }
                    }

                    // use a yield return outside of the try/catch
                    if (loadedManifest != null)
                    {
                        yield return loadedManifest;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override byte[] GetPackage(string appletId)
        {
            if (!Directory.Exists(this.p_configuration.AppletDirectory))
            {
                throw new InvalidOperationException(ErrorMessages.NOT_INITIALIZED);
            }

            // If we have the original copy send that if not create our own (unsigned) version don't
            var mfstFile = Path.Combine(this.p_configuration.AppletDirectory, $"{appletId}.manifest");
            if (File.Exists(mfstFile))
            {
                // The pakfile on disk is not compressed - we need to compress it for the response
                using (var fs = File.OpenRead(mfstFile))
                using (var ms = new MemoryStream())
                {
                    // Load the manifest
                    AppletManifest.Load(fs).CreatePackage().Save(ms);
                    return ms.ToArray();
                }
            }
            else
            {
                var manifest = this.GetApplet(appletId);
                using (var ms = new MemoryStream())
                {
                    manifest.CreatePackage().Save(ms);
                    return ms.ToArray();
                }
            }
        }

        /// <inheritdoc/>
        protected override string GetInstallationTargetFile(string appletId)
        {
            return Path.ChangeExtension(base.GetInstallationTargetFile(appletId), "manifest");
        }

        /// <inheritdoc/>
        protected override AppletManifest SaveAppletPackageData(AppletPackage package)
        {
            using(var mfstStream = File.Create(this.GetInstallationTargetFile(package.Meta.Id)))
            {
                var manifest = package.Unpack();
                manifest.Save(mfstStream);
                return manifest;
            }
        }
    }
}
