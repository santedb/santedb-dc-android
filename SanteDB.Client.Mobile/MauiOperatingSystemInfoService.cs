using SanteDB.Core;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.Mobile
{
    /// <summary>
    /// Specialized <see cref="IOperatingSystemInfoService"/> with specific bindings for the Maui platform.
    /// </summary>
    [PreferredService(typeof(IOperatingSystemInfoService))]
    internal class MauiOperatingSystemInfoService : IOperatingSystemInfoService
    {
        public string VersionString => DeviceInfo.Current.VersionString;

        public OperatingSystemID OperatingSystem
        {
            get
            {
                var currentplatform = DeviceInfo.Current.Platform;

                if (currentplatform == DevicePlatform.Android)
                {
                    return OperatingSystemID.Android;
                }
                else if (currentplatform == DevicePlatform.iOS)
                {
                    return OperatingSystemID.iOS;
                }
                else if (currentplatform == DevicePlatform.WinUI)
                {
                    return OperatingSystemID.Win32;
                }
                else
                {
                    return OperatingSystemID.Other;
                }
            }
        }

        public string MachineName => DeviceInfo.Current.Name;

        public string ManufacturerName => DeviceInfo.Current.Manufacturer;
    }
}
