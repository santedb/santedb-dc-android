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
 * Date: 2023-4-20
 */
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
