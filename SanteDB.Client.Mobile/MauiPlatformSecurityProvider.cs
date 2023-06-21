using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.Mobile
{
    public class MauiPlatformSecurityProvider : IPlatformSecurityProvider
    {



        public IEnumerable<X509Certificate2> FindAllCertificates(X509FindType findType, object findValue, StoreName storeName = StoreName.My, StoreLocation storeLocation = StoreLocation.CurrentUser, bool validOnly = true)
        {
        }

        public bool IsAssemblyTrusted(Assembly assembly)
        {

        }

        public bool TryGetCertificate(X509FindType findType, object findValue, out X509Certificate2 certificate)
        {
        }

        public bool TryGetCertificate(X509FindType findType, object findValue, StoreName storeName, out X509Certificate2 certificate)
        {
        }

        public bool TryGetCertificate(X509FindType findType, object findValue, StoreName storeName, StoreLocation storeLocation, out X509Certificate2 certificate)
        {
        }

        public bool TryInstallCertificate(X509Certificate2 certificate, StoreName storeName = StoreName.My, StoreLocation storeLocation = StoreLocation.CurrentUser)
        {

        }

        public bool TryUninstallCertificate(X509Certificate2 certificate, StoreName storeName = StoreName.My, StoreLocation storeLocation = StoreLocation.CurrentUser)
        {
        }
    }
}
