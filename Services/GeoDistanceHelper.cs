using System;

namespace SkyTech.Services
{
    public static class GeoDistanceHelper
    {
        // Returns distance in meters between two lat/lng points
        public static double DistanceMeters(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371000; // Earth radius in meters
            var dLat = ToRad(lat2 - lat1);
            var dLng = ToRad(lng2 - lng1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRad(double deg) => deg * Math.PI / 180;
    }
}