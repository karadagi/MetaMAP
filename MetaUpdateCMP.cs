using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using MetaMAP.Properties;

namespace MetaMap
{
    public class MetaUpdateCMP : GH_Component
    {
        private string _statusMessage = "Idle";
        private bool _isUpdating = false;

        public MetaUpdateCMP()
          : base("MetaUPDATE", "MetaUPDATE",
              "Update MetaMAP to the latest version from a URL",
              "MetaMAP", "MetaMAP")
        {
        }

        public override Guid ComponentGuid => new Guid("12345678-1234-1234-1234-123456789012");

        protected override Bitmap Icon
        {
            get
            {
                if (!PlatformUtils.IsWindows())
                    return null;

                var iconBytes = Resources.ResourceManager.GetObject("MetaMAP_update") as byte[];
                if (iconBytes != null)
                    using (var ms = new MemoryStream(iconBytes))
                    {
                        return new Bitmap(ms);
                    }

                return null;
            }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Update", "Upd", "Set to true to start update", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Update status", GH_ParamAccess.item);
            pManager.AddTextParameter("Your Version", "V", "Currently installed version", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool update = false;

            if (!DA.GetData(0, ref update)) return;

            // Reset to Idle only when button is released and we are not currently updating
            if (!update && !_isUpdating)
            {
                _statusMessage = "Idle";
            }

            // Only start if update is requested, we aren't already running, AND we are in Idle state
            // This prevents the component from restarting immediately after finishing (Success or Fail)
            // while the button is still held down.
            if (update && !_isUpdating && _statusMessage == "Idle")
            {
                _isUpdating = true;
                _statusMessage = "Starting update...";
                
                // Run update asynchronously to avoid freezing UI
                string updateUrl = "http://archidynamics.com/MetaMAP_Manual_New.zip";
                Task.Run(() => PerformUpdate(updateUrl));
            }

            DA.SetData(0, _statusMessage);
            DA.SetData(1, GetCurrentVersion());
        }

        private string GetCurrentVersion()
        {
            try
            {
                string installDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string versionFile = Path.Combine(installDir, "version.txt");

                if (File.Exists(versionFile))
                {
                    return File.ReadAllText(versionFile).Trim();
                }
            }
            catch
            {
                // Ignore errors reading file
            }

            // Fallback to assembly version
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private async Task PerformUpdate(string url)
        {
            try
            {
                _statusMessage = "Downloading update...";
                UpdateStatus();

                string tempFile = Path.GetTempFileName();
                string installDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                using (var client = new HttpClient())
                {
                    // Add User-Agent to avoid being blocked by some servers
                    client.DefaultRequestHeaders.Add("User-Agent", "MetaMAP-Updater");
                    
                    // Append timestamp to URL to prevent caching
                    string downloadUrl = url;
                    if (url.Contains("?"))
                        downloadUrl += $"&t={DateTime.Now.Ticks}";
                    else
                        downloadUrl += $"?t={DateTime.Now.Ticks}";

                    // Download file
                    var response = await client.GetAsync(downloadUrl);
                    response.EnsureSuccessStatusCode();
                    
                    using (var fs = new FileStream(tempFile, FileMode.Create))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }

                _statusMessage = "Checking version...";
                UpdateStatus();

                // Check version before installing
                bool isNewVersion = true;
                string remoteVersion = "";

                using (var archive = ZipFile.OpenRead(tempFile))
                {
                    var versionEntry = archive.GetEntry("version.txt");
                    if (versionEntry != null)
                    {
                        using (var reader = new StreamReader(versionEntry.Open()))
                        {
                            remoteVersion = reader.ReadToEnd().Trim();
                        }

                        string localVersion = GetCurrentVersion();
                        if (string.Equals(remoteVersion, localVersion, StringComparison.OrdinalIgnoreCase))
                        {
                            isNewVersion = false;
                        }
                    }
                }

                if (!isNewVersion)
                {
                    _statusMessage = $"You have the latest version ({remoteVersion}). No update needed.";
                    // Cleanup temp file
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                    return;
                }

                _statusMessage = "Installing...";
                UpdateStatus();

                // Extract and replace
                using (var archive = ZipFile.OpenRead(tempFile))
                {
                    foreach (var entry in archive.Entries)
                    {
                        // Skip directories
                        if (string.IsNullOrEmpty(entry.Name)) continue;

                        string destPath = Path.Combine(installDir, entry.FullName);
                        string destDir = Path.GetDirectoryName(destPath);

                        if (!Directory.Exists(destDir))
                            Directory.CreateDirectory(destDir);

                        // Handle locked files by renaming
                        if (File.Exists(destPath))
                        {
                            try
                            {
                                string oldPath = destPath + ".old";
                                if (File.Exists(oldPath))
                                    File.Delete(oldPath);
                                
                                File.Move(destPath, oldPath);
                            }
                            catch (Exception ex)
                            {
                                // If we can't move it, we might fail to extract
                                _statusMessage = $"Error renaming {entry.Name}: {ex.Message}";
                                // Don't return here, try to continue or let the extract fail
                            }
                        }

                        // Extract new file
                        entry.ExtractToFile(destPath, true);
                    }
                }

                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);

                _statusMessage = "Success! Please restart Rhino to apply changes.";
            }
            catch (Exception ex)
            {
                _statusMessage = $"Update Failed: {ex.Message}";
            }
            finally
            {
                _isUpdating = false;
                UpdateStatus();
            }
        }

        private void UpdateStatus()
        {
            // Request a solution expire on the UI thread to update the output message
            Rhino.RhinoApp.InvokeOnUiThread((Action)delegate
            {
                ExpireSolution(true);
            });
        }
    }
}
