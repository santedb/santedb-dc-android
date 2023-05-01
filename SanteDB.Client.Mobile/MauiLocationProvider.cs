using SanteDB.Client.Services;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Client.Mobile
{
    [PreferredService(typeof(IGeographicLocationProvider))]
    public class MauiLocationProvider : IGeographicLocationProvider
    {
        public string ServiceName => nameof(MauiLocationProvider);

        public GeoTag GetCurrentPosition()
        {
            var location = Nito.AsyncEx.AsyncContext.Run(GetCurrentLocationAsync);

            if (null != location && !location.IsFromMockProvider)
            {
                return new GeoTag(location.Latitude, location.Longitude, location.Accuracy < 101d);
            }

            return null;
        }

        private async Task<Location> GetCurrentLocationAsync()
        {
            try
            {
                var result = await Geolocation.Default.GetLastKnownLocationAsync();

                if (result != null && (DateTimeOffset.UtcNow - result.Timestamp) < TimeSpan.FromMinutes(2))
                {
                    return result;
                }

                var request = new GeolocationRequest(GeolocationAccuracy.Medium);

#if IOS
                request.RequestFullAccuracy = true;
#endif

                result = await Geolocation.Default.GetLocationAsync(request);

                return result;
            }
            catch (FeatureNotSupportedException)
            {
            }
            catch (FeatureNotEnabledException)
            {
            }
            catch (PermissionException)
            {
            }

            return null;
        }
    }
}
