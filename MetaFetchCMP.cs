using Eto.Drawing;
using Eto.Forms;
using Grasshopper.Kernel;
using MetaMAP.Properties;
using Rhino;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
namespace MetaMap
{
    public class MetaFetchCMP : GH_Component
    {
        private double _lat, _lng;
        private bool _hasValue;
        private WebView _currentWebView;
        private Form _currentForm;

        public MetaFetchCMP()
          : base("MetaFETCH", "MetaFETCH", $"Interactive location picker for fetching coordinates. {Environment.NewLine} Use the 'Fetch Location' button to get coordinates.", "MetaMAP", "MetaMAP")
        { }

        public override Guid ComponentGuid => new Guid("7ECA432E-26BB-4E97-8A5D-A1C98D319888");
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                if (!PlatformUtils.IsWindows())
                    return null;

                var iconBytes = Resources.MetaMAP_fetch;
                if (iconBytes != null)
                    // Convert byte array to a MemoryStream
                    using (var ms = new MemoryStream(iconBytes))
                    {
                        // Return the Bitmap from the stream
                        return new System.Drawing.Bitmap(ms);
                    }

                return null; // Fallback in case iconBytes is null
            }
        }
        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddBooleanParameter("Show Map", "S", "Opens the map window", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddNumberParameter("Latitude", "Lat", "Clicked latitude", GH_ParamAccess.item);
            p.AddNumberParameter("Longitude", "Lng", "Clicked longitude", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool show = false;
            
            if (!DA.GetData(0, ref show)) return;

            if (show)
            {
                RhinoApp.InvokeOnUiThread((Action)ShowMapWindow);
            }

            if (_hasValue)
            {
                DA.SetData(0, _lat);
                DA.SetData(1, _lng);
                RhinoApp.WriteLine($"OSM Picker: Returning lat={_lat}, lng={_lng}");
            }
            else
            {
                DA.SetData(0, null);
                DA.SetData(1, null);
                RhinoApp.WriteLine("OSM Picker: No value selected yet");
            }
        }

        private void ShowMapWindow()
        {
            try
            {
                // Check if map window is already open
                if (_currentForm != null && !_currentForm.IsDisposed)
                {
                    _currentForm.BringToFront();
                    RhinoApp.WriteLine("Map window is already open - bringing to front");
                    return;
                }

                // Check if WebView is supported on this platform
                if (!PlatformUtils.IsWebViewSupported())
                {
                    RhinoApp.WriteLine($"WebView not supported on {PlatformUtils.GetCurrentPlatform()}. Using fallback method.");
                    PlatformUtils.ShowCoordinateInputDialog((lat, lng) => {
                        _lat = lat;
                        _lng = lng;
                        _hasValue = true;
                        RhinoApp.WriteLine($"Manual input: lat={_lat}, lng={_lng}");
                        
                        // Trigger Grasshopper recompute
                        var doc = OnPingDocument();
                        if (doc != null)
                        {
                            doc.ScheduleSolution(1, d => ExpireSolution(false));
                        }
                    });
                    return;
                }

            var html = @"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'/>
<link rel='stylesheet' href='https://unpkg.com/leaflet/dist/leaflet.css'/>
<script src='https://unpkg.com/leaflet/dist/leaflet.js'></script>
<style>
html,body,#map{height:100%;margin:0;padding:0}
#searchContainer {
  position: absolute;
  top: 10px;
  left: 10px;
  z-index: 1000;
  background: white;
  border-radius: 5px;
  box-shadow: 0 2px 5px rgba(0,0,0,0.2);
  padding: 5px;
  display: flex;
  gap: 5px;
}
#searchInput {
  border: 1px solid #ccc;
  border-radius: 3px;
  padding: 8px 12px;
  font-size: 14px;
  width: 200px;
  outline: none;
}
#searchInput:focus {
  border-color: #007cba;
}
#searchButton {
  background: #007cba;
  color: white;
  border: none;
  padding: 8px 12px;
  border-radius: 3px;
  cursor: pointer;
  font-size: 14px;
}
#searchButton:hover {
  background: #005a87;
}
#fetchButton {
  position: absolute;
  top: 10px;
  right: 10px;
  z-index: 1000;
  background: #007cba;
  color: white;
  border: none;
  padding: 10px 15px;
  border-radius: 5px;
  cursor: pointer;
  font-size: 14px;
  box-shadow: 0 2px 5px rgba(0,0,0,0.2);
}
#fetchButton:hover {
  background: #005a87;
}
</style>
</head>
<body>
<div id='map'></div>
<div id='searchContainer'>
  <input type='text' id='searchInput' placeholder='Search for a location...' />
  <button id='searchButton'>Search</button>
</div>
<button id='fetchButton'>Fetch Location</button>
<script>
var map=L.map('map', {zoomControl: false}).setView([33.749,-84.388],12);
L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',{maxZoom:19}).addTo(map);

// Store current map center
var currentCenter = map.getCenter();

// Update center when map moves
map.on('moveend', function() {
  currentCenter = map.getCenter();
});

