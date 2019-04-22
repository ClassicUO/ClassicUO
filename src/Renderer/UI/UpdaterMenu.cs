using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ClassicUO.Game.UI.Controls;
using ClassicUO.Network;
using ClassicUO.Utility.Logging;


using Microsoft.Xna.Framework;


namespace ClassicUO.Renderer.UI
{
    internal class UpdaterMenu : Control
    {
        public UpdaterMenu()
        {
            CanCloseWithRightClick = false;
            CanCloseWithEsc = false;

            CanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = false;
            ControlInfo.Layer = UILayer.Over;

            Width = 200;
            Height = 200;

            Color background = Color.Black;
            background.A = 160;

            Add(new Panel(0, 0, Width, Height, background));

            Add(new Label("New update available!", 5, 5));

            Button button;
            Add(button = new Button( (Width - 80) / 2, (Height - 30) ,80, 20, "Update"));

            button.MouseClick += (s, e) =>
            {
                if (_isUpdating == 0)
                    Task.Run(DoUpdate);
            };
        }

        private static int _isUpdating;

        private static async void DoUpdate()
        {
            //try
            //{
            //    Interlocked.Increment(ref _isUpdating);

            //    Log.Message(LogTypes.Trace, "Checking update...");

            //    GitHubClient client = new GitHubClient(new ProductHeaderValue("ClassicUO"), new Uri("https://github.com/andreakarasho/ClassicUO"));
            //    var releases = await client.Repository.Release.GetAll("andreakarasho", "ClassicUO");

            //    foreach (Release release in releases)
            //    {
            //        if (Version.TryParse(release.TagName, out Version version) && version > Engine.Version)
            //        {
            //            Log.Message(LogTypes.Trace, "Found new version available: " + version);

            //            ReleaseAsset zip = release.Assets.FirstOrDefault();

            //            if (zip == null)
            //            {
            //                Log.Message(LogTypes.Error, "No zip found for: " + release.Name);
            //                continue;
            //            }

            //            Log.Message(LogTypes.Trace, "Downloading: " + zip.Url);

            //            var response = await client.Connection.Get<object>(new Uri(zip.Url), new Dictionary<string, string>(), "application/octet-stream");

            //            string tempPath = Path.Combine(Engine.ExePath, "update-temp");

            //            if (!Directory.Exists(tempPath))
            //                Directory.CreateDirectory(tempPath);

            //            Log.Message(LogTypes.Trace, "Exctracting zip...");

            //            using (ZipFile file = ZipFile.Read(new MemoryStream((byte[])response.Body)))
            //                file.ExtractAll(tempPath, ExtractExistingFileAction.OverwriteSilently);

            //            Log.Message(LogTypes.Trace, "Start replacing...");

            //            Process currentProcess = Process.GetCurrentProcess();
            //            Process.Start(Path.Combine(tempPath, "ClassicUO.exe"), $"--source {Engine.ExePath} --pid {currentProcess.Id} --action update");
            //            currentProcess.Kill();

            //            break;
            //        }
            //    }

            //    Log.Message(LogTypes.Trace, "No update available.");

            //    Interlocked.Decrement(ref _isUpdating);
            //}
            //catch (Exception e)
            //{
            //    Log.Message(LogTypes.Panic, "UPDATE EXCEPTION: " + e);
            //}
        }
    }
}
