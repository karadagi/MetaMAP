using System.Collections.Generic;

namespace MetaMap.Core
{
    public static class GridGenerator
    {
        public readonly struct LocalPoint
        {
            public LocalPoint(double x, double y)
            {
                X = x;
                Y = y;
            }

            public double X { get; }
            public double Y { get; }
        }

        public static List<LocalPoint> GenerateLocalGridPoints(double centerLatitude, double centerLongitude, double radiusMeters, int gridResolution)
        {
            var points = new List<LocalPoint>();

            var latDelta = GeoMath.LatDeltaFromMeters(radiusMeters);
            var lonDelta = GeoMath.LonDeltaFromMeters(radiusMeters, centerLatitude);

            double minLat = centerLatitude - latDelta;
            double maxLat = centerLatitude + latDelta;
            double minLon = centerLongitude - lonDelta;
            double maxLon = centerLongitude + lonDelta;

            for (int i = 0; i < gridResolution; i++)
            {
                for (int j = 0; j < gridResolution; j++)
                {
                    double lat = minLat + (maxLat - minLat) * i / (gridResolution - 1);
                    double lon = minLon + (maxLon - minLon) * j / (gridResolution - 1);

                    var local = GeoMath.ToLocalXY(lat, lon, centerLatitude, centerLongitude);
                    points.Add(new LocalPoint(local.X, local.Y));
                }
            }

            return points;
        }
    }
}
