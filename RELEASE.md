# MetaMAP - Automated GitHub Releases

## How to Release a New Version

1. **Update the version** in `MetaMAP.csproj`:
   ```xml
   <Version>0.0.30</Version>
   <AssemblyVersion>0.0.30</AssemblyVersion>
   <FileVersion>0.0.30</FileVersion>
   ```

2. **Commit your changes**:
   ```bash
   git add .
   git commit -m "Release v0.0.30"
   ```

3. **Create and push a version tag**:
   ```bash
   git tag v0.0.30
   git push origin main
   git push origin v0.0.30
   ```

4. **GitHub Actions will automatically**:
   - Build the project
   - Create a new GitHub Release
   - Upload `MetaMAP_Manual_New.zip` to the release

## Update the GitHub Repository URL

In `MetaUpdateCMP.cs`, replace `YOUR_USERNAME` with your actual GitHub username:

```csharp
string apiUrl = "https://api.github.com/repos/YOUR_USERNAME/MetaMAP/releases/latest";
```

For example, if your username is `ilkerkaradag`:
```csharp
string apiUrl = "https://api.github.com/repos/ilkerkaradag/MetaMAP/releases/latest";
```

## How the Auto-Update Works

1. Users click the "Update" button in the MetaUPDATE component
2. The component queries GitHub API for the latest release
3. Downloads `MetaMAP_Manual_New.zip` from the latest release
4. Compares versions and installs if newer
5. Falls back to `http://archidynamics.com/MetaMAP_Manual_New.zip` if GitHub is unavailable

## Benefits

- ✅ **Automatic builds** - No manual zip creation
- ✅ **Version control** - All releases tracked in GitHub
- ✅ **Reliable hosting** - GitHub's CDN
- ✅ **Fallback URL** - Still works if GitHub is down
- ✅ **Easy rollback** - Can download any previous release
