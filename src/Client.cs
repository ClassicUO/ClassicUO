#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Game.Data;
using ClassicUO.IO;
using ClassicUO.Utility.Logging;
using System;
using System.Diagnostics;
using System.IO;
using ClassicUO.Network;
using ClassicUO.Utility.Platforms;

using SDL2;

namespace ClassicUO
{
    static class Client
    {
        public static ClientVersion Version { get; private set; } 
        public static ClientFlags Protocol { get; set; }
        public static string ClientPath { get; private set; }
        public static bool IsUOPInstallation { get; private set; }
        public static bool UseUOPGumps { get; set; }
        public static GameController Game { get; private set; }


        public static void Run()
        {
            Debug.Assert(Game == null);

            Log.Trace("Running game...");
            using (Game = new GameController())
            {
                // https://github.com/FNA-XNA/FNA/wiki/7:-FNA-Environment-Variables#fna_graphics_enable_highdpi
                CUOEnviroment.IsHighDPI = Environment.GetEnvironmentVariable("FNA_GRAPHICS_ENABLE_HIGHDPI") == "1";
                Game.Run();
            }
            Log.Trace("Exiting game...");
        }

        public static void ShowErrorMessage(string msg)
        {
            SDL.SDL_ShowSimpleMessageBox(SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR, "ERROR", msg, IntPtr.Zero);
        }


        public static void Load()
        {
            Log.Trace(">>>>>>>>>>>>> Loading >>>>>>>>>>>>>");

            string clientPath = Settings.GlobalSettings.UltimaOnlineDirectory;
            Log.Trace($"Ultima Online installation folder: {clientPath}");

            Log.Trace("Loading files...");

            if (!string.IsNullOrWhiteSpace(Settings.GlobalSettings.ClientVersion))
            {
                // sanitize client version
                Settings.GlobalSettings.ClientVersion = Settings.GlobalSettings.ClientVersion.Replace(",", ".").Replace(" ", "").ToLower();
            }

            string clientVersionText = Settings.GlobalSettings.ClientVersion;

            // check if directory is good
            if (!Directory.Exists(clientPath))
            {
                Log.Error("Invalid client directory: " + clientPath);
                ShowErrorMessage($"'{clientPath}' is not a valid UO directory");
                throw new InvalidClientDirectory($"'{clientPath}' is not a valid directory");
            }

            // try to load the client version
            if (!ClientVersionHelper.IsClientVersionValid(clientVersionText, out ClientVersion clientVersion))
            {
                Log.Warn($"Client version [{clientVersionText}] is invalid, let's try to read the client.exe");

                // mmm something bad happened, try to load from client.exe
                if (!ClientVersionHelper.TryParseFromFile(Path.Combine(clientPath, "client.exe"), out clientVersionText) ||
                    !ClientVersionHelper.IsClientVersionValid(clientVersionText, out clientVersion))
                {
                    Log.Error("Invalid client version: " + clientVersionText);
                    ShowErrorMessage($"Impossible to define the client version.\nClient version: '{clientVersionText}'");
                    throw new InvalidClientVersion($"Invalid client version: '{clientVersionText}'");
                }

                Log.Trace($"Found a valid client.exe [{clientVersionText} - {clientVersion}]");

                // update the wrong/missing client version in settings.json
                Settings.GlobalSettings.ClientVersion = clientVersionText;
            }

            Version = clientVersion;
            ClientPath = clientPath;
            IsUOPInstallation = Version >= ClientVersion.CV_7000 && File.Exists(UOFileManager.GetUOFilePath("MainMisc.uop"));
            Protocol = ClientFlags.CF_T2A;

            if (Version >= ClientVersion.CV_200)
                Protocol |= ClientFlags.CF_RE;
            if (Version >= ClientVersion.CV_300)
                Protocol |= ClientFlags.CF_TD;
            if (Version >= ClientVersion.CV_308)
                Protocol |= ClientFlags.CF_LBR;
            if (Version >= ClientVersion.CV_308Z)
                Protocol |= ClientFlags.CF_AOS;
            if (Version >= ClientVersion.CV_405A)
                Protocol |= ClientFlags.CF_SE;
            if (Version >= ClientVersion.CV_60144)
                Protocol |= ClientFlags.CF_SA;

            Log.Trace($"Client path: '{clientPath}'");
            Log.Trace($"Client version: {clientVersion}");
            Log.Trace($"Protocol: {Protocol}");
            Log.Trace("UOP? " + (IsUOPInstallation ? "yes" : "no"));

            // ok now load uo files
            UOFileManager.Load();
            StaticFilters.Load();

            Log.Trace("Network calibration...");
            PacketHandlers.Load();
            //ATTENTION: you will need to enable ALSO ultimalive server-side, or this code will have absolutely no effect!
            UltimaLive.Enable();
            PacketsTable.AdjustPacketSizeByVersion(Version);

            if (Settings.GlobalSettings.Encryption != 0)
            {
                Log.Trace("Calculating encryption by client version...");
                EncryptionHelper.CalculateEncryption(Version);
                Log.Trace($"encryption: {EncryptionHelper.Type}");

                if (EncryptionHelper.Type != (ENCRYPTION_TYPE) Settings.GlobalSettings.Encryption)
                {
                    Log.Warn($"Encryption found: {EncryptionHelper.Type}");
                    Settings.GlobalSettings.Encryption = (byte) EncryptionHelper.Type;
                }
            }

            Log.Trace("Done!");

            Log.Trace("Loading plugins...");

            foreach (var p in Settings.GlobalSettings.Plugins)
                Plugin.Create(p);
            Log.Trace("Done!");

            UoAssist.Start();

            Log.Trace(">>>>>>>>>>>>> DONE >>>>>>>>>>>>>");
        }
    }
}
