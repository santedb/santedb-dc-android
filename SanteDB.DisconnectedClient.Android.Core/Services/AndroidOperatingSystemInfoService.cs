using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AndroidOS = Android.OS;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SanteDB.Core;
using SanteDB.DisconnectedClient.Security;

namespace SanteDB.DisconnectedClient.Android.Core.Services
{
    /// <summary>
    /// Operating system information service
    /// </summary>
    public class AndroidOperatingSystemInfoService : SanteDB.Core.Services.IOperatingSystemInfoService
    {
        /// <summary>
        /// Gets the current version of the operatin
        /// </summary>
        public string VersionString
        {
            get
            {
                return Application.Context.ApplicationContext.PackageManager.GetPackageInfo(Application.Context.ApplicationContext.PackageName, 0).VersionName;
            }
        }

        /// <summary>
        /// Gets the operating system class
        /// </summary>
        public OperatingSystemID OperatingSystem => OperatingSystemID.Android;

        /// <summary>
        /// Gets the machine name
        /// </summary>
        public string MachineName => AndroidOS.Build.Model;

        /// <summary>
        /// Get the manufacturer
        /// </summary>
        public string ManufacturerName => AndroidOS.Build.Manufacturer;
    }
}