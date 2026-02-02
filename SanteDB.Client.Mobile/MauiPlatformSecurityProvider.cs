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
 * Date: 2023-5-16
 */
using Android;
using Android.App;
using Jint.Runtime.Debugger;
using Kotlin.Contracts;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Dispatching;
using SanteDB.Client.Shared;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.i18n;
using SanteDB.Core.Model.Audit;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZstdSharp.Unsafe;
using static Microsoft.Maui.ApplicationModel.Permissions;

namespace SanteDB.Client.Mobile
{
    /// <summary>
    /// MAUI Certificate Provider
    /// </summary>
    /// <remarks>
    /// This implementation is based on the <see cref="MonoPlatformSecurityProvider"/>. Since PersistKeySet is not supported on Mono based
    /// environments, this implementation stores the private keys in a hidden directory password protected as PFX files.
    /// </remarks>
    [PreferredService(typeof(IPlatformSecurityProvider))]
    public class MauiPlatformSecurityProvider : MonoPlatformSecurityProvider
    {
        readonly SanteDBChain _InternalChain;
        readonly Tracer _Tracer = Tracer.GetTracer(typeof(MauiPlatformSecurityProvider));
        
        public MauiPlatformSecurityProvider()
        {
            _InternalChain = new SanteDBChain();
        }

        /// <inheritdoc/>
        [SuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file", Justification = "We are not using AOT and the Assembly will resolve.")]
        public override bool IsAssemblyTrusted(Assembly assembly)
        {
            if (null == assembly)
            {
                return false;
            }

            //Try to validate using internal chain first.
            var asmlocation = assembly?.Location;

            if (string.IsNullOrEmpty(asmlocation))
            {
                //TODO: Validate we're in a single-file app instead of just returning.
                return true;
            }
            else
            {
                var signedcert = new X509Certificate2(X509Certificate2.CreateFromSignedFile(asmlocation));

                if (null == signedcert)
                {
                    return false; //AllowUnsignedAssemblies is false if we get this far.
                }

                if (_InternalChain.ValidateCertificate(signedcert))
                {
                    return true;
                }

                //Try using the underlying platform validation.
                assembly?.ValidateCodeIsSigned(false); // will throw if not valid

                return true;
            }
        }

        public override bool IsCertificateTrusted(X509Certificate2 certificate, DateTimeOffset? asOfDate = null)
        {
            return _InternalChain.ValidateCertificate(certificate);
        }

        /// <inheritdoc/>
        /// <remarks>This is not required on Windows or Linux</remarks>
#pragma warning disable CA1416 // We manually check the android version
        public override bool DemandPlatformServicePermission(PlatformServicePermission platformServicePermission)
        {
            try
            {
                return Nito.AsyncEx.AsyncContext.Run(async () => await this.DemandPlatformServicePermissionInternalAsync(platformServicePermission));
            }
            catch (Exception ex)
            {
                throw new SecurityException(ErrorMessages.PLATFORM_SECURITY_ERROR, ex);
            }
        }

        private async Task<bool> DemandPlatformServicePermissionInternalAsync(PlatformServicePermission platformServicePermission)
        {
            if (!MainThread.IsMainThread)
            {
                return await MainThread.InvokeOnMainThreadAsync(async () => await this.DemandPlatformServicePermissionInternalAsync(platformServicePermission));
            }
            else
            {
                var permission = PermissionStatus.Unknown;
                switch (platformServicePermission)
                {
                    case PlatformServicePermission.ExternalMedia:
                        permission = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
                        if (permission != PermissionStatus.Granted)
                        {
                            permission = await Permissions.RequestAsync<Permissions.StorageWrite>();
                        }
                        break;
                    case PlatformServicePermission.Camera:
                        permission = await Permissions.CheckStatusAsync<Permissions.Camera>();
                        if (permission != PermissionStatus.Granted)
                        {
                            permission = await Permissions.RequestAsync<Permissions.Camera>();
                        }
                        break;
                    case PlatformServicePermission.Bluetooth:
                        permission = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
                        if (permission != PermissionStatus.Granted)
                        {
                            permission = await Permissions.RequestAsync<Permissions.Bluetooth>();
                        }
                        break;
                    case PlatformServicePermission.Geolocation:
                        permission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                        if (permission != PermissionStatus.Granted)
                        {
                            permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                        }
                        break;
                }

                return permission == PermissionStatus.Granted;
            }
        }
#pragma warning restore CA1416
    }
}
