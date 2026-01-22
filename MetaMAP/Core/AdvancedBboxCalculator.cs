using System;

namespace MetaMap.Core
{
    public static class AdvancedBboxCalculator
    {
        public static string CalculateBbox(double latitude, double longitude, double radiusMeters)
        {
            const double earthRadius = 6378137.0;
            double dLat = radiusMeters / earthRadius;
            double dLon = radiusMeters / (earthRadius * Math.Cos(Math.PI * latitude / 180.0));

            double latOffset = dLat * 180.0 / Math.PI;
            double lonOffset = dLon * 180.0 / Math.PI;

            double minLat = latitude - latOffset;
            double maxLat = latitude + latOffset;
            double minLon = longitude - lonOffset;
            double maxLon = longitude + lonOffset;

            return FormattableString.Invariant($"{minLon},{minLat},{maxLon},{maxLat},EPSG:4326");
        }
    }
}
