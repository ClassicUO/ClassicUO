using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Game.Data;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Utility.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Managers;
using ClassicUO.Network;
using ClassicUO.Utility.Platforms;

using SDL2;

namespace ClassicUO
{
    [Flags]
    enum ClientFlags : uint
    {
        CF_T2A = 0x00,
        CF_RE = 0x01,
        CF_TD = 0x02,
        CF_LBR = 0x04,
        CF_AOS = 0x08,
        CF_SE = 0x10,
        CF_SA = 0x20,
        CF_UO3D = 0x40,
        CF_RESERVED = 0x80,
        CF_3D = 0x100,
        CF_UNDEFINED = 0xFFFF,
    }


    static class Client
    {
        public static ClientVersion Version { get; private set; } 
        public static ClientFlags Protocol { get; set; }
        public static string ClientPath { get; private set; }
        public static bool IsUOPInstallation { get; private set; }
        public static bool UseUOPGumps { get; set; }
        public static ushort GraphicMask { get; private set; }
        public static GameController Game { get; private set; }




        public static void Run()
        {
            Debug.Assert(Game == null);

            Load();

            Log.Trace("Running game...");
            using (Game = new GameController())
            {
                Game.Run();
            }
            Log.Trace("Exiting game...");
        }

        public static void ShowErrorMessage(string msg)
        {
            if (Game != null && Game.Window.Handle != IntPtr.Zero)
                SDL.SDL_ShowSimpleMessageBox(SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR, "ERROR", msg, Game.Window.Handle);
        }


        private static void Load()
        {
            Log.Trace("Loading");
            Log.PushIndent();

            string clientPath = UOFileManager.UoFolderPath = Settings.GlobalSettings.UltimaOnlineDirectory;
            Log.Trace($"Ultima Online installation folder: {UOFileManager.UoFolderPath}");

            Log.Trace("Loading files...");
            Log.PushIndent();

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
                throw new InvalidClientDirectory($"'{clientPath}' is not a valid directory");
            }

            // try to load the client version
            if (!ClientVersionHelper.TryParse(clientVersionText, out ClientVersion clientVersion))
            {
                Log.Warn($"Client version [{clientVersionText}] is invalid, let's try to read the client.exe");

                // mmm something bad happened, try to load from client.exe
                if (!ClientVersionHelper.TryParseFromFile(Path.Combine(clientPath, "client.exe"), out clientVersionText) ||
                    !ClientVersionHelper.TryParse(clientVersionText, out clientVersion))
                {
                    Log.Error("Invalid client version: " + clientVersionText);
                    throw new InvalidClientVersion($"Invalid client version: '{clientVersionText}'");
                }

                Log.Trace($"Found a valid client.exe [{clientVersion}]");

                // update the wrong/missing client version in settings.json
                Settings.GlobalSettings.ClientVersion = clientVersionText;
            }

            Version = clientVersion;
            ClientPath = clientPath;
            IsUOPInstallation = Version >= ClientVersion.CV_70240;
            GraphicMask = IsUOPInstallation ? (ushort) 0xFFFF : (ushort) 0x3FFF;
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
            PacketsTable.AdjustPacketSizeByVersion(Client.Version);
            Log.Trace("Done!");

            Log.Trace("Loading plugins...");

            foreach (var p in Settings.GlobalSettings.Plugins)
                Plugin.Create(p);
            Log.Trace("Done!");


            UoAssist.Start();

            Log.PopIndent();
            Log.Trace("Loading done");
        }
    }
}
