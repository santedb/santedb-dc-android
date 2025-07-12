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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZstdSharp.Unsafe;

namespace SanteDB.Client.Mobile
{
    [PreferredService(typeof(IPlatformSecurityProvider))]
    public class MauiPlatformSecurityProvider : IPlatformSecurityProvider
    {
        readonly SanteDBChain _InternalChain;
        readonly Tracer _Tracer;
        private readonly ManualResetEventSlim _AsyncCallback = new ManualResetEventSlim(false);

        public MauiPlatformSecurityProvider()
        {
            _Tracer = new Tracer(nameof(MauiPlatformSecurityProvider));
            _InternalChain = new SanteDBChain();
        }


        /// <inheritdoc/>
        public IEnumerable<X509Certificate2> FindAllCertificates(X509FindType findType, object findValue, StoreName storeName = StoreName.My, StoreLocation storeLocation = StoreLocation.CurrentUser, bool validOnly = true)
        {
            using (var store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);
                foreach (var cert in store.Certificates.Find(findType, findValue, validOnly))
                {
                    yield return cert;
                }
            }
        }

        /// <inheritdoc/>
        [SuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file", Justification = "<Pending>")]
        public bool IsAssemblyTrusted(Assembly assembly)
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

        public bool IsCertificateTrusted(X509Certificate2 certificate, DateTimeOffset? asOfDate = null)
        {
            return _InternalChain.ValidateCertificate(certificate);
        }

        ///<inheritdoc />
        public bool TryGetCertificate(X509FindType findType, object findValue, out X509Certificate2 certificate, bool validOnly = true)
        {
            return TryGetCertificate(findType, findValue, StoreName.My, StoreLocation.CurrentUser, out certificate, validOnly);
        }

        ///<inheritdoc />
        public bool TryGetCertificate(X509FindType findType, object findValue, StoreName storeName, out X509Certificate2 certificate, bool validOnly = true)
        {
            return TryGetCertificate(findType, findValue, storeName, StoreLocation.CurrentUser, out certificate, validOnly);
        }

        ///<inheritdoc />
        public bool TryGetCertificate(X509FindType findType, object findValue, StoreName storeName, StoreLocation storeLocation, out X509Certificate2 certificate, bool validOnly = true)
        {
            if (findValue == null)
            {
                throw new ArgumentNullException(nameof(findValue));
            }

            try
            {
                using (var store = new X509Store(storeName, storeLocation))
                {
                    store.Open(OpenFlags.ReadOnly);

                    var certs = store.Certificates.Find(findType, findValue, validOnly: validOnly); // since the user is asking for a specific certificate allow for searching of invalid certificates

                    if (certs.Count == 0)
                    {
                        certificate = null;
                        return false;
                    }

                    certificate = certs[0];

                    store.Close();

                    return true;
                }
            }
            catch (CryptographicException)
            {
                certificate = null;
                return false;
            }
        }

        ///<inheritdoc />
        public bool TryInstallCertificate(X509Certificate2 certificate, StoreName storeName = StoreName.My, StoreLocation storeLocation = StoreLocation.CurrentUser)
        {
            var audit = this.AuditCertificateInstallation(certificate);

            try
            {
                using (var store = new X509Store(storeName, storeLocation))
                {
                    store.Open(OpenFlags.ReadWrite);

                    var password = Guid.NewGuid().ToString();

                    var certtext = certificate.Export(X509ContentType.Pfx, password);

                    var importcert = new X509Certificate2(certtext, password);

                    store.Add(importcert);

                    this._Tracer.TraceWarning("Certificate {0} has been installed to {1}/{2}", certificate.Subject, storeLocation, storeName);
                    audit?.WithOutcome(OutcomeIndicator.Success);

                    store.Close();

                    return true;
                }
            }
            catch (CryptographicException cex)
            {
                audit?.WithOutcome(OutcomeIndicator.SeriousFail);
                return false;
            }
            catch (Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
            {
                audit?.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            finally
            {
                audit?.Send();
            }
        }

        ///<inheritdoc />
        public bool TryUninstallCertificate(X509Certificate2 certificate, StoreName storeName = StoreName.My, StoreLocation storeLocation = StoreLocation.CurrentUser)
        {
            var audit = this.AuditCertificateRemoval(certificate);

            try
            {
                using (var store = new X509Store(storeName, storeLocation))
                {
                    store.Open(OpenFlags.ReadWrite);

                    var thumbprint = certificate?.Thumbprint;

                    var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, true);

                    if (certs.Count == 0)
                    {
                        audit?.WithOutcome(OutcomeIndicator.MinorFail);
                        return false;
                    }

                    foreach (var cert in certs)
                    {
                        store.Certificates.Remove(cert);
                    }
                    this._Tracer.TraceWarning("Certificate {0} has been removed from {1}/{2}", certificate.Subject, storeLocation, storeName);

                    audit?.WithOutcome(OutcomeIndicator.Success);

                    store.Close();

                    return true;
                }
            }
            catch (CryptographicException cex)
            {
                audit?.WithOutcome(OutcomeIndicator.SeriousFail);
                return false;
            }
            catch (Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
            {
                audit?.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            finally
            {
                audit?.Send();
            }
        }


        /// <summary>
        /// Create an audit builder for certificate installation.
        /// </summary>
        /// <param name="certificate">The certificate being installed.</param>
        /// <returns></returns>
        private IAuditBuilder AuditCertificateInstallation(X509Certificate2 certificate)
            => ApplicationServiceContext.Current?.GetAuditService()?.Audit() // Prevents circular dependency in dCDR
                .WithTimestamp()
                .WithEventType(EventTypeCodes.SecurityAlert)
                .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Import)
                .WithAction(Core.Model.Audit.ActionType.Execute)
                .WithLocalDestination()
                .WithPrincipal()
                .WithSystemObjects(Core.Model.Audit.AuditableObjectRole.SecurityResource, Core.Model.Audit.AuditableObjectLifecycle.Import, certificate);

        /// <summary>
        /// Create an audit builder for certificate removal.
        /// </summary>
        /// <param name="certificate">The certificate being removed.</param>
        /// <returns></returns>
        private IAuditBuilder AuditCertificateRemoval(X509Certificate2 certificate)
            => ApplicationServiceContext.Current?.GetAuditService()?.Audit()
                .WithTimestamp()
                .WithEventType(EventTypeCodes.SecurityAlert)
                .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.SecurityAlert)
                .WithAction(Core.Model.Audit.ActionType.Delete)
                .WithLocalDestination()
                .WithPrincipal()
                .WithSystemObjects(Core.Model.Audit.AuditableObjectRole.SecurityResource, Core.Model.Audit.AuditableObjectLifecycle.PermanentErasure, certificate);


        /// <inheritdoc/>
        /// <remarks>This is not required on Windows or Linux</remarks>
        public bool DemandPlatformServicePermission(PlatformServicePermission platformServicePermission)
        {
            try
            {

                if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.M)
                {
                    return true; // Android Marshmallow Already applies
                }
                else // using this method as it works better than the Maui versions which don't seem to work beyond SDK 31
                {
                    var context = Platform.AppContext;
                    var activity = Platform.CurrentActivity;
                    string permissionString = string.Empty;
                    switch (platformServicePermission)
                    {
                        case PlatformServicePermission.Camera:
                            permissionString = Android.Manifest.Permission.Camera;
                            break;
                        case PlatformServicePermission.Geolocation:
                            permissionString = Android.Manifest.Permission.AccessCoarseLocation;
                            break;
                        case PlatformServicePermission.ExternalMedia:
                            permissionString = Android.Manifest.Permission.WriteExternalStorage;
                            break;
                        case PlatformServicePermission.Bluetooth:
                            permissionString = Android.Manifest.Permission.Bluetooth;
                            break;
                    }
                    if(activity.CheckSelfPermission(permissionString) != Android.Content.PM.Permission.Granted)
                    {
                        activity.RequestPermissions(new String[] { permissionString }, 0);
                        return activity.CheckSelfPermission(permissionString) == Android.Content.PM.Permission.Granted;
                    }
                    return true;
                }
                
            }
            catch(Exception ex)
            {
                throw new SecurityException(ErrorMessages.PLATFORM_SECURITY_ERROR, ex);
            }
        }

    }
}
