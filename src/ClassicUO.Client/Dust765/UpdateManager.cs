using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ClassicUO;
using ClassicUO.Utility.Platforms;

namespace ClassicUO.Dust765
{
    internal static class UpdateManager
    {
        private const string GITHUB_RELEASES_URL = "https://api.github.com/repos/dust765/ClassicUO/releases";

        public static bool SkipUpdateCheck { get; set; } = false;
        public static event EventHandler<EventArgs> UpdateStatusChanged;
        public static bool HasUpdate { get; private set; } = false;
        public static GitHubReleaseData MainReleaseData;
        public static bool IsUpdating { get; private set; }

        public static void CheckForUpdates()
        {
            if (!SkipUpdateCheck)
            {
                Task.Run(CheckForUpdatesAsync);
            }
        }

        private static async Task CheckForUpdatesAsync()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, new Uri(GITHUB_RELEASES_URL));
                request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
                request.Headers.Add("User-Agent", "ClassicUO-Update");
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();
                    var releases = JsonSerializer.Deserialize<GitHubReleaseData[]>(json);
                    if (releases != null && releases.Length > 0)
                    {
                        MainReleaseData = releases[0];
                        if (MainReleaseData.draft)
                        {
                            var published = releases.FirstOrDefault(r => !r.draft);
                            if (published != null)
                                MainReleaseData = published;
                        }
                        var tag = MainReleaseData.tag_name.TrimStart('v');
                        if (Version.TryParse(tag, out var remote) && remote > CUOEnviroment.Version)
                        {
                            HasUpdate = true;
                            UpdateStatusChanged?.Invoke(null, EventArgs.Empty);
                        }
                    }
                }
            }
            catch { }
        }

        public static void SendDelayedUpdateMessage()
        {
            Task.Run(SendDelayedUpdateMessageAsync);
        }

        private static async Task SendDelayedUpdateMessageAsync()
        {
            await Task.Delay(30000);
            ClassicUO.Game.GameActions.Print("Update available. Visit https://github.com/dust765/ClassicUO/releases", 32);
        }

        public static void StartUpdateAndExit()
        {
            if (IsUpdating)
                return;
            if (MainReleaseData?.assets == null || MainReleaseData.assets.Length == 0)
            {
                PlatformHelper.LaunchBrowser(MainReleaseData?.html_url ?? "https://github.com/dust765/ClassicUO/releases");
                return;
            }
            var zipAsset = MainReleaseData.assets.FirstOrDefault(a => a.name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true);
            if (zipAsset == null)
            {
                PlatformHelper.LaunchBrowser(MainReleaseData.html_url ?? "https://github.com/dust765/ClassicUO/releases");
                return;
            }
            IsUpdating = true;
            Task.Run(async () =>
            {
                try
                {
                    var exeDir = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
                    var tempZip = Path.Combine(Path.GetTempPath(), "ClassicUO_Update_" + Guid.NewGuid().ToString("N") + ".zip");
                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "ClassicUO-Update");
                        using (var stream = await client.GetStreamAsync(zipAsset.browser_download_url))
                        using (var fs = File.Create(tempZip))
                            await stream.CopyToAsync(fs);
                    }
                    var batchPath = Path.Combine(Path.GetTempPath(), "ClassicUO_Update_" + Guid.NewGuid().ToString("N") + ".bat");
                    var exePath = Path.Combine(exeDir, "ClassicUO.exe");
                    var batchContent = string.Format(@"@echo off
timeout /t 2 /nobreak > nul
cd /d ""{0}""
powershell -NoProfile -ExecutionPolicy Bypass -Command ""Expand-Archive -Path '{1}' -DestinationPath '.' -Force""
del ""{1}""
start """" ""{2}""
del ""{3}""
", exeDir, tempZip, exePath, batchPath);
                    File.WriteAllText(batchPath, batchContent);
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = batchPath,
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        WorkingDirectory = exeDir
                    });
                    await Task.Delay(500);
                    Environment.Exit(0);
                }
                catch
                {
                    IsUpdating = false;
                    if (MainReleaseData?.html_url != null)
                        PlatformHelper.LaunchBrowser(MainReleaseData.html_url);
                }
            });
        }
    }

    internal class GitHubReleaseData
    {
        public string url { get; set; }
        public string assets_url { get; set; }
        public string upload_url { get; set; }
        public string html_url { get; set; }
        public int id { get; set; }
        public Author author { get; set; }
        public string node_id { get; set; }
        public string tag_name { get; set; }
        public string target_commitish { get; set; }
        public string name { get; set; }
        public bool draft { get; set; }
        public bool prerelease { get; set; }
        public DateTime created_at { get; set; }
        public DateTime published_at { get; set; }
        public Asset[] assets { get; set; }
        public string tarball_url { get; set; }
        public string zipball_url { get; set; }
        public string body { get; set; }
        public int mentions_count { get; set; }

        public class Author
        {
            public string login { get; set; }
            public int id { get; set; }
            public string node_id { get; set; }
            public string avatar_url { get; set; }
            public string gravatar_id { get; set; }
            public string url { get; set; }
            public string html_url { get; set; }
            public string followers_url { get; set; }
            public string following_url { get; set; }
            public string gists_url { get; set; }
            public string starred_url { get; set; }
            public string subscriptions_url { get; set; }
            public string organizations_url { get; set; }
            public string repos_url { get; set; }
            public string events_url { get; set; }
            public string received_events_url { get; set; }
            public string type { get; set; }
            public bool site_admin { get; set; }
        }

        public class Asset
        {
            public string url { get; set; }
            public int id { get; set; }
            public string node_id { get; set; }
            public string name { get; set; }
            public object label { get; set; }
            public Uploader uploader { get; set; }
            public string content_type { get; set; }
            public string state { get; set; }
            public int size { get; set; }
            public int download_count { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
            public string browser_download_url { get; set; }
        }

        public class Uploader
        {
            public string login { get; set; }
            public int id { get; set; }
            public string node_id { get; set; }
            public string avatar_url { get; set; }
            public string gravatar_id { get; set; }
            public string url { get; set; }
            public string html_url { get; set; }
            public string followers_url { get; set; }
            public string following_url { get; set; }
            public string gists_url { get; set; }
            public string starred_url { get; set; }
            public string subscriptions_url { get; set; }
            public string organizations_url { get; set; }
            public string repos_url { get; set; }
            public string events_url { get; set; }
            public string received_events_url { get; set; }
            public string type { get; set; }
            public bool site_admin { get; set; }
        }
    }
}
