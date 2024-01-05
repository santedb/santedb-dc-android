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
 * Date: 2023-4-28
 */
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
                if ((await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>()) != PermissionStatus.Granted)
                {
                    if ((await Permissions.RequestAsync<Permissions.LocationWhenInUse>() != PermissionStatus.Granted))
                    {
                        return null;
                    }
                }

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
