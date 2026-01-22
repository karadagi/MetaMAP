using System;

namespace MetaMap.Core
{
    public static class GeoMath
    {
        public static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        public static double LatDeltaFromMeters(double meters)
        {
            return meters / 111000.0;
        }

        public static double LonDeltaFromMeters(double meters, double atLatitudeDegrees)
        {
            return meters / (111000.0 * Math.Cos(ToRadians(atLatitudeDegrees)));
        }

        public static (double South, double West, double North, double East) BoundingBox(double latitude, double longitude, double radiusMeters)
        {
            double latDelta = LatDeltaFromMeters(radiusMeters);
            double lonDelta = LonDeltaFromMeters(radiusMeters, latitude);

            double minLat = latitude - latDelta;
            double maxLat = latitude + latDelta;
            double minLon = longitude - lonDelta;
            double maxLon = longitude + lonDelta;

            double south = Math.Min(minLat, maxLat);
            double north = Math.Max(minLat, maxLat);
            double west = Math.Min(minLon, maxLon);
            double east = Math.Max(minLon, maxLon);

            return (south, west, north, east);
        }

        public static (double X, double Y) ToLocalXY(double latitude, double longitude, double centerLatitude, double centerLongitude)
        {
            double x = (longitude - centerLongitude) * 111320.0 * Math.Cos(ToRadians(centerLatitude));
            double y = (latitude - centerLatitude) * 110540.0;

            return (x, y);
        }
    }
}