// Search functionality
function searchLocation(query) {
  if (!query.trim()) return;
  
  // Use Nominatim (OpenStreetMap's geocoding service)
  fetch('https://nominatim.openstreetmap.org/search?format=json&q=' + encodeURIComponent(query) + '&limit=1')
    .then(response => response.json())
    .then(data => {
      if (data && data.length > 0) {
        var result = data[0];
        var lat = parseFloat(result.lat);
        var lon = parseFloat(result.lon);
        
        // Center map on search result
        map.setView([lat, lon], 15);
        
        // Add marker for search result
        if (window.searchMarker) {
          map.removeLayer(window.searchMarker);
        }
        window.searchMarker = L.marker([lat, lon]).addTo(map);
        window.searchMarker.bindPopup('<b>' + result.display_name + '</b>').openPopup();
        
        console.log('Found location:', result.display_name, 'at', lat, lon);
      } else {
        alert('Location not found. Please try a different search term.');
      }
    })
    .catch(error => {
      console.error('Search error:', error);
      alert('Search failed. Please check your internet connection.');
    });
}

// Search button click handler
document.getElementById('searchButton').addEventListener('click', function() {
  var query = document.getElementById('searchInput').value;
  searchLocation(query);
});

// Search on Enter key
document.getElementById('searchInput').addEventListener('keypress', function(e) {
  if (e.key === 'Enter') {
    var query = this.value;
    searchLocation(query);
  }
});

// Fetch button click handler - only way to fetch location
document.getElementById('fetchButton').addEventListener('click', function() {
  var center = map.getCenter();
  document.title='callback://' + center.lat + ',' + center.lng;
});
</script>
</body>
</html>";

            var form = new Form
            {
                Title = "MetaFETCH - Location Picker",
                Size = new Eto.Drawing.Size(600, 400)
            };
            _currentForm = form; // Store reference to prevent multiple windows

            WebView web = null;
            try
            {
                web = new WebView();
                _currentWebView = web; // Store reference for fetch functionality
                
                // Load HTML with error handling
                web.LoadHtml(html, new Uri("https://openstreetmap.org/"));
                RhinoApp.WriteLine("WebView initialized successfully");
            }
            catch (Exception webEx)
            {
                RhinoApp.WriteLine($"WebView initialization failed: {webEx.Message}");
                form.Close();
                PlatformUtils.ShowCoordinateInputDialog((lat, lng) => {
                    _lat = lat;
                    _lng = lng;
                    _hasValue = true;
                    RhinoApp.WriteLine($"Manual input: lat={_lat}, lng={_lng}");
                    
                    // Trigger Grasshopper recompute
                    var doc = OnPingDocument();
                    if (doc != null)
                    {
                        doc.ScheduleSolution(1, d => ExpireSolution(false));
                    }
                });
                return;
            }
            
            // Use DocumentTitleChanged event as a more reliable callback mechanism
            web.DocumentTitleChanged += (s, e) =>
            {
                try
                {
                    var title = web.DocumentTitle;
                    RhinoApp.WriteLine($"Document title changed: {title}");
                    
                    if (!string.IsNullOrEmpty(title) && title.StartsWith("callback://"))
                    {
                        var parts = title.Replace("callback://", "").Split(',');
                        RhinoApp.WriteLine($"Parsing callback: {string.Join(", ", parts)}");
                        
                        if (parts.Length == 2 &&
                            double.TryParse(parts[0], out double newLat) &&
                            double.TryParse(parts[1], out double newLng))
                        {
                            _lat = newLat;
                            _lng = newLng;
                            _hasValue = true;

                            RhinoApp.WriteLine($"Successfully parsed coordinates: {_lat}, {_lng}");

                            // 🔁 trigger Grasshopper to recompute
                            var doc = OnPingDocument();
                            if (doc != null)
                            {
                                // schedule recompute safely on GH main thread
                                doc.ScheduleSolution(1, d => ExpireSolution(false));
                            }
                        }
                        else
                        {
                            RhinoApp.WriteLine($"Failed to parse coordinates from: {title}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Error in DocumentTitleChanged event: {ex.Message}");
                }
            };

            form.Content = web;
            
            // Clean up references when form is closed
            form.Closed += (s, e) =>
            {
                try
                {
                    _currentForm = null;
                    _currentWebView = null;
                    RhinoApp.WriteLine("Map window closed");
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Error during form cleanup: {ex.Message}");
                }
            };
            
            form.Show();
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error creating map window: {ex.Message}");
                PlatformUtils.ShowCoordinateInputDialog((lat, lng) => {
                    _lat = lat;
                    _lng = lng;
                    _hasValue = true;
                    RhinoApp.WriteLine($"Manual input: lat={_lat}, lng={_lng}");
                    
                    // Trigger Grasshopper recompute
                    var doc = OnPingDocument();
                    if (doc != null)
                    {
                        doc.ScheduleSolution(1, d => ExpireSolution(false));
                    }
                });
            }
        }
      }
    }