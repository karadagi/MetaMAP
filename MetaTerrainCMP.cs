using Grasshopper.Kernel;
using MetaMAP.Properties;
using Newtonsoft.Json;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Linq;

namespace MetaMap;

public class MetaTerrainCMP : GH_Component
{
    private double _currentCenterLat = 33.775678; // Default Atlanta
    private double _currentCenterLon = -84.395133; // Default Atlanta
    private string _lastDebugInfo = "";

    /// <summary>
    ///     Initializes a new instance of the MetaTerrainCMP class.
    /// </summary>
    public MetaTerrainCMP()
        : base("MetaTERRAIN", "MetaTERRAIN",
            $"Read terrain elevation data from OpenElevation. {Environment.NewLine}Use 'Show Points' to control visibility of elevation points.",
            "MetaMAP", "MetaMAP")
    {
    }

    /// <summary>
    ///     Provides an Icon for the component.
    /// </summary>
    protected override Bitmap Icon
    {
        get
        {
            if (!PlatformUtils.IsWindows())
                return null;

            var iconBytes = Resources.MetaMAP_terrain;
            if (iconBytes != null)
                using (var ms = new MemoryStream(iconBytes))
                {
                    return new Bitmap(ms);
                }

            return null;
        }
    }

    /// <summary>
    ///     Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid => new("B2C3D4E5-F6A7-8901-BCDE-F23456789012");

    /// <summary>
    ///     Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Latitude", "Lat", "Latitude for terrain query. Default: 33.775678 (Atlanta, GA)", GH_ParamAccess.item);
        pManager.AddNumberParameter("Longitude", "Lon", "Longitude for terrain query. Default: -84.395133 (Atlanta, GA)", GH_ParamAccess.item);
        pManager.AddNumberParameter("Radius", "R", "Search radius in meters for terrain extraction. Default: 100m", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Grid Resolution", "GR", "Grid resolution for elevation sampling. Default: 10 (10x10 grid)", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Show Points", "SP", "Show/hide terrain elevation points. Default: true", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Run", "Run", "Execute the terrain data query and processing", GH_ParamAccess.item);
        
        pManager[0].Optional = true;
        pManager[1].Optional = true;
        pManager[2].Optional = true;
        pManager[3].Optional = true;
        pManager[4].Optional = true;
        pManager[5].Optional = true;
    }

