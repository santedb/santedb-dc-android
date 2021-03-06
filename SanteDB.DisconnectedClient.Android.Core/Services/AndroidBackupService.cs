/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 * 
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
 * User: fyfej
 * Date: 2017-10-30
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SanteDB.DisconnectedClient;
using SanteDB.DisconnectedClient.Security;
using SanteDB.DisconnectedClient.Services;
using SanteDB.DisconnectedClient.Backup;
using A = Android;

namespace SanteDB.DisconnectedClient.Android.Core.Services
{
    /// <summary>
    /// Android 
    /// </summary>
    public class AndroidBackupService : DefaultBackupService
    {
        /// <summary>
        /// Get backup directory
        /// </summary>
        protected override string GetBackupDirectory(BackupMedia media)
        {
            switch (media)
            {
                case BackupMedia.Private:
                    return System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                case BackupMedia.Public:
                    var ossec = ApplicationContext.Current.GetService<IOperatingSystemSecurityService>();
                    if (ossec.HasPermission(PermissionType.FileSystem) ||
                        ossec.RequestPermission(PermissionType.FileSystem))
                    { 
                        var retVal = A.App.Application.Context.GetExternalFilesDir("").AbsolutePath;
                        if (!System.IO.Directory.Exists(retVal))
                            System.IO.Directory.CreateDirectory(retVal);
                        return retVal;
                    }
                    else 
                        return System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                default:
                    throw new PlatformNotSupportedException("Don't support external media on this platform");
            }
        }
    }
}