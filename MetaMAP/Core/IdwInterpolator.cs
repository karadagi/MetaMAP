using System;
using System.Collections.Generic;
using System.Linq;

namespace MetaMap.Core
{
    public static class IdwInterpolator
    {
        public readonly struct ElevationPoint
        {
            public ElevationPoint(double x, double y, double elevation)
            {
                X = x;
                Y = y;
                Elevation = elevation;
            }

            public double X { get; }
            public double Y { get; }
            public double Elevation { get; }
        }

        public static double InterpolateElevation(double x, double y, IReadOnlyList<ElevationPoint> points, int k = 8, double power = 2.0)
        {
            if (points == null || points.Count == 0)
                return 0.0;

            var neighbors = points
                .Select(pt => new { Point = pt, Distance = Distance2D(x, y, pt.X, pt.Y) })
                .OrderBy(pt => pt.Distance)
                .Take(k)
                .ToList();

            foreach (var neighbor in neighbors)
            {
                if (neighbor.Distance < 0.001)
                    return neighbor.Point.Elevation;
            }

            double numerator = 0.0;
            double denominator = 0.0;
            foreach (var neighbor in neighbors)
            {
                double weight = 1.0 / Math.Pow(neighbor.Distance, power);
                numerator += weight * neighbor.Point.Elevation;
                denominator += weight;
            }

            if (denominator > 0)
                return numerator / denominator;

            return neighbors.FirstOrDefault().Point.Elevation;
        }

        private static double Distance2D(double x1, double y1, double x2, double y2)
        {
            double dx = x1 - x2;
            double dy = y1 - y2;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
