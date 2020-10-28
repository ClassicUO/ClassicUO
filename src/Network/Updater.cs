using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using TinyJson;

namespace ClassicUO.Network
{
    internal class Updater
    {
        private const string REPO_USER = "andreakarasho";
        private const string REPO_NAME = "ClassicUO";
        private const string API_RELEASES_LINK = "https://api.github.com/repos/{0}/{1}/releases";
        private int _animIndex;

        private readonly WebClient _client;
        private int _countDownload;
        private string _currentText = string.Empty;
        private double _progress;

        static Updater()
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        }

        public Updater()
        {
            _client = new WebClient
            {
                Proxy = null
            };

            _client.Headers.Add("User-Agent: Other");

            _client.DownloadFileCompleted += (s, e) =>
            {
                if (IsDownloading)
                {
                    Console.WriteLine();
                    Log.Trace("Download finished!");
                }
            };
        }

        public bool IsDownloading => _countDownload != 0;

        private void ClientOnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (IsDownloading)
            {
                double value = Math.Max(0, Math.Min(1, e.ProgressPercentage / 100d));
                Interlocked.Exchange(ref _progress, value);
                const int BLOCK_COUNT = 20;
                const string ANIMATION = @"|/-\";

                int progressBlockCount = (int) (_progress * BLOCK_COUNT);

                string text =
                    $"{new string('#', progressBlockCount)}{new string('-', BLOCK_COUNT - progressBlockCount)} - {e.ProgressPercentage}% {ANIMATION[_animIndex++ % ANIMATION.Length]}";

                int commonPrefixLength = 0;
                int commonLength = Math.Min(_currentText.Length, text.Length);

                while (commonPrefixLength < commonLength &&
                       text[commonPrefixLength] == _currentText[commonPrefixLength])
                {
                    commonPrefixLength++;
                }

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


        public bool Check()
        {
            try
            {
                return Download();
            }
            catch (Exception e)
            {
                Log.Error("UPDATER EXCEPTION: " + e);

                _client.DownloadProgressChanged -= ClientOnDownloadProgressChanged;
            }

            return false;
        }

        private bool Download()
        {
            if (IsDownloading)
            {
                return false;
            }

            Interlocked.Increment(ref _countDownload);

            Log.Trace("Checking update...");

            Reset();

            string json = _client.DownloadString(string.Format(API_RELEASES_LINK, REPO_USER, REPO_NAME));

            object[] data = json.Decode<object[]>();

#if DEV_BUILD
            FileInfo fileLastCommit = new FileInfo(Path.Combine(CUOEnviroment.ExecutablePath, "version.txt"));
#endif


            foreach (object entry in data)
            {
                Dictionary<string, object> releaseToken = entry as Dictionary<string, object>;

                if (releaseToken == null)
                {
                    continue;
                }

                string tagName = releaseToken["tag_name"].ToString();

                Log.Trace("Fetching: " + tagName);

#if DEV_BUILD
                if (tagName == "ClassicUO-dev-preview")
                {
                    bool ok = false;

                    string commitID = releaseToken["target_commitish"].ToString();

                    if (fileLastCommit.Exists)
                    {
                        string lastCommit = File.ReadAllText(fileLastCommit.FullName);
                        ok = lastCommit != commitID;
                    }

                    File.WriteAllText(fileLastCommit.FullName, commitID);
                    if (!ok)
                    {
                        break;
                    }
#else
                if (Version.TryParse(tagName, out Version version) && version > CUOEnviroment.Version)
                {
                    Log.Trace("Found new version available: " + version);

#endif
                    string name = releaseToken["name"].ToString();

                    string body = releaseToken["body"].ToString();

                    List<object> asset_list = releaseToken["assets"] as List<object>;

                    if (asset_list == null || asset_list.Count == 0)
                    {
                        Log.Error("No zip found for: " + name);

                        continue;
                    }

                    Dictionary<string, object> asset = asset_list[0] as Dictionary<string, object>;

                    if (asset == null)
                    {
                        continue;
                    }

                    string assetName = asset["name"].ToString();

                    string downloadUrl = asset["browser_download_url"].ToString();

                    string temp;

                    try
                    {
                        temp = Path.GetTempPath();
                    }
                    catch
                    {
                        Log.Warn("Impossible to retrive OS temp path. CUO will use current path");
                        temp = CUOEnviroment.ExecutablePath;
                    }

                    string tempPath = Path.Combine(temp, "update-temp");
                    string zipFile = Path.Combine(tempPath, assetName);

                    if (!Directory.Exists(tempPath))
                    {
                        Directory.CreateDirectory(tempPath);
                    }

                    Log.Trace("Downloading: " + assetName);

                    _client.DownloadProgressChanged += ClientOnDownloadProgressChanged;

                    _client.DownloadFile(downloadUrl, zipFile);

                    Log.Trace(assetName + "..... done");

                    _client.DownloadProgressChanged -= ClientOnDownloadProgressChanged;

                    Reset();

                    try
                    {
                        using (ZipArchive zip = new ZipArchive(File.OpenRead(zipFile)))
                        {
                            zip.ExtractToDirectory(tempPath, true);
                        }

                        File.Delete(zipFile);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("[UPDATER ERROR]: impossible to update.\n" + ex);
                    }

                    ProcessStartInfo processStartInfo = new ProcessStartInfo
                    {
                        WorkingDirectory = tempPath,
                        UseShellExecute = false
                    };

                    if (CUOEnviroment.IsUnix)
                    {
                        processStartInfo.FileName = "mono";

                        processStartInfo.Arguments =
                            $"\"{Path.Combine(tempPath, "ClassicUO.exe")}\" --source \"{CUOEnviroment.ExecutablePath}\" --pid {Process.GetCurrentProcess().Id} --action update";
                    }
                    else
                    {
                        processStartInfo.FileName = Path.Combine(tempPath, "ClassicUO.exe");

                        processStartInfo.Arguments =
                            $"--source \"{CUOEnviroment.ExecutablePath}\" --pid {Process.GetCurrentProcess().Id} --action update";
                    }

                    Process.Start(processStartInfo);

                    return true;
                }
            }

            Interlocked.Decrement(ref _countDownload);

            return false;
        }

        private void Reset()
        {
            _progress = 0;
            _currentText = string.Empty;
            _animIndex = 0;
        }
    }
}