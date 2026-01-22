param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "artifacts\\yak",
    [string]$ProjectPath = "MetaMAP\\MetaMAP.csproj"
)

$ErrorActionPreference = "Stop"

$projectFullPath = Join-Path (Get-Location) $ProjectPath
if (-not (Test-Path $projectFullPath)) {
    throw "Project not found: $projectFullPath"
}

[xml]$csproj = Get-Content $projectFullPath
$versionNode = $csproj.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1
if (-not $versionNode -or -not $versionNode.Version) {
    throw "Version not found in $ProjectPath"
}

$version = $versionNode.Version.Trim()
$targetFramework = "net8.0-windows"

$buildOutput = Join-Path (Get-Location) "MetaMAP\\bin\\$Configuration\\$targetFramework"
if (-not (Test-Path $buildOutput)) {
    throw "Build output not found: $buildOutput. Run dotnet build first."
}

$distRoot = Join-Path (Get-Location) $OutputDir
if (Test-Path $distRoot) {
    Remove-Item -Recurse -Force $distRoot
}
New-Item -ItemType Directory -Path $distRoot | Out-Null

# Copy plugin output
Copy-Item -Path (Join-Path $buildOutput "MetaMAP.gha") -Destination $distRoot -Force
Copy-Item -Path (Join-Path $buildOutput "*.dll") -Destination $distRoot -Force
Copy-Item -Path (Join-Path $buildOutput "*.deps.json") -Destination $distRoot -Force -ErrorAction SilentlyContinue
Copy-Item -Path (Join-Path $buildOutput "version.txt") -Destination $distRoot -Force -ErrorAction SilentlyContinue

# Copy templates
$templatesOut = Join-Path $distRoot "Templates"
Copy-Item -Path (Join-Path $buildOutput "Templates") -Destination $templatesOut -Recurse -Force -ErrorAction SilentlyContinue

# Add icon
Copy-Item -Path (Join-Path (Get-Location) "MetaMAP\\Resources\\MetaBuilding.png") -Destination (Join-Path $distRoot "icon.png") -Force

# Add misc docs
$miscDir = Join-Path $distRoot "misc"
New-Item -ItemType Directory -Path $miscDir | Out-Null
Copy-Item -Path (Join-Path (Get-Location) "README.md") -Destination (Join-Path $miscDir "README.md") -Force
if (Test-Path (Join-Path (Get-Location) "RELEASE.md")) {
    Copy-Item -Path (Join-Path (Get-Location) "RELEASE.md") -Destination (Join-Path $miscDir "RELEASE.md") -Force
}

# Write manifest
$manifestPath = Join-Path $distRoot "manifest.yml"
$manifest = @"
---
name: metamap
version: $version
authors:
- Ilker Karadag
description: >
  MetaMAP component for advanced mapping and analysis.
url: https://archidynamics.com/
icon: icon.png
keywords:
- metamap
- mapping
- analysis
- grasshopper
- rhino
"@

Set-Content -Path $manifestPath -Value $manifest

Write-Host "Yak dist prepared at $distRoot"
