using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SanteDB.DisconnectedClient.Core.Security;
using A = Android;

namespace SanteDB.DisconnectedClient.Android.Core.Activities
{
    /// <summary>
    /// An operating system security service which is for Android OS
    /// </summary>
    public abstract class AndroidActivityBase : Activity, IOperatingSystemSecurityService
    {
        private ManualResetEvent m_permissionEvent = new ManualResetEvent(false);

        /// <summary>
        /// Return true if this object has permission
        /// </summary>
        public bool HasPermission(PermissionType permission)
        {
            if ((int)Build.VERSION.SdkInt < 23)
                return true;
            else
                switch (permission)
                {
                    case PermissionType.FileSystem:
                        return this.CheckSelfPermission(A.Manifest.Permission.WriteExternalStorage) == (int)A.Content.PM.Permission.Granted;
                    case PermissionType.GeoLocation:
                        return this.CheckSelfPermission(A.Manifest.Permission.AccessCoarseLocation) == (int)A.Content.PM.Permission.Granted;
                    default:
                        return false;
                }
        }

        /// <summary>
        /// Requests permission
        /// </summary>
        public bool RequestPermission(PermissionType permission)
        {
            if ((int)Build.VERSION.SdkInt < 23)
                return true;
            else
            {
                this.m_permissionEvent.Reset();
                String permissionString = String.Empty;
                switch (permission)
                {
                    case PermissionType.FileSystem:
                        permissionString = A.Manifest.Permission.WriteExternalStorage;
                        break;
                    case PermissionType.GeoLocation:
                        permissionString = A.Manifest.Permission.AccessCoarseLocation;
                        break;
                    case PermissionType.Camera:
                        permissionString = A.Manifest.Permission.Camera;
                        break;
                    default:
                        return false;
                }
                this.RequestPermissions(new string[] { permissionString }, 0);
                this.m_permissionEvent.WaitOne();
                return this.CheckSelfPermission(permissionString) == A.Content.PM.Permission.Granted;
            }
        }

        /// <summary>
        /// Request permission result
        /// </summary>
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] A.Content.PM.Permission[] grantResults)
        {
            this.m_permissionEvent.Set();
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

    }
}