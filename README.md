# MetaMAP

MetaMAP is a [Grasshopper](https://www.grasshopper3d.com/) plugin for Rhino that provides tools to fetch and process geospatial data from OpenStreetMap (OSM) and other sources. It allows you to create 3D models of buildings and terrain for architectural and urban planning purposes.

## Components

MetaMAP consists of three main components:

### 1. MetaBuilding

The `MetaBuilding` component extracts building data from OpenStreetMap.

**Features:**

- Fetches building footprints based on latitude, longitude, and radius.
- Extracts building heights from OSM data or uses default values.
- Creates 2D building projection meshes.
- Aligns buildings with a terrain mesh for accurate placement.

**Inputs:**

- `Latitude` (Number): The latitude for the center of the query.
- `Longitude` (Number): The longitude for the center of the query.
- `Radius` (Number): The search radius in meters.
- `Terrain Mesh` (Mesh): An optional terrain mesh to align the buildings with.
- `Run` (Boolean): A boolean toggle to execute the data fetching and processing.

**Outputs:**

- `Building Meshes` (Mesh): A list of 2D building footprint meshes.
- `Building Heights` (Number): A list of building heights in meters.
- `Status` (Text): The processing status and other information.

### 2. MetaTerrain

The `MetaTerrain` component fetches elevation data to create a terrain mesh.

**Features:**

- Fetches elevation data from the Open-Elevation API.
- Falls back to OSM contour data or generates synthetic terrain if the primary source is unavailable.
- Creates a Delaunay-triangulated terrain mesh.
- Outputs elevation points and values for further analysis.

**Inputs:**

- `Latitude` (Number): The latitude for the center of the query.
- `Longitude` (Number): The longitude for the center of the query.
- `Radius` (Number): The search radius in meters.
- `Grid Resolution` (Integer): The resolution of the grid for elevation sampling.
- `Show Points` (Boolean): A boolean to control the visibility of elevation points.
- `Run` (Boolean): A boolean toggle to execute the data fetching and processing.

**Outputs:**

- `Terrain Mesh` (Mesh): The generated terrain mesh.
- `Elevation Points` (Point): A list of points with elevation data.
- `Elevation Values` (Number): A list of elevation values in meters.
- `Status` (Text): The processing status and other information.

### 3. MetaFetch

The `MetaFetch` component provides an interactive map to select a location and get its coordinates.

**Features:**

- Opens an interactive map window.
- Allows you to search for a location by name.
- Fetches the latitude and longitude of the selected location.

**Inputs:**

- `Show Map` (Boolean): A boolean to open the map window.

**Outputs:**

- `Latitude` (Number): The latitude of the selected location.
- `Longitude` (Number): The longitude of the selected location.

## How to Use

1.  Install the `MetaMAP.gha` file in your Grasshopper `Components` folder.
2.  Open Grasshopper in Rhino.
3.  You will find the MetaMAP components under the "MetaMAP" tab.
4.  Use the `MetaFetch` component to pick a location.
5.  Connect the `Latitude` and `Longitude` outputs of `MetaFetch` to the corresponding inputs of `MetaBuilding` and `MetaTerrain`.
6.  Adjust the `Radius` and other parameters as needed.
7.  Set the `Run` input to `True` to fetch the data and generate the geometry.

## Dependencies

- [Rhino](https://www.rhino3d.com/)
- [Grasshopper](https://www.grasshopper3d.com/)

## Disclaimer

This plugin relies on external APIs such as OpenStreetMap and Open-Elevation. The availability and reliability of these services may vary. Please use this tool responsibly and respect the terms of use of the respective data providers.