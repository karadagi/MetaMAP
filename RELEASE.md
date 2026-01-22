# MetaMAP - GitHub Releases + Yak Packaging

## Release workflow (tagged releases)

1. Update the version in `MetaMAP.csproj`:
   ```xml
   <Version>0.0.56</Version>
   <AssemblyVersion>0.0.56</AssemblyVersion>
   <FileVersion>0.0.56</FileVersion>
   ```

2. Commit your changes:
   ```bash
   git add .
   git commit -m "Release v0.0.56"
   ```

3. Tag and push:
   ```bash
   git tag v0.0.56
   git push origin main
   git push origin v0.0.56
   ```

4. GitHub Actions will:
   - Build and test
   - Create `MetaMAP_Manual_New.zip`
   - Build a Rhino Yak package (`.yak`)
   - Publish a GitHub Release with both artifacts
   - Publish to Yak if `YAK_TOKEN` secret is set

## Yak package notes

- The Yak manifest is generated during CI by `scripts/pack-yak.ps1`.
- The package name is `metamap`.
- The build includes:
  - `MetaMAP.gha`
  - Dependency DLLs
  - `Templates/`
  - `version.txt`
  - `README.md`
  - `icon.png` (from `Resources/MetaBuilding.png`)

## Local packaging

```powershell
dotnet build -c Release
.\scripts\pack-yak.ps1 -Configuration Release
yak build --platform win -o .\artifacts\yak
```
