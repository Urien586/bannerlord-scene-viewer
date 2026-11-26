// Bannerlord Scene Viewer: a desktop window (WebView2) showing the embedded viewer page.
// Usage: scene-viewer.exe [scene folder]  (or drop a SceneObj/<scene> folder onto the exe or into the window)
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

static class Program
{
    // Only these files are ever read from the scene folder given on the command line.
    static readonly string[] SceneFiles = { "navmesh.bin", "scene.xscene", "terrain.bin" };
    const string Origin = "https://app.local";

    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        string? scene = args.Length > 0 ? args[0].Trim('"') : null;
        if (scene != null && !SceneFiles.Any(f => File.Exists(Path.Combine(scene, f))))
        {
            MessageBox.Show($"{scene}\n\nhas no navmesh.bin, scene.xscene or terrain.bin. Pick a SceneObj/<scene> folder.", "Bannerlord Scene Viewer");
            scene = null;
        }
        byte[] page;
        using (var s = typeof(Program).Assembly.GetManifestResourceStream("scene-viewer.html")!) { using var ms = new MemoryStream(); s.CopyTo(ms); page = ms.ToArray(); }

        var form = new Form { Text = "Bannerlord Scene Viewer" + (scene != null ? " - " + Path.GetFileName(scene.TrimEnd('\\', '/')) : ""), Width = 1400, Height = 900, StartPosition = FormStartPosition.CenterScreen, Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!) };
        var web = new WebView2 { Dock = DockStyle.Fill };
        form.Controls.Add(web);
        form.Load += async (_, _) =>
        {
            try
            {
                // keep WebView2's cache in LocalAppData, not next to the exe (which may be read-only)
                var env = await CoreWebView2Environment.CreateAsync(null, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BannerlordSceneViewer"));
                await web.EnsureCoreWebView2Async(env);
            }
            catch (WebView2RuntimeNotFoundException)
            {
                MessageBox.Show("Microsoft Edge WebView2 Runtime is missing. Install it from https://go.microsoft.com/fwlink/p/?LinkId=2124703 and start again.", "Bannerlord Scene Viewer");
                form.Close(); return;
            }
            var core = web.CoreWebView2;
            core.Settings.AreDevToolsEnabled = false;
            core.AddWebResourceRequestedFilter(Origin + "/*", CoreWebView2WebResourceContext.All);
            core.WebResourceRequested += (_, e) =>
            {
                string path = new Uri(e.Request.Uri).AbsolutePath;
                if (path == "/")
                    e.Response = core.Environment.CreateWebResourceResponse(new MemoryStream(page), 200, "OK", "Content-Type: text/html; charset=utf-8");
                else if (scene != null && path.StartsWith("/scene/") && SceneFiles.Contains(path[7..]) && File.Exists(Path.Combine(scene, path[7..])))
                    e.Response = core.Environment.CreateWebResourceResponse(File.OpenRead(Path.Combine(scene, path[7..])), 200, "OK", "Content-Type: application/octet-stream");
                else
                    e.Response = core.Environment.CreateWebResourceResponse(null, 404, "Not Found", "");
            };
            core.Navigate(Origin + "/" + (scene != null ? "?scene=1" : ""));
        };
        Application.Run(form);
    }
}
