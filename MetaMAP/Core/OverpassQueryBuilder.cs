using System;

namespace MetaMap.Core
{
    public static class OverpassQueryBuilder
    {
        public static string BuildBuildingQuery(double latitude, double longitude, double radiusMeters)
        {
            var bbox = GeoMath.BoundingBox(latitude, longitude, radiusMeters);

            return FormattableString.Invariant($@"
[out:json][timeout:25];
(
  way[""building""]({bbox.South},{bbox.West},{bbox.North},{bbox.East});
  way[""building:part""]({bbox.South},{bbox.West},{bbox.North},{bbox.East});
  way[""building:use""]({bbox.South},{bbox.West},{bbox.North},{bbox.East});
  relation[""building""]({bbox.South},{bbox.West},{bbox.North},{bbox.East});
  relation[""building:part""]({bbox.South},{bbox.West},{bbox.North},{bbox.East});
  relation[""building:use""]({bbox.South},{bbox.West},{bbox.North},{bbox.East});
);
out geom;");
        }

        public static string BuildContourQuery(double south, double west, double north, double east)
        {
            return FormattableString.Invariant($@"
[out:json][timeout:25];
(
  way[""contour""]({south},{west},{north},{east});
  way[""ele""]({south},{west},{north},{east});
);
out geom;");
        }
    }
}
