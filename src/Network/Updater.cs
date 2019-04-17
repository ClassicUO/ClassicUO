using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClassicUO.Network
{
    internal class Updater
    {
        private const string REPO_USER = "andreakarasho";
        private const string REPO_NAME = "ClassicUO";
        private const string API_RELEASES_LINK = "https://api.github.com/repos/{0}/{1}/releases";

        private readonly WebClient _client;
        private int _countDownload;
        private double _progress;
        private string _currentText = string.Empty;
        private int _animIndex;

        static Updater()
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate
            {
                return true;
            };
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12;
        }

        public Updater()
        {
            _client = new WebClient()
            {
                Proxy = null,
            };
            _client.Headers.Add("User-Agent: Other");

            _client.DownloadFileCompleted += (s, e) =>
            {
                if (IsDownloading)
                {
                    Console.WriteLine();
                    Log.Message(LogTypes.Trace, $"Download finished!");
                }
            };          
        }

        private void ClientOnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (IsDownloading)
            {
                double value = Math.Max(0, Math.Min(1, e.ProgressPercentage / 100d));
                Interlocked.Exchange(ref _progress, value);
                const int BLOCK_COUNT = 20;
                const string ANIMATION = @"|/-\";

                int progressBlockCount = (int)(_progress * BLOCK_COUNT);

                string text = $"{new string('#', progressBlockCount)}{new string('-', BLOCK_COUNT - progressBlockCount)} - {e.ProgressPercentage}% {ANIMATION[_animIndex++ % ANIMATION.Length]}";

                int commonPrefixLength = 0;
                int commonLength = Math.Min(_currentText.Length, text.Length);
                while (commonPrefixLength < commonLength && text[commonPrefixLength] == _currentText[commonPrefixLength])
                    commonPrefixLength++;

                StringBuilder outputBuilder = new StringBuilder();
                outputBuilder.Append('\b', _currentText.Length - commonPrefixLength);
                outputBuilder.Append(text.Substring(commonPrefixLength));
                int overlapCount = _currentText.Length - text.Length;
                if (overlapCount > 0)
                {
                    outputBuilder.Append(' ', overlapCount);
                    outputBuilder.Append('\b', overlapCount);
                }

                Console.Write(outputBuilder);
                _currentText = text;
            }
        }

        public bool IsDownloading => _countDownload != 0;


        public void Check()
        {          
            try
            {
                Download();
            }
            catch (Exception e)
            {
                Log.Message(LogTypes.Error, "UPDATER EXCEPTION: " + e);

                _client.DownloadProgressChanged -= ClientOnDownloadProgressChanged;
            }
        }

        private async void Download()
        {
            if (IsDownloading)
                return;

            Interlocked.Increment(ref _countDownload);

            Log.Message(LogTypes.Trace, "Checking update...");

            Reset();

            string json = await _client.DownloadStringTaskAsync(string.Format(API_RELEASES_LINK, REPO_USER, REPO_NAME));

            JArray data = JsonConvert.DeserializeObject<JArray>(json);

            foreach (JToken releaseToken in data.Children())
            {
                string tagName = releaseToken["tag_name"].ToString();

                Log.Message(LogTypes.Trace, "Fetching: " + tagName);

                if (Version.TryParse(tagName, out Version version) && version > Engine.Version)
                {
                    Log.Message(LogTypes.Trace, "Found new version available: " + version);

                    string name = releaseToken["name"].ToString();
                    string body = releaseToken["body"].ToString();

                    JToken asset = releaseToken["assets"];

                    if (!asset.HasValues)
                    {
                        Log.Message(LogTypes.Error, "No zip found for: " + name);
                        continue;
                    }

                    asset = asset.First;

                    string assetName = asset["name"].ToString();
                    string downloadUrl = asset["browser_download_url"].ToString();

                    string tempPath = Path.Combine(Engine.ExePath, "update-temp");
                    string zipFile = Path.Combine(tempPath, assetName);

                    if (!Directory.Exists(tempPath))
                        Directory.CreateDirectory(tempPath);

                    Log.Message(LogTypes.Trace, "Downloading: " + assetName);

                    _client.DownloadProgressChanged += ClientOnDownloadProgressChanged;

                    await _client.DownloadFileTaskAsync(downloadUrl, zipFile);

                    Log.Message(LogTypes.Trace, assetName + "..... done");

                    _client.DownloadProgressChanged -= ClientOnDownloadProgressChanged;

                    Reset();

                    ZipFile.ExtractToDirectory(zipFile, tempPath);

                    File.Delete(zipFile);

                    Process currentProcess = Process.GetCurrentProcess();

                    string prefix = Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix ? "mono " : string.Empty;

                    Process.Start(prefix + Path.Combine(Path.Combine(Engine.ExePath, "update-temp"), "ClassicUO.exe"), $"--source {Engine.ExePath} --pid {currentProcess.Id} --action update");
                    currentProcess.Kill();

                    break;
                }
            }

            Interlocked.Decrement(ref _countDownload);
        }

        private void Reset()
        {
            _progress = 0;
            _currentText = string.Empty;
            _animIndex = 0;

        }
    }
}
