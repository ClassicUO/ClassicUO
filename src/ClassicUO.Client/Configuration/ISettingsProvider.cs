// SPDX-License-Identifier: BSD-2-Clause

using Microsoft.Xna.Framework;

namespace ClassicUO.Configuration
{
    internal interface ISettingsProvider
    {
        string Username { get; set; }
        string Password { get; set; }
        string IP { get; set; }
        ushort Port { get; set; }
        bool IgnoreRelayIp { get; set; }
        string UltimaOnlineDirectory { get; set; }
        string ProfilesPath { get; set; }
        string ClientVersion { get; set; }
        string Language { get; set; }
        ushort LastServerNum { get; set; }
        string LastServerName { get; set; }
        int FPS { get; set; }
        float ScreenScale { get; set; }
        Point? WindowPosition { get; set; }
        Point? WindowSize { get; set; }
        bool IsWindowMaximized { get; set; }
        bool SaveAccount { get; set; }
        bool AutoLogin { get; set; }
        bool Reconnect { get; set; }
        int ReconnectTime { get; set; }
        bool LoginMusic { get; set; }
        int LoginMusicVolume { get; set; }
        bool FixedTimeStep { get; set; }
        bool RunMouseInASeparateThread { get; set; }
        byte ForceDriver { get; set; }
        bool UseVerdata { get; set; }
        string MapsLayouts { get; set; }
        byte Encryption { get; set; }
        string[] Plugins { get; set; }
        string OverrideFile { get; set; }

        void Save();
    }
}
