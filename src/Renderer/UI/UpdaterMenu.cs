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

using System.Threading.Tasks;

using ClassicUO.Game.UI.Controls;

using Microsoft.Xna.Framework;

namespace ClassicUO.Renderer.UI
{
    internal class UpdaterMenu : Control
    {
        private static int _isUpdating;

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
            Add(button = new Button((Width - 80) >> 1, Height - 30, 80, 20, "Update"));

            button.MouseClick += (s, e) =>
            {
                if (_isUpdating == 0)
                    Task.Run(DoUpdate);
            };
        }

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