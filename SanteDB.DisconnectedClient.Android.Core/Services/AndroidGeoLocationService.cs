using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.DisconnectedClient.Android.Core;
using SanteDB.DisconnectedClient.Core.Security;
using SanteDB.DisconnectedClient.Core.Services;

namespace SanteDB.DisconnectedClient.Android.Core.Services
{
    /// <summary>
    /// An Android application service which returns a geo-tag
    /// </summary>
    public class AndroidGeoLocationService : IGeoTaggingService
    {

        /// <summary>
        /// Android GeoTagging service
        /// </summary>
        public String ServiceName => "Android GeoTagging Service";

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(AndroidGeoLocationService));

        /// <summary>
        /// Get the current position
        /// </summary>
        /// <returns></returns>
        public GeoTag GetCurrentPosition()
        {
            try
            {
                var permService = ApplicationServiceContext.Current.GetService<IOperatingSystemSecurityService>();
                if (permService.HasPermission(PermissionType.GeoLocation) || permService.RequestPermission(PermissionType.GeoLocation))
                {
                    var locationManager = (ApplicationServiceContext.Current as AndroidApplicationContext).Context.GetSystemService(Context.LocationService) as LocationManager;
                    var lastKnownLocation = locationManager?.GetLastKnownLocation(locationManager.GetBestProvider(new Criteria()
                    {
                        Accuracy = Accuracy.Medium,
                        PowerRequirement = Power.Low
                    }, true));
                    if (lastKnownLocation != null)
                        return new GeoTag(lastKnownLocation.Latitude, lastKnownLocation.Longitude, lastKnownLocation.Accuracy > 0.5f);
                    else
                        return null;
                }
                else
                    return null;
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Cannot get location: {0}", e);
                return null;
            }
        }
    }
}