using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClassicUO.Game.Managers
{
    internal static class UpdateManager
    {
        public static bool SkipUpdateCheck { get; set; } = false;
        public static event EventHandler<EventArgs> UpdateStatusChanged;
        public static bool HasUpdate { get; private set; } = false;
        public static GitHubReleaseData MainReleaseData;

        public static void CheckForUpdates()
        {
            if (!SkipUpdateCheck)
            {
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        HttpRequestMessage restApi = new HttpRequestMessage()
                        {
                            Method = HttpMethod.Get,
                            RequestUri = new Uri("https://api.github.com/repos/bittiez/TazUO/releases/latest"),
                        };
                        restApi.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
                        restApi.Headers.Add("User-Agent", "Public");
                        string jsonResponse = new HttpClient().SendAsync(restApi).Result.Content.ReadAsStringAsync().Result;

                        MainReleaseData = JsonSerializer.Deserialize<GitHubReleaseData>(jsonResponse);

                        if (MainReleaseData != null)
                        {
                            if (MainReleaseData.tag_name.StartsWith("v"))
                            {
                                MainReleaseData.tag_name = MainReleaseData.tag_name.Substring(1);
                            }

                            if (Version.TryParse(MainReleaseData.tag_name, out var version))
                            {
                                if(version > CUOEnviroment.Version)
                                {
                                    HasUpdate = true;
                                    UpdateStatusChanged?.Invoke(null, EventArgs.Empty);
                                }
                            }
                        }
                    }
                    catch { }
                });
            }
        }

        public static void SendDelayedUpdateMessage()
        {
            Task.Factory.StartNew(() =>
            {
                Task.Delay(30000).Wait();
                GameActions.Print("TazUO has an update available, please visit https://github.com/bittiez/TazUO to get the most recent version.", 32);
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
