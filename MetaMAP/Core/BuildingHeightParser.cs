using System;
using System.Collections.Generic;

namespace MetaMap.Core
{
    public static class BuildingHeightParser
    {
        public static string GetBuildingType(IReadOnlyDictionary<string, string> tags)
        {
            if (tags != null && tags.TryGetValue("building", out var buildingType))
                return buildingType;

            return "Unknown";
        }

        public static double GetHeightMeters(IReadOnlyDictionary<string, string> tags)
        {
            if (tags != null)
            {
                if (tags.TryGetValue("height", out var heightStr))
                {
                    var normalized = heightStr.Replace("m", "").Replace("ft", "");
                    if (double.TryParse(normalized, out var height))
                    {
                        if (heightStr.Contains("ft"))
                            return height * 0.3048;
                        return height;
                    }
                }

                if (tags.TryGetValue("building:levels", out var levelsStr) &&
                    int.TryParse(levelsStr, out var levels))
                {
                    return Math.Max(levels * 3.0, 3.0);
                }
            }

            var buildingType = GetBuildingType(tags).ToLowerInvariant();
            return buildingType switch
            {
                "house" or "residential" => 6.0,
                "apartments" => 12.0,
                "commercial" or "retail" => 8.0,
                "industrial" => 10.0,
                "school" or "hospital" => 15.0,
                _ => 6.0
            };
        }
    }
}
