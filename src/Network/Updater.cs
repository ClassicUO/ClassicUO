#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;

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
        private int _animIndex;
        private int _countDownload;
        private string _currentText = string.Empty;
        private double _progress;

        static Updater()
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
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
                    Log.Message(LogTypes.Trace, "Download finished!");
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

        private void Download()
        {
            if (IsDownloading)
                return;

            Interlocked.Increment(ref _countDownload);

            Log.Message(LogTypes.Trace, "Checking update...");

            Reset();

            string json = _client.DownloadString(string.Format(API_RELEASES_LINK, REPO_USER, REPO_NAME));

            JArray data = JsonConvert.DeserializeObject<JArray>(json);

#if DEV_BUILD
            FileInfo fileLastCommit = new FileInfo(Path.Combine(Engine.ExePath, "version.txt"));
#endif
            

            foreach (JToken releaseToken in data.Children())
            {
                string tagName = releaseToken["tag_name"].ToString();

                Log.Message(LogTypes.Trace, "Fetching: " + tagName);

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
                if (Version.TryParse(tagName, out Version version) && version > Engine.Version)
                {
                    Log.Message(LogTypes.Trace, "Found new version available: " + version);

#endif
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

                    if (Directory.Exists(tempPath))
                        Directory.Delete(tempPath);

                    Directory.CreateDirectory(tempPath);

                    Log.Message(LogTypes.Trace, "Downloading: " + assetName);

                    _client.DownloadProgressChanged += ClientOnDownloadProgressChanged;

                    _client.DownloadFile(downloadUrl, zipFile);

                    Log.Message(LogTypes.Trace, assetName + "..... done");

                    _client.DownloadProgressChanged -= ClientOnDownloadProgressChanged;

                    Reset();

                    ZipFile.ExtractToDirectory(zipFile, tempPath);

                    File.Delete(zipFile);

                    Process currentProcess = Process.GetCurrentProcess();

                    string prefix = Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix ? "mono " : string.Empty;

                    Process.Start(prefix + Path.Combine(Path.Combine(Engine.ExePath, "update-temp"), "ClassicUO.exe"), $"--source \"{Engine.ExePath}\" --pid {currentProcess.Id} --action update");
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