    /// <summary>
    ///     Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddMeshParameter("Terrain Mesh", "TM", "Generated terrain mesh with elevation data", GH_ParamAccess.item);
        pManager.AddPointParameter("Elevation Points", "EP", "Grid points with elevation data", GH_ParamAccess.list);
        pManager.AddNumberParameter("Elevation Values", "EV", "Elevation values in meters", GH_ParamAccess.list);
        pManager.AddTextParameter("Status", "S", "Processing status and information", GH_ParamAccess.item);
    }

    /// <summary>
    ///     This is the method that actually does the work.
    /// </summary>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        // Default values
        double radius = 100.0; // Default 100 meters
        int gridResolution = 10; // Default 10x10 grid
        bool showPoints = false; // Default show points
        bool run = false;

        // Get input values (with defaults if not provided)
        double lat = 33.775678; // Default Atlanta latitude
        double lon = -84.395133; // Default Atlanta longitude
        DA.GetData(0, ref lat);
        DA.GetData(1, ref lon);
        DA.GetData(2, ref radius);
        DA.GetData(3, ref gridResolution);
        DA.GetData(4, ref showPoints);
        DA.GetData(5, ref run);

        if (!run)
        {
            DA.SetData(0, null);
            DA.SetDataList(1, showPoints ? new List<Point3d>() : null);
            DA.SetDataList(2, showPoints ? new List<double>() : null);
            DA.SetData(3, $"MetaTERRAIN ready - Location: {lat:F6}, {lon:F6}, Radius: {radius}m, Grid: {gridResolution}x{gridResolution}. Points: {(showPoints ? "Visible" : "Hidden")}. Set 'Run' to true to execute.");
            return;
        }

        try
        {
            // Store the current center coordinates
            _currentCenterLat = lat;
            _currentCenterLon = lon;

            // Validate coordinates
            if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
            {
                throw new Exception("Invalid coordinates. Use latitude (-90 to 90) and longitude (-180 to 180)");
            }

            // Validate radius
            if (radius <= 0 || radius > 1000)
            {
                throw new Exception("Radius must be between 1 and 1000 meters");
            }

            // Validate grid resolution
            if (gridResolution < 3 || gridResolution > 50)
            {
                throw new Exception("Grid resolution must be between 3 and 50");
            }

            DA.SetData(3, "Fetching terrain data from multiple sources...");

            // Generate grid points for elevation sampling
            var gridPoints = GenerateGridPoints(lat, lon, radius, gridResolution);
            
            // Fetch elevation data from multiple sources
            var elevationData = FetchElevationData(gridPoints);
            
            DA.SetData(3, $"Processing terrain mesh... Found {elevationData.Count} elevation points");

            // Center terrain at Z=0 by subtracting minimum elevation
            if (elevationData.Count > 0)
            {
                double minElevation = elevationData.Min(ed => ed.Elevation);
                foreach (var ed in elevationData)
                {
                    ed.Point = new Point3d(ed.Point.X, ed.Point.Y, ed.Elevation - minElevation);
                    ed.Elevation = ed.Elevation - minElevation;
                }
            }
            
            // Create terrain mesh from elevation data (after centering)
            var terrainMesh = CreateTerrainMesh(elevationData, lat, lon, radius);
            
            // Output results
            var elevationPoints = elevationData.Select(ed => ed.Point).ToList();
            var elevationValues = elevationData.Select(ed => ed.Elevation).ToList();
            
            DA.SetData(0, terrainMesh);
            DA.SetDataList(1, showPoints ? elevationPoints : null);
            DA.SetDataList(2, showPoints ? elevationValues : null);
            DA.SetData(3, $"Successfully processed terrain data. Location: {lat:F6}, {lon:F6}, Radius: {radius}m, Grid: {gridResolution}x{gridResolution}. Points: {(showPoints ? "Visible" : "Hidden")}. {_lastDebugInfo}");
        }
        catch (Exception ex)
        {
            DA.SetData(0, null);
            DA.SetDataList(1, showPoints ? new List<Point3d>() : null);
            DA.SetDataList(2, showPoints ? new List<double>() : null);
            DA.SetData(3, $"Error: {ex.Message}");
        }
    }

    private List<Point3d> GenerateGridPoints(double centerLat, double centerLon, double radius, int gridResolution)
    {
        var points = new List<Point3d>();
        
        // Convert radius to degrees (approximate)
        double latDelta = radius / 111000.0; // 1 degree ≈ 111km
        double lonDelta = radius / (111000.0 * Math.Cos(centerLat * Math.PI / 180.0));
        
        double minLat = centerLat - latDelta;
        double maxLat = centerLat + latDelta;
        double minLon = centerLon - lonDelta;
        double maxLon = centerLon + lonDelta;
        
        // Generate grid points
        for (int i = 0; i < gridResolution; i++)
        {
            for (int j = 0; j < gridResolution; j++)
            {
                double lat = minLat + (maxLat - minLat) * i / (gridResolution - 1);
                double lon = minLon + (maxLon - minLon) * j / (gridResolution - 1);
                
                // Convert to local coordinates
                double x = (lon - _currentCenterLon) * 111320.0 * Math.Cos(_currentCenterLat * Math.PI / 180.0);
                double y = (lat - _currentCenterLat) * 110540.0;
                
                points.Add(new Point3d(x, y, 0)); // Z will be set by elevation data
            }
        }
        
        return points;
    }

    private List<ElevationData> FetchElevationData(List<Point3d> gridPoints)
    {
        var elevationData = new List<ElevationData>();
        
        try
        {
            // Try multiple elevation data sources
            var openElevationData = FetchFromOpenElevationAPI(gridPoints);
            if (openElevationData.Count > 0)
            {
                elevationData.AddRange(openElevationData);
                _lastDebugInfo = $"OpenElevation API: {openElevationData.Count} points";
            }
            else
            {
                // Fallback to OSM contour data
                var osmContourData = FetchFromOSMContours(gridPoints);
                if (osmContourData.Count > 0)
                {
                    elevationData.AddRange(osmContourData);
                    _lastDebugInfo = $"OSM Contours: {osmContourData.Count} points";
                }
                else
                {
                    // Final fallback: generate synthetic terrain
                    elevationData = GenerateSyntheticTerrain(gridPoints);
                    _lastDebugInfo = $"Synthetic terrain: {elevationData.Count} points";
                }
            }
        }
        catch (Exception ex)
        {
            // Fallback to synthetic terrain if all APIs fail
            elevationData = GenerateSyntheticTerrain(gridPoints);
            _lastDebugInfo = $"Fallback synthetic terrain: {ex.Message}";
        }
        
        return elevationData;
    }

    private List<ElevationData> FetchFromOpenElevationAPI(List<Point3d> gridPoints)
    {
        var elevationData = new List<ElevationData>();
        
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            // Prepare coordinates for OpenElevation API
            var coordinates = gridPoints.Select(p => 
            {
                // Convert back to lat/lon
                double lat = _currentCenterLat + (p.Y / 110540.0);
                double lon = _currentCenterLon + (p.X / (111320.0 * Math.Cos(_currentCenterLat * Math.PI / 180.0)));
                return new { latitude = lat, longitude = lon };
            }).ToList();
            
            var requestBody = new
            {
                locations = coordinates
            };
            
            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = httpClient.PostAsync("https://api.open-elevation.com/api/v1/lookup", content).Result;
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = response.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<OpenElevationResponse>(responseContent);
                
                if (result?.Results != null)
                {
                    for (int i = 0; i < result.Results.Count && i < gridPoints.Count; i++)
                    {
                        var point = gridPoints[i];
                        var elevation = result.Results[i].Elevation;
                        
                        elevationData.Add(new ElevationData
                        {
                            Point = new Point3d(point.X, point.Y, elevation),
                            Elevation = elevation,
                            Source = "OpenElevation"
                        });
                    }
                }
            }
        }
        catch
        {
            // Return empty list if API fails
        }
        
        return elevationData;
    }

    private List<ElevationData> FetchFromOSMContours(List<Point3d> gridPoints)
    {
        var elevationData = new List<ElevationData>();
        
        try
        {
            // Generate Overpass query for contour lines
            var bounds = CalculateBounds(gridPoints);
            string overpassQuery = GenerateContourQuery(bounds.South, bounds.West, bounds.North, bounds.East);
            
            var osmData = FetchOSMDataSync(overpassQuery);
            if (!string.IsNullOrEmpty(osmData))
            {
                var contours = ParseOSMContours(osmData);
                elevationData = InterpolateElevationsFromContours(gridPoints, contours);
            }
        }
        catch
        {
            // Return empty list if OSM query fails
        }
        
        return elevationData;
    }

    private List<ElevationData> GenerateSyntheticTerrain(List<Point3d> gridPoints)
    {
        var elevationData = new List<ElevationData>();
        var random = new Random(42); // Fixed seed for reproducible results
        
        foreach (var point in gridPoints)
        {
            // Generate synthetic terrain with some variation
            double distance = Math.Sqrt(point.X * point.X + point.Y * point.Y);
            double baseElevation = 100.0; // Base elevation
            double variation = Math.Sin(distance / 50.0) * 10.0; // Terrain variation
            double noise = (random.NextDouble() - 0.5) * 5.0; // Random noise
            
            double elevation = baseElevation + variation + noise;
            
            elevationData.Add(new ElevationData
            {
                Point = new Point3d(point.X, point.Y, elevation),
                Elevation = elevation,
                Source = "Synthetic"
            });
        }
        
        return elevationData;
    }

    private string GenerateContourQuery(double south, double west, double north, double east)
    {
        return $@"
[out:json][timeout:25];
(
  way[""contour""]({south},{west},{north},{east});
  way[""ele""]({south},{west},{north},{east});
);
out geom;";
    }

    private string FetchOSMDataSync(string query)
    {
        string[] endpoints = {
            "https://overpass-api.de/api/interpreter",
            "https://lz4.overpass-api.de/api/interpreter",
            "https://z.overpass-api.de/api/interpreter"
        };

        foreach (var endpoint in endpoints)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(15);

                var content = new StringContent(query, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");
                var response = httpClient.PostAsync(endpoint, content).Result;
                
                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsStringAsync().Result;
                    if (!string.IsNullOrEmpty(result) && result.Contains("elements"))
                    {
                        return result;
                    }
                }
            }
            catch
            {
                continue;
            }
        }

        throw new Exception("All OpenStreetMap API endpoints failed");
    }

    private List<ContourLine> ParseOSMContours(string jsonData)
    {
        var contours = new List<ContourLine>();
        
        try
        {
            var osmResponse = JsonConvert.DeserializeObject<OSMResponse>(jsonData);
            
            foreach (var element in osmResponse.Elements)
            {
                if (element.Type == "way" && element.Geometry != null && element.Tags != null)
                {
                    if (element.Tags.ContainsKey("contour") || element.Tags.ContainsKey("ele"))
                    {
                        double elevation = 0;
                        if (element.Tags.ContainsKey("ele") && double.TryParse(element.Tags["ele"], out double ele))
                        {
                            elevation = ele;
                        }
                        
                        var points = new List<Point3d>();
                        foreach (var coord in element.Geometry)
                        {
                            double x = (coord.Lon - _currentCenterLon) * 111320.0 * Math.Cos(_currentCenterLat * Math.PI / 180.0);
                            double y = (coord.Lat - _currentCenterLat) * 110540.0;
                            points.Add(new Point3d(x, y, elevation));
                        }
                        
                        if (points.Count > 1)
                        {
                            contours.Add(new ContourLine
                            {
                                Points = points,
                                Elevation = elevation
                            });
                        }
                    }
                }
            }
        }
        catch
        {
            // Return empty list if parsing fails
        }
        
        return contours;
    }

    private List<ElevationData> InterpolateElevationsFromContours(List<Point3d> gridPoints, List<ContourLine> contours)
    {
        var elevationData = new List<ElevationData>();
        
        foreach (var point in gridPoints)
        {
            double elevation = InterpolateElevationAtPoint(point, contours);
            
            elevationData.Add(new ElevationData
            {
                Point = new Point3d(point.X, point.Y, elevation),
                Elevation = elevation,
                Source = "OSM Contours"
            });
        }
        
        return elevationData;
    }

    private double InterpolateElevationAtPoint(Point3d point, List<ContourLine> contours)
    {
        if (contours.Count == 0)
            return 0;
        
        // Simple nearest neighbor interpolation
        double minDistance = double.MaxValue;
        double elevation = 0;
        
        foreach (var contour in contours)
        {
            foreach (var contourPoint in contour.Points)
            {
                double distance = point.DistanceTo(new Point3d(contourPoint.X, contourPoint.Y, 0));
                if (distance < minDistance)
                {
                    minDistance = distance;
                    elevation = contour.Elevation;
                }
            }
        }
        
        return elevation;
    }

    private Bounds CalculateBounds(List<Point3d> points)
    {
        if (points.Count == 0)
            return new Bounds { South = 0, West = 0, North = 0, East = 0 };
        
        double minX = points.Min(p => p.X);
        double maxX = points.Max(p => p.X);
        double minY = points.Min(p => p.Y);
        double maxY = points.Max(p => p.Y);
        
        // Convert back to lat/lon
        double south = _currentCenterLat + (minY / 110540.0);
        double north = _currentCenterLat + (maxY / 110540.0);
        double west = _currentCenterLon + (minX / (111320.0 * Math.Cos(_currentCenterLat * Math.PI / 180.0)));
        double east = _currentCenterLon + (maxX / (111320.0 * Math.Cos(_currentCenterLat * Math.PI / 180.0)));
        
        return new Bounds { South = south, West = west, North = north, East = east };
    }

    private Mesh CreateTerrainMesh(List<ElevationData> elevationData, double centerLat, double centerLon, double radius)
    {
        if (elevationData.Count < 3)
            return null;
        
        try
        {
            // Create a mesh from the elevation points using Delaunay triangulation
            var points = elevationData.Select(ed => ed.Point).ToArray();
            
            // Use Rhino's mesh creation from points
            var mesh = new Mesh();
            
            // Add vertices
            foreach (var point in points)
            {
                mesh.Vertices.Add(point);
            }
            
            // Create faces using simple grid triangulation
            int gridSize = (int)Math.Sqrt(elevationData.Count);
            if (gridSize > 1)
            {
                for (int i = 0; i < gridSize - 1; i++)
                {
                    for (int j = 0; j < gridSize - 1; j++)
                    {
                        int idx1 = i * gridSize + j;
                        int idx2 = i * gridSize + (j + 1);
                        int idx3 = (i + 1) * gridSize + j;
                        int idx4 = (i + 1) * gridSize + (j + 1);
                        
                        if (idx4 < points.Length)
                        {
                            // Create two triangles for each quad
                            mesh.Faces.AddFace(idx1, idx2, idx3);
                            mesh.Faces.AddFace(idx2, idx4, idx3);
                        }
                    }
                }
            }
            
            if (mesh.Faces.Count > 0)
            {
                mesh.Normals.ComputeNormals();
                mesh.Compact();
                return mesh;
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    // Data structures
    private class ElevationData
    {
        public Point3d Point { get; set; }
        public double Elevation { get; set; }
        public string Source { get; set; }
    }

    private class ContourLine
    {
        public List<Point3d> Points { get; set; }
        public double Elevation { get; set; }
    }

    private class Bounds
    {
        public double South { get; set; }
        public double West { get; set; }
        public double North { get; set; }
        public double East { get; set; }
    }

    private class OpenElevationResponse
    {
        [JsonProperty("results")]
        public List<ElevationResult> Results { get; set; }
    }

    private class ElevationResult
    {
        [JsonProperty("elevation")]
        public double Elevation { get; set; }
    }

    private class OSMResponse
    {
        [JsonProperty("elements")]
        public List<OSMElement> Elements { get; set; } = new List<OSMElement>();
    }

    private class OSMElement
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("geometry")]
        public List<OSMCoordinate> Geometry { get; set; }

        [JsonProperty("tags")]
        public Dictionary<string, string> Tags { get; set; }
    }

    private class OSMCoordinate
    {
        [JsonProperty("lat")]
        public double Lat { get; set; }

        [JsonProperty("lon")]
        public double Lon { get; set; }
    }
}
